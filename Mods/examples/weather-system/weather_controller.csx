#r "PokeSharp.Engine.Core.dll"
#load "events/WeatherEvents.csx"

using PokeSharp.Engine.Core.Events;
using PokeSharp.Engine.Core.Scripting;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Central weather controller that manages dynamic weather changes over time.
/// Publishes weather events that other mods can subscribe to.
///
/// Configuration:
/// - weatherChangeDurationMinutes: How often weather changes (default: 5)
/// - thunderProbabilityDuringRain: Chance of thunder during rain (default: 0.3)
/// - snowProbabilityInWinter: Chance of snow in winter months (default: 0.6)
/// </summary>
public class WeatherController : ScriptBase
{
    private string? _currentWeather;
    private DateTime _lastWeatherChange;
    private Random _random = new Random();
    private CancellationTokenSource? _weatherLoopCancellation;
    private Task? _weatherLoopTask;

    // Weather types
    private static readonly string[] WeatherTypes = { "Clear", "Rain", "Thunder", "Snow", "Sunshine", "Fog" };

    // Configuration (loaded from mod.json)
    private int _changeDurationMinutes = 5;
    private float _thunderProbability = 0.3f;
    private float _snowProbabilityWinter = 0.6f;

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        LogInfo("Weather Controller initialized");

        // Load configuration
        LoadConfiguration();

        // Set initial weather
        SetInitialWeather();

