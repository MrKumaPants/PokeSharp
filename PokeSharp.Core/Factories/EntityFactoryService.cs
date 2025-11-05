using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeSharp.Core.Templates;

namespace PokeSharp.Core.Factories;

/// <summary>
///     Implementation of <see cref="IEntityFactoryService" /> for spawning Arch ECS entities from templates.
///     Resolves templates from <see cref="TemplateCache" /> and instantiates entities with components.
///     Thread-safe and supports hot-reload via template cache invalidation.
/// </summary>
public sealed class EntityFactoryService : IEntityFactoryService
{
    private readonly ILogger<EntityFactoryService> _logger;
    private readonly TemplateCache _templateCache;

    public EntityFactoryService(TemplateCache templateCache, ILogger<EntityFactoryService> logger)
    {
        _templateCache = templateCache ?? throw new ArgumentNullException(nameof(templateCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Entity> SpawnFromTemplateAsync(
        string templateId,
        World world,
        EntitySpawnContext? context = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId, nameof(templateId));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        // Retrieve template from cache
        var template = _templateCache.Get(templateId);
        if (template == null)
        {
            _logger.LogError("Template not found: {TemplateId}", templateId);
            throw new ArgumentException(
                $"Template '{templateId}' not found in cache",
                nameof(templateId)
            );
        }

        // Validate template before spawning
        var validationResult = ValidateTemplateInternal(template);
        if (!validationResult.IsValid)
        {
            _logger.LogError(
                "Template validation failed for {TemplateId}: {Errors}",
                templateId,
                string.Join(", ", validationResult.Errors)
            );
            throw new InvalidOperationException(
                $"Template '{templateId}' is invalid: {string.Join(", ", validationResult.Errors)}"
            );
        }

        // Build component array from template
        var components = BuildComponentArray(template, context);

        // Create empty entity first
        var entity = world.Create();

        // Add each component using reflection (Arch requires compile-time types)
        foreach (var component in components)
        {
            var componentType = component.GetType();

            // Get the generic Add<T> method and make it concrete for this component type
            var addMethod = typeof(World)
                .GetMethods()
                .Where(m => m.Name == nameof(World.Add) && m.IsGenericMethod)
                .FirstOrDefault(m =>
                {
                    var parameters = m.GetParameters();
                    return parameters.Length == 2 && parameters[0].ParameterType == typeof(Entity);
                });

            if (addMethod != null)
            {
                // Make the generic method concrete for this component type
                var genericMethod = addMethod.MakeGenericMethod(componentType);

                // Invoke Add<T>(entity, component)
                genericMethod.Invoke(world, new[] { entity, component });
                _logger.LogDebug("  ✓ Added {Type} to entity", componentType.Name);
            }
            else
            {
                _logger.LogWarning(
                    "Could not find Add<T> method for component type {Type}",
                    componentType.Name
                );
            }
        }

        _logger.LogDebug(
            "Spawned entity {EntityId} from template {TemplateId} with {ComponentCount} components",
            entity.Id,
            templateId,
            components.Count
        );

        return await Task.FromResult(entity);
    }

    /// <inheritdoc />
    public async Task<Entity> SpawnFromTemplateAsync(
        string templateId,
        World world,
        Action<EntityBuilder> configure,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId, nameof(templateId));
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        // Build context from fluent API
        var builder = new EntityBuilder();
        configure(builder);

        // Convert builder to spawn context
        var context = new EntitySpawnContext
        {
            Tag = builder.Tag,
            Overrides = builder.ComponentOverrides.ToDictionary(
                kvp => kvp.Key.Name,
                kvp => kvp.Value
            ),
        };

        // Add custom properties if any
        if (builder.CustomProperties.Any())
            context.Metadata = builder.CustomProperties.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
            );

        return await SpawnFromTemplateAsync(templateId, world, context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Entity>> SpawnBatchAsync(
        IEnumerable<string> templateIds,
        World world,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(templateIds, nameof(templateIds));
        ArgumentNullException.ThrowIfNull(world, nameof(world));

        var entities = new List<Entity>();
        var templateIdList = templateIds.ToList();

        _logger.LogDebug("Batch spawning {Count} entities", templateIdList.Count);

        foreach (var templateId in templateIdList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var entity = await SpawnFromTemplateAsync(
                    templateId,
                    world,
                    (EntitySpawnContext?)null,
                    cancellationToken
                );
                entities.Add(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to spawn entity from template {TemplateId}",
                    templateId
                );
                throw;
            }
        }

        _logger.LogInformation("Successfully spawned {Count} entities in batch", entities.Count);
        return entities;
    }

    /// <inheritdoc />
    public TemplateValidationResult ValidateTemplate(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId, nameof(templateId));

        var template = _templateCache.Get(templateId);
        if (template == null)
            return TemplateValidationResult.Failure($"Template '{templateId}' not found in cache");

        return ValidateTemplateInternal(template);
    }

    /// <inheritdoc />
    public bool HasTemplate(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId, nameof(templateId));
        return _templateCache.Get(templateId) != null;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetTemplateIdsByTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag, nameof(tag));
        return _templateCache.GetByTag(tag).Select(t => t.TemplateId);
    }

    // Private helper methods

    private static TemplateValidationResult ValidateTemplateInternal(EntityTemplate template)
    {
        var isValid = template.Validate(out var errors);
        return isValid
            ? TemplateValidationResult.Success(template.TemplateId)
            : TemplateValidationResult.Failure(template.TemplateId, errors.ToArray());
    }

    private static List<object> BuildComponentArray(
        EntityTemplate template,
        EntitySpawnContext? context
    )
    {
        var components = new List<object>();

        foreach (var componentTemplate in template.Components)
        {
            // Check if context has override for this component
            var componentTypeName = componentTemplate.ComponentType.Name;
            if (
                context?.Overrides != null
                && context.Overrides.TryGetValue(componentTypeName, out var overrideData)
            )
                // Use override data
                components.Add(overrideData);
            else
                // Use template's initial data
                components.Add(componentTemplate.InitialData);
        }

        return components;
    }
}
