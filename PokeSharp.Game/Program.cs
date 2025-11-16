using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeSharp.Engine.Common.Logging;
using PokeSharp.Game;

// Ensure glyph-heavy logging renders correctly
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

// Enable Windows ANSI color support (required for Spectre.Console colors via Serilog)
if (OperatingSystem.IsWindows())
{
    const int STD_OUTPUT_HANDLE = -11;
    const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    var handle = GetStdHandle(STD_OUTPUT_HANDLE);
    if (handle != IntPtr.Zero)
    {
        GetConsoleMode(handle, out uint mode);
        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        SetConsoleMode(handle, mode);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}

// Determine environment (Development, Production, etc.)
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";

// Load configuration from appsettings.json
var basePath = AppDomain.CurrentDomain.BaseDirectory;
var configuration = SerilogConfiguration.LoadConfiguration(basePath, environment);

// Setup DI container
var services = new ServiceCollection();

// Add game services with Serilog logging configuration
// This replaces the old ConsoleLoggerFactory setup
services.AddGameServices(configuration, environment);

// Add the game itself
services.AddSingleton<PokeSharpGame>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get logger to log startup
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("PokeSharp starting | environment: {Environment}, config: {BasePath}", environment, basePath);

// Create and run the game
try
{
    using var game = serviceProvider.GetRequiredService<PokeSharpGame>();
    game.Run();
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error during game execution");
    throw;
}
finally
{
    // Ensure all logs are flushed before exit
    Serilog.Log.CloseAndFlush();
}
