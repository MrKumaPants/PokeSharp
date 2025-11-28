using Arch.Core;
using Arch.Core.Extensions;
using System.Reflection;

namespace PokeSharp.Engine.Debug.Entities;

/// <summary>
/// Registry for ECS component types used in the entity browser.
/// Allows extensible component detection without modifying ConsoleSystem.
/// </summary>
public class DebugComponentRegistry
{
    private readonly List<ComponentDescriptor> _descriptors = new();
    private readonly Dictionary<Type, ComponentDescriptor> _typeToDescriptor = new();

    /// <summary>
    /// Describes a registered component type.
    /// </summary>
    public class ComponentDescriptor
    {
        public Type ComponentType { get; init; } = null!;
        public string DisplayName { get; init; } = "";
        public string? Category { get; init; }
        public Func<Entity, bool> HasComponent { get; init; } = _ => false;
        public Func<Entity, Dictionary<string, string>>? GetProperties { get; init; }
        public int Priority { get; init; } = 0;
    }

    /// <summary>
    /// Registers a component type with the given display name.
    /// </summary>
    public DebugComponentRegistry Register<T>(string displayName, string? category = null, int priority = 0) where T : struct
    {
        var descriptor = new ComponentDescriptor
        {
            ComponentType = typeof(T),
            DisplayName = displayName,
            Category = category,
            Priority = priority,
            HasComponent = entity => entity.Has<T>()
        };

        _descriptors.Add(descriptor);
        _typeToDescriptor[typeof(T)] = descriptor;
        return this;
    }

    /// <summary>
    /// Registers a component type with a property reader.
    /// </summary>
    public DebugComponentRegistry Register<T>(
        string displayName,
        Func<T, Dictionary<string, string>> propertyReader,
        string? category = null,
        int priority = 0) where T : struct
    {
        var descriptor = new ComponentDescriptor
        {
            ComponentType = typeof(T),
            DisplayName = displayName,
            Category = category,
            Priority = priority,
            HasComponent = entity => entity.Has<T>(),
            GetProperties = entity =>
            {
                if (!entity.Has<T>()) return new Dictionary<string, string>();
                ref var component = ref entity.Get<T>();
                return propertyReader(component);
            }
        };

        _descriptors.Add(descriptor);
        _typeToDescriptor[typeof(T)] = descriptor;
        return this;
    }

    /// <summary>
    /// Detects all registered components on an entity.
    /// </summary>
    public List<string> DetectComponents(Entity entity)
    {
        return _descriptors
            .Where(d => d.HasComponent(entity))
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.DisplayName)
            .Select(d => d.DisplayName)
            .ToList();
    }

    /// <summary>
    /// Gets properties for all components on an entity.
    /// </summary>
    public Dictionary<string, string> GetEntityProperties(Entity entity)
    {
        var properties = new Dictionary<string, string>();

        foreach (var descriptor in _descriptors.Where(d => d.HasComponent(entity) && d.GetProperties != null))
        {
            var componentProps = descriptor.GetProperties!(entity);
            foreach (var (key, value) in componentProps)
            {
                // Prefix with component name to avoid collisions
                properties[$"{descriptor.DisplayName}.{key}"] = value;
            }
        }

        return properties;
    }

    /// <summary>
    /// Gets a simple properties dictionary (no component prefix) for display.
    /// </summary>
    public Dictionary<string, string> GetSimpleProperties(Entity entity)
    {
        var properties = new Dictionary<string, string>();

        foreach (var descriptor in _descriptors.Where(d => d.HasComponent(entity) && d.GetProperties != null))
        {
            var componentProps = descriptor.GetProperties!(entity);
            foreach (var (key, value) in componentProps)
            {
                // Use simple key, last one wins in case of collision
                properties[key] = value;
            }
        }

        return properties;
    }

    /// <summary>
    /// Gets all registered component names.
    /// </summary>
    public IEnumerable<string> GetAllComponentNames()
    {
        return _descriptors.Select(d => d.DisplayName).Distinct();
    }

    /// <summary>
    /// Gets all categories.
    /// </summary>
    public IEnumerable<string> GetCategories()
    {
        return _descriptors
            .Where(d => d.Category != null)
            .Select(d => d.Category!)
            .Distinct();
    }

    /// <summary>
    /// Determines entity name based on registered components (highest priority first).
    /// </summary>
    public string DetermineEntityName(Entity entity, List<string> components)
    {
        // Priority-based naming
        if (components.Contains("Player"))
            return "Player";
        if (components.Contains("Npc"))
            return $"NPC_{entity.Id}";
        if (components.Contains("TileSprite"))
            return $"Tile_{entity.Id}";
        if (components.Contains("AnimatedTile"))
            return $"AnimTile_{entity.Id}";
        if (components.Contains("Sprite") && components.Contains("Position"))
            return $"Sprite_{entity.Id}";

        return $"Entity_{entity.Id}";
    }

    /// <summary>
    /// Determines entity tag based on components.
    /// </summary>
    public string? DetermineEntityTag(List<string> components)
    {
        if (components.Contains("Player"))
            return "Player";
        if (components.Contains("Npc"))
            return "NPC";
        if (components.Contains("TileSprite"))
            return "Tile";
        if (components.Contains("AnimatedTile"))
            return "AnimatedTile";
        if (components.Contains("Collision"))
            return "Collision";
        if (components.Contains("Sprite"))
            return "Sprite";
        if (components.Contains("Behavior"))
            return "Behavior";

        return components.Count > 0 ? components[0] : null;
    }
}

