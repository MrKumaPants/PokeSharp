using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PokeSharp.Core.Components;
using PokeSharp.Core.Templates;
using PokeSharp.Input.Components;

namespace PokeSharp.Game.Templates;

/// <summary>
///     Centralized registry for all entity templates.
///     Registers templates with the cache during game initialization.
///     Located in Game project to access all component types without circular dependencies.
/// </summary>
public static class TemplateRegistry
{
    /// <summary>
    ///     Register all built-in entity templates with the cache.
    ///     Called during game initialization.
    /// </summary>
    /// <param name="cache">Template cache to register templates with</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public static void RegisterAllTemplates(TemplateCache cache, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(cache);

        logger?.LogInformation("Registering entity templates...");

        // Register player template
        RegisterPlayerTemplate(cache, logger);

        // Register NPC templates
        RegisterNpcTemplates(cache, logger);

        var stats = cache.GetStatistics();
        logger?.LogInformation("✅ Registered {Count} entity templates", stats.TotalTemplates);
    }

    /// <summary>
    ///     Register the player entity template.
    /// </summary>
    private static void RegisterPlayerTemplate(TemplateCache cache, ILogger? logger = null)
    {
        var template = new EntityTemplate
        {
            TemplateId = "player",
            Name = "Player Character",
            Tag = "player",
            Metadata = new EntityTemplateMetadata
            {
                Version = "1.0.0",
                CompiledAt = DateTime.UtcNow,
                SourcePath = "TemplateRegistry.RegisterPlayerTemplate",
            },
        };

        // Add components in the order they should be created
        template.WithComponent(new Player());
        template.WithComponent(new Position(0, 0)); // Default position, overridden at spawn
        template.WithComponent(new Sprite("player-spritesheet") { Tint = Color.White, Scale = 1f });
        template.WithComponent(new GridMovement(4.0f)); // 4 tiles per second
        template.WithComponent(Direction.Down); // Face down by default
        template.WithComponent(new Animation("idle_down")); // Start idle
        template.WithComponent(new InputState());
        template.WithComponent(new Collision(true)); // Player blocks movement

        cache.Register(template);
        logger?.LogDebug("Registered template: {TemplateId}", template.TemplateId);
    }

    /// <summary>
    ///     Register NPC entity templates.
    /// </summary>
    private static void RegisterNpcTemplates(TemplateCache cache, ILogger? logger = null)
    {
        // Example: Generic NPC template
        var npcTemplate = new EntityTemplate
        {
            TemplateId = "npc/generic",
            Name = "Generic NPC",
            Tag = "npc",
            Metadata = new EntityTemplateMetadata
            {
                Version = "1.0.0",
                CompiledAt = DateTime.UtcNow,
                SourcePath = "TemplateRegistry.RegisterNpcTemplates",
            },
        };

        npcTemplate.WithComponent(new Position(0, 0));
        npcTemplate.WithComponent(new Sprite("npc-spritesheet") { Tint = Color.White, Scale = 1f });
        npcTemplate.WithComponent(new GridMovement(2.0f)); // NPCs move slower
        npcTemplate.WithComponent(Direction.Down);
        npcTemplate.WithComponent(new Animation("idle_down"));
        npcTemplate.WithComponent(new Collision(true)); // NPCs block movement

        cache.Register(npcTemplate);
        logger?.LogDebug("Registered template: {TemplateId}", npcTemplate.TemplateId);
    }

    /// <summary>
    ///     Get all template IDs registered in the system.
    ///     Useful for debugging and tools.
    /// </summary>
    public static string[] GetAllTemplateIds()
    {
        return new[] { "player", "npc/generic" };
    }
}
