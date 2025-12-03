#r "PokeSharp.Engine.Core.dll"
#load "events/WeatherEvents.csx"

using PokeSharp.Engine.Core.Events;
using PokeSharp.Engine.Core.Scripting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Modifies Pokémon encounter rates and types based on current weather.
/// Subscribes to all weather events and adjusts spawns dynamically.
///
/// Weather Effects:
/// - Rain: Water-type spawns increase 1.5x
/// - Thunder: Electric-type spawns increase 2.0x, Water-types increase 1.3x
/// - Snow: Ice-type spawns increase 2.0x, Steel-types increase 1.2x
/// - Sunshine: Grass-type and Fire-type spawns increase 1.4x
/// - Clear: Normal spawn rates (baseline)
/// </summary>
public class WeatherEncounters : ScriptBase
{
    private string _currentWeather = "Clear";
    private Dictionary<string, float> _typeMultipliers = new Dictionary<string, float>();
    private float _globalMultiplier = 1.0f;

    // Configuration
    private float _weatherEncounterMultiplier = 1.5f;

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        LogInfo("Weather Encounters system initialized");

        // Load configuration
        if (Configuration != null)
        {
            _weatherEncounterMultiplier = Configuration.GetValueOrDefault("weatherEncounterMultiplier", 1.5f);
        }

        // Subscribe to all weather events
        EventBus?.Subscribe<RainStartedEvent>(OnRainStarted);
        EventBus?.Subscribe<RainStoppedEvent>(OnRainStopped);
        EventBus?.Subscribe<ThunderstrikeEvent>(OnThunderstrike);
        EventBus?.Subscribe<SnowStartedEvent>(OnSnowStarted);
        EventBus?.Subscribe<SunshineEvent>(OnSunshine);
        EventBus?.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
        EventBus?.Subscribe<WeatherClearedEvent>(OnWeatherCleared);

        // Initialize with clear weather multipliers
        ResetMultipliers();

