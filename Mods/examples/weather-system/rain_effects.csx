#r "PokeSharp.Engine.Core.dll"
#load "events/WeatherEvents.csx"

using PokeSharp.Engine.Core.Events;
using PokeSharp.Engine.Core.Scripting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Handles visual and audio effects for rain weather.
/// Subscribes to RainStartedEvent and RainStoppedEvent.
/// Creates puddles on walkable tiles and manages rain particle effects.
/// </summary>
public class RainEffects : ScriptBase
{
    private bool _isRaining = false;
    private float _rainIntensity = 0.0f;
    private HashSet<(int X, int Y)> _puddlePositions = new HashSet<(int, int)>();
    private DateTime _rainStoppedTime;

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        LogInfo("Rain Effects system initialized");

        // Subscribe to rain events
        EventBus?.Subscribe<RainStartedEvent>(OnRainStarted);
        EventBus?.Subscribe<RainStoppedEvent>(OnRainStopped);
        EventBus?.Subscribe<SunshineEvent>(OnSunshine);

        LogInfo("Subscribed to rain weather events");
    }

    public override Task OnDisposedAsync()
    {
        LogInfo("Rain Effects system shutting down");

        // Clean up effects
        if (_isRaining)
        {
            StopRainEffects();
        }

        ClearAllPuddles();

        return base.OnDisposedAsync();
    }

    private void OnRainStarted(RainStartedEvent evt)
    {
        LogInfo($"Rain started! Intensity: {evt.Intensity:F2}, Duration: {evt.DurationSeconds}s");

        _isRaining = true;
        _rainIntensity = evt.Intensity;

        // Start visual rain effects
        StartRainEffects(evt.Intensity);

        // Play rain sound
        PlayRainSound(evt.Intensity);

        // Create puddles if enabled
        if (evt.CreatePuddles)
        {
            StartCreatingPuddles(evt.Intensity);
        }

        // Log thunder capability
        if (evt.CanThunder)
        {
            LogInfo("Thunderstorm possible during this rain");
        }
    }

    private void OnRainStopped(RainStoppedEvent evt)
    {
        LogInfo($"Rain stopped. Puddles persist: {evt.PersistPuddles}");

        _isRaining = false;
        _rainIntensity = 0.0f;
        _rainStoppedTime = DateTime.UtcNow;

        // Stop rain effects
        StopRainEffects();

        // Stop rain sound
        StopRainSound();

        // Handle puddle persistence
        if (evt.PersistPuddles)
        {
            SchedulePuddleEvaporation(evt.PuddleEvaporationSeconds);
        }
        else
        {
            ClearAllPuddles();
        }
    }

    private void OnSunshine(SunshineEvent evt)
    {
        // Accelerate puddle evaporation in sunshine
        if (evt.AcceleratesEvaporation && _puddlePositions.Count > 0)
        {
            LogInfo("Sunshine accelerating puddle evaporation");

            // Reduce evaporation time
            int acceleratedTime = Math.Max(30, 120 - (int)(evt.Intensity * 60));
            SchedulePuddleEvaporation(acceleratedTime);
        }
    }

    private void StartRainEffects(float intensity)
    {
        // In a real implementation, this would:
        // 1. Create particle systems for rain droplets
        // 2. Adjust particle count based on intensity
        // 3. Add splash effects when droplets hit ground
        // 4. Darken the lighting/sky

        LogInfo($"Starting rain particle effects (intensity: {intensity:F2})");

        // Calculate particle count based on intensity
        int particleCount = (int)(intensity * 500); // 0-500 particles

        // Example: Would call game engine's particle system
        // ParticleSystem.Create("rain_droplets", particleCount);
        // ParticleSystem.SetVelocity(new Vector2(0, -5 * intensity));
        // Lighting.SetBrightness(1.0f - intensity * 0.3f);

        LogInfo($"Rain particles created: {particleCount}");
    }

    private void StopRainEffects()
    {
        LogInfo("Stopping rain particle effects");

        // Example: Would call game engine
        // ParticleSystem.Destroy("rain_droplets");
        // Lighting.SetBrightness(1.0f);

        LogInfo("Rain visual effects stopped");
    }

    private void PlayRainSound(float intensity)
    {
        // In a real implementation, this would:
        // 1. Load rain ambient sound
        // 2. Adjust volume based on intensity
        // 3. Loop the sound

        LogInfo($"Playing rain sound at volume {intensity:F2}");

        // Example: Would call game audio system
        // AudioManager.PlayAmbient("rain_loop", intensity);
    }

    private void StopRainSound()
    {
        LogInfo("Stopping rain sound");

        // Example: Would call game audio system
        // AudioManager.StopAmbient("rain_loop");
    }

    private void StartCreatingPuddles(float intensity)
    {
        // Create puddles on walkable tiles over time
        // More intense rain = more puddles, faster

        int puddleCount = (int)(intensity * 20); // 0-20 puddles

        LogInfo($"Creating {puddleCount} puddles");

        var random = new Random();

        for (int i = 0; i < puddleCount; i++)
        {
            // In real implementation, would check if tile is walkable
            // For now, just create random positions
            int x = random.Next(0, 100);
            int y = random.Next(0, 100);

            CreatePuddle(x, y);
        }
    }

    private void CreatePuddle(int x, int y)
    {
        var position = (x, y);

        if (_puddlePositions.Contains(position))
        {
            return; // Puddle already exists
        }

        _puddlePositions.Add(position);

        // In real implementation, would:
        // 1. Check if tile at (x,y) is walkable
        // 2. Add puddle sprite/animation to tile
        // 3. Modify tile properties (slippery, splash effect)

        LogInfo($"Puddle created at ({x}, {y})");

        // Example: Would call game map/tile system
        // MapManager.GetTile(x, y).AddEffect("puddle");
    }

    private void RemovePuddle(int x, int y)
    {
        var position = (x, y);

        if (!_puddlePositions.Contains(position))
        {
            return;
        }

        _puddlePositions.Remove(position);

        LogInfo($"Puddle removed at ({x}, {y})");

        // Example: Would call game map/tile system
        // MapManager.GetTile(x, y).RemoveEffect("puddle");
    }

    private void ClearAllPuddles()
    {
        int puddleCount = _puddlePositions.Count;

        if (puddleCount == 0)
        {
            return;
        }

        LogInfo($"Clearing {puddleCount} puddles");

        foreach (var position in _puddlePositions.ToList())
        {
            RemovePuddle(position.X, position.Y);
        }

        _puddlePositions.Clear();
    }

    private async void SchedulePuddleEvaporation(int seconds)
    {
        LogInfo($"Puddles will evaporate in {seconds} seconds");

        // Gradual evaporation
        int puddleCount = _puddlePositions.Count;

        if (puddleCount == 0)
        {
            return;
        }

        int intervalMs = (seconds * 1000) / puddleCount;

        var random = new Random();
        var puddlesList = _puddlePositions.ToList();

        foreach (var position in puddlesList)
        {
            await Task.Delay(intervalMs);
            RemovePuddle(position.X, position.Y);
        }

        LogInfo("All puddles evaporated");
    }

    /// <summary>
    /// Check if a tile position has a puddle.
    /// Other mods can call this to check for puddles.
    /// </summary>
    public bool HasPuddle(int x, int y)
    {
        return _puddlePositions.Contains((x, y));
    }

    /// <summary>
    /// Get count of active puddles.
    /// </summary>
    public int GetPuddleCount() => _puddlePositions.Count;

    /// <summary>
    /// Get current rain intensity.
    /// </summary>
    public float GetRainIntensity() => _rainIntensity;

    /// <summary>
    /// Check if it's currently raining.
    /// </summary>
    public bool IsRaining() => _isRaining;
}

// Instantiate and return the rain effects handler
return new RainEffects();
