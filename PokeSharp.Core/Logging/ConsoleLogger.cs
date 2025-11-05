using Microsoft.Extensions.Logging;

namespace PokeSharp.Core.Logging;

/// <summary>
///     Simple console logger implementation for systems without dependency injection.
///     Provides basic console output for debugging and development.
/// </summary>
/// <typeparam name="T">Type being logged</typeparam>
public sealed class ConsoleLogger<T> : ILogger<T>
{
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;

    public ConsoleLogger(LogLevel minLevel = LogLevel.Information)
    {
        _categoryName = typeof(T).Name;
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLevelStr = GetLogLevelString(logLevel);

        Console.WriteLine($"[{timestamp}] [{logLevelStr}] {_categoryName}: {message}");

        if (exception != null)
        {
            Console.WriteLine($"  Exception: {exception.Message}");
            if (logLevel >= LogLevel.Debug)
                Console.WriteLine($"  StackTrace: {exception.StackTrace}");
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT ",
            _ => "NONE ",
        };
    }
}

/// <summary>
///     Factory for creating console loggers without DI.
/// </summary>
public static class ConsoleLoggerFactory
{
    /// <summary>
    ///     Create a console logger for the specified type.
    /// </summary>
    /// <typeparam name="T">Type to create logger for</typeparam>
    /// <param name="minLevel">Minimum log level to output</param>
    /// <returns>Console logger instance</returns>
    public static ILogger<T> Create<T>(LogLevel minLevel = LogLevel.Information)
    {
        return new ConsoleLogger<T>(minLevel);
    }
}