        LogInfo($"Subscribed to weather events (multiplier: {_weatherEncounterMultiplier})");
    }

    public override Task OnDisposedAsync()
    {
        LogInfo("Weather Encounters system shutting down");
        ResetMultipliers();
        return base.OnDisposedAsync();
    }

    private void OnRainStarted(RainStartedEvent evt)
    {
        LogInfo($"Rain weather active - adjusting encounters (intensity: {evt.Intensity:F2})");

        _currentWeather = "Rain";
        ResetMultipliers();

        // Water types spawn more in rain
        float waterMultiplier = 1.0f + (evt.Intensity * _weatherEncounterMultiplier);
        _typeMultipliers["Water"] = waterMultiplier;

        // Bug types also more active in rain
        _typeMultipliers["Bug"] = 1.0f + (evt.Intensity * 0.3f);

        // Fire types less common in rain
        _typeMultipliers["Fire"] = 1.0f - (evt.Intensity * 0.5f);

        ApplyEncounterMultipliers();

        LogInfo($"Water-type encounter rate: {waterMultiplier:F2}x");
    }

    private void OnRainStopped(RainStoppedEvent evt)
    {
        LogInfo("Rain stopped - resetting encounter rates");

        if (_currentWeather == "Rain" || _currentWeather == "Thunder")
        {
            _currentWeather = "Clear";
            ResetMultipliers();
            ApplyEncounterMultipliers();
        }
    }

    private void OnThunderstrike(ThunderstrikeEvent evt)
    {
        // Thunder events indicate thunderstorm weather
        if (_currentWeather != "Thunder")
        {
            LogInfo("Thunderstorm active - electric types surging!");

            _currentWeather = "Thunder";
            ResetMultipliers();

            // Electric types much more common in thunderstorms
            float electricMultiplier = 1.0f + (_weatherEncounterMultiplier * 1.5f); // 2.25x default
            _typeMultipliers["Electric"] = electricMultiplier;

            // Water types still boosted
            _typeMultipliers["Water"] = 1.0f + (_weatherEncounterMultiplier * 0.5f);

            // Flying types avoid thunderstorms
            _typeMultipliers["Flying"] = 0.4f;

            // Fire types very rare
            _typeMultipliers["Fire"] = 0.2f;

            ApplyEncounterMultipliers();

            LogInfo($"Electric-type encounter rate: {electricMultiplier:F2}x");
        }
    }

    private void OnSnowStarted(SnowStartedEvent evt)
    {
        LogInfo($"Snow weather active - ice types appearing (intensity: {evt.Intensity:F2})");

        _currentWeather = "Snow";
        ResetMultipliers();

        // Ice types much more common in snow
        float iceMultiplier = 1.0f + (evt.Intensity * _weatherEncounterMultiplier * 1.5f);
        _typeMultipliers["Ice"] = iceMultiplier;

        // Steel types more common in snow
        _typeMultipliers["Steel"] = 1.0f + (evt.Intensity * 0.3f);

        // Water types can appear (as ice-water types)
        _typeMultipliers["Water"] = 1.0f + (evt.Intensity * 0.2f);

        // Fire, Grass, Bug types much less common
        _typeMultipliers["Fire"] = 0.3f;
        _typeMultipliers["Grass"] = 0.5f;
        _typeMultipliers["Bug"] = 0.2f;

        ApplyEncounterMultipliers();

        LogInfo($"Ice-type encounter rate: {iceMultiplier:F2}x");
    }

    private void OnSunshine(SunshineEvent evt)
    {
        LogInfo($"Sunshine weather active - fire and grass types thriving (intensity: {evt.Intensity:F2})");

        _currentWeather = "Sunshine";
        ResetMultipliers();

        // Fire types more common in intense sunshine
        float fireMultiplier = 1.0f + (evt.Intensity * _weatherEncounterMultiplier * 0.8f);
        _typeMultipliers["Fire"] = fireMultiplier;

        // Grass types boosted if configured
        if (evt.BoostsGrassTypes)
        {
            float grassMultiplier = 1.0f + (evt.Intensity * _weatherEncounterMultiplier * 0.7f);
            _typeMultipliers["Grass"] = grassMultiplier;
        }

        // Bug types more active in sunshine
        _typeMultipliers["Bug"] = 1.0f + (evt.Intensity * 0.3f);

        // Water and Ice types less common
        _typeMultipliers["Water"] = 0.6f;
        _typeMultipliers["Ice"] = 0.3f;

        ApplyEncounterMultipliers();

        LogInfo($"Fire-type encounter rate: {fireMultiplier:F2}x");
    }

    private void OnWeatherChanged(WeatherChangedEvent evt)
    {
        LogInfo($"Weather changed: {evt.PreviousWeather ?? "None"} -> {evt.NewWeather}");

        // The specific weather events will handle multiplier changes
        // This is just for logging and tracking
    }

    private void OnWeatherCleared(WeatherClearedEvent evt)
    {
        LogInfo($"Weather cleared: {evt.ClearedWeather}");

        _currentWeather = "Clear";
        ResetMultipliers();
        ApplyEncounterMultipliers();
    }

    private void ResetMultipliers()
    {
        _typeMultipliers.Clear();
        _globalMultiplier = 1.0f;
    }

    private void ApplyEncounterMultipliers()
    {
        // In real implementation, would:
        // 1. Access the game's encounter system
        // 2. Modify spawn rates for each Pokémon type
        // 3. Update encounter tables based on multipliers

        LogInfo($"Applying encounter multipliers for {_currentWeather} weather:");

        foreach (var kvp in _typeMultipliers)
        {
            string pokemonType = kvp.Key;
            float multiplier = kvp.Value;

            LogInfo($"  {pokemonType} type: {multiplier:F2}x");

            // Example: Would call game encounter system
            // EncounterManager.SetTypeMultiplier(pokemonType, multiplier);
        }

        // Example: Would update encounter tables
        // EncounterManager.RefreshEncounterTables();

        LogInfo($"Encounter multipliers applied for {_typeMultipliers.Count} types");
    }

    /// <summary>
    /// Get the current spawn rate multiplier for a specific Pokémon type.
    /// </summary>
    public float GetTypeMultiplier(string pokemonType)
    {
        if (_typeMultipliers.TryGetValue(pokemonType, out float multiplier))
        {
            return multiplier;
        }

        return 1.0f; // Default multiplier
    }

    /// <summary>
    /// Get all active type multipliers.
    /// </summary>
    public Dictionary<string, float> GetAllMultipliers()
    {
        return new Dictionary<string, float>(_typeMultipliers);
    }

    /// <summary>
    /// Get current weather affecting encounters.
    /// </summary>
    public string GetCurrentWeather() => _currentWeather;

    /// <summary>
    /// Calculate effective spawn chance for a Pokémon type.
    /// </summary>
    public float CalculateSpawnChance(string pokemonType, float baseChance)
    {
        float multiplier = GetTypeMultiplier(pokemonType);
        return baseChance * multiplier * _globalMultiplier;
    }

    /// <summary>
    /// Manually set a type multiplier (for testing or special events).
    /// </summary>
    public void SetTypeMultiplier(string pokemonType, float multiplier)
    {
        _typeMultipliers[pokemonType] = multiplier;
        LogInfo($"Manual multiplier set: {pokemonType} = {multiplier:F2}x");
        ApplyEncounterMultipliers();
    }
}

// Instantiate and return the weather encounters handler
return new WeatherEncounters();