        // Start weather loop
        StartWeatherLoop();
    }

    public override Task OnDisposedAsync()
    {
        LogInfo("Weather Controller shutting down");
        StopWeatherLoop();
        return base.OnDisposedAsync();
    }

    private void LoadConfiguration()
    {
        if (Configuration != null)
        {
            _changeDurationMinutes = Configuration.GetValueOrDefault("weatherChangeDurationMinutes", 5);
            _thunderProbability = Configuration.GetValueOrDefault("thunderProbabilityDuringRain", 0.3f);
            _snowProbabilityWinter = Configuration.GetValueOrDefault("snowProbabilityInWinter", 0.6f);

            LogInfo($"Configuration loaded: Change every {_changeDurationMinutes} minutes");
        }
    }

    private void SetInitialWeather()
    {
        // Start with clear weather
        _currentWeather = "Clear";
        _lastWeatherChange = DateTime.UtcNow;

        // Publish initial sunshine event
        PublishWeatherEvent(new SunshineEvent
        {
            WeatherType = "Sunshine",
            Intensity = 0.7f,
            DurationSeconds = _changeDurationMinutes * 60
        });

        LogInfo("Initial weather set to Clear/Sunshine");
    }

    private void StartWeatherLoop()
    {
        _weatherLoopCancellation = new CancellationTokenSource();

        _weatherLoopTask = Task.Run(async () =>
        {
            while (!_weatherLoopCancellation.Token.IsCancellationRequested)
            {
                try
                {
                    // Check if it's time to change weather
                    var timeSinceLastChange = DateTime.UtcNow - _lastWeatherChange;

                    if (timeSinceLastChange.TotalMinutes >= _changeDurationMinutes)
                    {
                        ChangeWeather();
                    }

                    // Check for thunder during rain
                    if (_currentWeather == "Rain" && _random.NextDouble() < 0.1) // 10% check per loop
                    {
                        TriggerThunderstrike();
                    }

                    // Wait before next check (10 seconds)
                    await Task.Delay(10000, _weatherLoopCancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogError($"Error in weather loop: {ex.Message}");
                }
            }
        }, _weatherLoopCancellation.Token);
    }

    private void StopWeatherLoop()
    {
        if (_weatherLoopCancellation != null)
        {
            _weatherLoopCancellation.Cancel();
            _weatherLoopTask?.Wait(TimeSpan.FromSeconds(2));
            _weatherLoopCancellation.Dispose();
        }
    }

    private void ChangeWeather()
    {
        string? previousWeather = _currentWeather;
        string newWeather = SelectNewWeather();

        // Stop previous weather
        if (previousWeather != null && previousWeather != "Clear")
        {
            StopWeather(previousWeather);
        }

        // Start new weather
        _currentWeather = newWeather;
        _lastWeatherChange = DateTime.UtcNow;

        StartWeather(newWeather);

        // Publish weather changed event
        PublishWeatherEvent(new WeatherChangedEvent
        {
            PreviousWeather = previousWeather,
            NewWeather = newWeather,
            IsNaturalTransition = true
        });

        LogInfo($"Weather changed: {previousWeather ?? "None"} -> {newWeather}");
    }

    private string SelectNewWeather()
    {
        // Consider seasonal factors
        int month = DateTime.UtcNow.Month;
        bool isWinter = month == 12 || month == 1 || month == 2;
        bool isSummer = month >= 6 && month <= 8;

        // Weight different weather types
        double roll = _random.NextDouble();

        if (isWinter && roll < _snowProbabilityWinter)
        {
            return "Snow";
        }
        else if (isSummer && roll < 0.4)
        {
            return "Sunshine";
        }
        else if (roll < 0.5)
        {
            return "Rain";
        }
        else if (roll < 0.7)
        {
            return "Sunshine";
        }
        else if (roll < 0.85)
        {
            return "Clear";
        }
        else
        {
            return "Fog";
        }
    }

    private void StartWeather(string weather)
    {
        int duration = _changeDurationMinutes * 60;
        float intensity = 0.5f + (float)_random.NextDouble() * 0.5f; // 0.5-1.0

        switch (weather)
        {
            case "Rain":
                PublishWeatherEvent(new RainStartedEvent
                {
                    WeatherType = "Rain",
                    Intensity = intensity,
                    DurationSeconds = duration,
                    CreatePuddles = true,
                    CanThunder = _random.NextDouble() < _thunderProbability
                });
                break;

            case "Thunder":
                // Thunder is treated as heavy rain with thunder enabled
                PublishWeatherEvent(new RainStartedEvent
                {
                    WeatherType = "Thunder",
                    Intensity = 0.9f,
                    DurationSeconds = duration,
                    CreatePuddles = true,
                    CanThunder = true
                });
                break;

            case "Snow":
                PublishWeatherEvent(new SnowStartedEvent
                {
                    WeatherType = "Snow",
                    Intensity = intensity,
                    DurationSeconds = duration,
                    AccumulatesOnGround = true,
                    AccumulationRate = intensity * 0.5f,
                    MaxDepthLayers = 3,
                    CreatesIcyTerrain = intensity > 0.7f
                });
                break;

            case "Sunshine":
                PublishWeatherEvent(new SunshineEvent
                {
                    WeatherType = "Sunshine",
                    Intensity = intensity,
                    DurationSeconds = duration,
                    BrightnessMultiplier = 1.0f + intensity * 0.5f,
                    AcceleratesEvaporation = true,
                    BoostsGrassTypes = true,
                    TemperatureBonus = intensity * 10.0f
                });
                break;

            case "Clear":
            case "Fog":
                // These don't have specific start events yet
                LogInfo($"Weather set to {weather}");
                break;
        }
    }

    private void StopWeather(string weather)
    {
        switch (weather)
        {
            case "Rain":
            case "Thunder":
                PublishWeatherEvent(new RainStoppedEvent
                {
                    WeatherType = weather,
                    Intensity = 0.0f,
                    PersistPuddles = true,
                    PuddleEvaporationSeconds = 120
                });
                break;

            default:
                PublishWeatherEvent(new WeatherClearedEvent
                {
                    ClearedWeather = weather,
                    ImmediateCleanup = false
                });
                break;
        }
    }

    private void TriggerThunderstrike()
    {
        // Random position for lightning (would normally use actual map bounds)
        int x = _random.Next(0, 100);
        int y = _random.Next(0, 100);

        bool enableDamage = Configuration?.GetValueOrDefault("enableWeatherDamage", true) ?? true;

        PublishWeatherEvent(new ThunderstrikeEvent
        {
            WeatherType = "Thunder",
            Intensity = 1.0f,
            StrikePosition = (x, y),
            Damage = enableDamage ? 10 : 0,
            AffectRadius = 2,
            CausesEnvironmentalEffects = enableDamage
        });

        LogInfo($"Thunder struck at ({x}, {y})!");
    }

    private void PublishWeatherEvent(IGameEvent weatherEvent)
    {
        // Publish to event bus
        EventBus?.Publish(weatherEvent);
    }

    /// <summary>
    /// Public method to manually change weather (can be called by other scripts).
    /// </summary>
    public void SetWeather(string weatherType, int durationSeconds = -1)
    {
        if (!WeatherTypes.Contains(weatherType))
        {
            LogWarning($"Unknown weather type: {weatherType}");
            return;
        }

        string? previousWeather = _currentWeather;

        if (previousWeather != null && previousWeather != "Clear")
        {
            StopWeather(previousWeather);
        }

        _currentWeather = weatherType;
        _lastWeatherChange = DateTime.UtcNow;

        if (durationSeconds > 0)
        {
            _changeDurationMinutes = durationSeconds / 60;
        }

        StartWeather(weatherType);

        PublishWeatherEvent(new WeatherChangedEvent
        {
            PreviousWeather = previousWeather,
            NewWeather = weatherType,
            IsNaturalTransition = false
        });

        LogInfo($"Weather manually set to {weatherType}");
    }

    /// <summary>
    /// Get current weather type.
    /// </summary>
    public string? GetCurrentWeather() => _currentWeather;
}

// Instantiate and return the controller
return new WeatherController();
