#r "PokeSharp.Engine.Core.dll"
#load "events/WeatherEvents.csx"

using PokeSharp.Engine.Core.Events;
using PokeSharp.Engine.Core.Scripting;
using System;
using System.Threading.Tasks;

/// <summary>
/// Handles lightning flash and thunder sound effects during thunderstorms.
/// Subscribes to ThunderstrikeEvent and creates dramatic visual/audio effects.
/// Can cause damage to entities in open areas during strikes.
/// </summary>
public class ThunderEffects : ScriptBase
{
    private int _thunderstrikeCount = 0;
    private bool _enableDamage = true;

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        LogInfo("Thunder Effects system initialized");

        // Load configuration
        if (Configuration != null)
        {
            _enableDamage = Configuration.GetValueOrDefault("enableWeatherDamage", true);
        }

        // Subscribe to thunder events
        EventBus?.Subscribe<ThunderstrikeEvent>(OnThunderstrike);

        LogInfo($"Subscribed to thunder events (damage: {_enableDamage})");
    }

    public override Task OnDisposedAsync()
    {
        LogInfo($"Thunder Effects shutting down ({_thunderstrikeCount} total strikes)");
        return base.OnDisposedAsync();
    }

    private async void OnThunderstrike(ThunderstrikeEvent evt)
    {
        _thunderstrikeCount++;

        LogInfo($"⚡ THUNDERSTRIKE #{_thunderstrikeCount} at ({evt.StrikePosition.X}, {evt.StrikePosition.Y})!");
        LogInfo($"   Intensity: {evt.Intensity:F2}, Damage: {evt.Damage}, Radius: {evt.AffectRadius}");

        // Create visual lightning flash effect
        await CreateLightningFlash(evt);

        // Play thunder sound with delay based on distance
        await PlayThunderSound(evt);

        // Apply damage to entities if enabled
        if (evt.Damage > 0 && _enableDamage)
        {
            ApplyThunderDamage(evt);
        }

        // Create environmental effects if enabled
        if (evt.CausesEnvironmentalEffects)
        {
            CreateEnvironmentalEffects(evt);
        }

        // Create ground scorch mark at strike position
        CreateScorchMark(evt.StrikePosition.X, evt.StrikePosition.Y);
    }

    private async Task CreateLightningFlash(ThunderstrikeEvent evt)
    {
        // In real implementation, this would:
        // 1. Create bright white flash overlay
        // 2. Draw lightning bolt from sky to strike position
        // 3. Flash intensity based on event intensity
        // 4. Brief screen shake

        LogInfo($"⚡ Lightning flash at ({evt.StrikePosition.X}, {evt.StrikePosition.Y})");

        // Example: Would call game rendering system
        // Renderer.CreateFlash(Color.White, evt.Intensity);
        // Renderer.DrawLightningBolt(
        //     new Vector2(evt.StrikePosition.X, 0),
        //     new Vector2(evt.StrikePosition.X, evt.StrikePosition.Y),
        //     evt.Intensity
        // );
        // Camera.Shake(evt.Intensity * 5.0f, 0.2f);

        // Flash duration based on intensity
        int flashDurationMs = (int)(evt.Intensity * 200); // 0-200ms

        LogInfo($"Flash duration: {flashDurationMs}ms");

        await Task.Delay(flashDurationMs);

        // Example: Would fade out flash
        // Renderer.FadeOutFlash(50);
    }

    private async Task PlayThunderSound(ThunderstrikeEvent evt)
    {
        // Simulate sound travel delay
        // Lightning is instant, thunder sound takes time based on distance

        // For simplicity, use a small random delay to simulate distance
        var random = new Random();
        int delayMs = random.Next(100, 500); // 0.1-0.5 seconds

        await Task.Delay(delayMs);

        LogInfo($"💥 THUNDER! (delay: {delayMs}ms)");

        // In real implementation, would:
        // 1. Calculate distance from player to strike
        // 2. Delay sound based on distance
        // 3. Adjust volume based on distance
        // 4. Play different thunder sound variations

        float volume = evt.Intensity * 0.8f; // Loud but not overwhelming

        // Example: Would call game audio system
        // AudioManager.PlaySound("thunder_crack", volume);

        LogInfo($"Thunder sound played at volume {volume:F2}");
    }

    private void ApplyThunderDamage(ThunderstrikeEvent evt)
    {
        // In real implementation, would:
        // 1. Find all entities within affect radius of strike position
        // 2. Check if entities are in open areas (not under shelter)
        // 3. Apply damage to exposed entities
        // 4. Apply status effects (paralysis?)

        LogInfo($"Applying {evt.Damage} thunder damage in radius {evt.AffectRadius}");

        // Example: Would query game entity system
        // var entities = EntityManager.GetEntitiesInRadius(
        //     evt.StrikePosition.X,
        //     evt.StrikePosition.Y,
        //     evt.AffectRadius
        // );
        //
        // foreach (var entity in entities)
        // {
        //     if (!entity.IsUnderShelter())
        //     {
        //         entity.TakeDamage(evt.Damage, DamageType.Electric);
        //         entity.ApplyStatusEffect(StatusEffect.Paralysis, 0.3f);
        //     }
        // }

        // For now, just log hypothetical damage
        LogInfo($"Thunder damaged entities at ({evt.StrikePosition.X}, {evt.StrikePosition.Y})");
    }

    private void CreateEnvironmentalEffects(ThunderstrikeEvent evt)
    {
        LogInfo("Creating environmental effects from lightning strike");

        // In real implementation, could:
        // 1. Start fires on flammable tiles
        // 2. Damage/destroy certain objects
        // 3. Charge electrical objects
        // 4. Scare away wild Pokémon temporarily

        var random = new Random();

        // Small chance to start a fire
        if (random.NextDouble() < 0.2) // 20% chance
        {
            LogInfo($"⚠️  Lightning started a fire at ({evt.StrikePosition.X}, {evt.StrikePosition.Y})!");

            // Example: Would call fire system
            // FireManager.StartFire(evt.StrikePosition.X, evt.StrikePosition.Y, evt.Intensity);
        }

        // Scare away wild Pokémon in the area
        if (random.NextDouble() < 0.5) // 50% chance
        {
            LogInfo("Wild Pokémon fled from the thunderstrike");

            // Example: Would affect encounter system
            // EncounterManager.ClearEncountersInRadius(
            //     evt.StrikePosition.X,
            //     evt.StrikePosition.Y,
            //     evt.AffectRadius * 2
            // );
        }
    }

    private void CreateScorchMark(int x, int y)
    {
        // In real implementation, would:
        // 1. Add a dark scorch mark sprite to the tile
        // 2. Scorch mark fades over time
        // 3. Multiple strikes in same area make darker marks

        LogInfo($"Scorch mark created at ({x}, {y})");

        // Example: Would call game map/tile system
        // MapManager.GetTile(x, y).AddEffect("scorch_mark");
        //
        // // Schedule scorch mark fade
        // Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => {
        //     MapManager.GetTile(x, y).RemoveEffect("scorch_mark");
        // });
    }

    /// <summary>
    /// Get total number of thunderstrikes that have occurred.
    /// </summary>
    public int GetThunderstrikeCount() => _thunderstrikeCount;

    /// <summary>
    /// Manually trigger a thunderstrike at a specific position (for testing/items).
    /// </summary>
    public void TriggerThunderstrike(int x, int y, int damage = 10)
    {
        var evt = new ThunderstrikeEvent
        {
            WeatherType = "Thunder",
            Intensity = 1.0f,
            StrikePosition = (x, y),
            Damage = damage,
            AffectRadius = 2,
            CausesEnvironmentalEffects = true
        };

        EventBus?.Publish(evt);

        LogInfo($"Manual thunderstrike triggered at ({x}, {y})");
    }
}

// Instantiate and return the thunder effects handler
return new ThunderEffects();
