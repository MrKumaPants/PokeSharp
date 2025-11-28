# Logging Framework Migration Guide

## Overview

This guide documents the migration from the custom console logger (using Spectre.Console) to **Serilog**, an industry-standard structured logging framework for .NET.

## Changes Made

### 1. Package Updates

**PokeSharp.Engine.Common.csproj:**
- ✅ Added Serilog 4.2.0
- ✅ Added Serilog.Extensions.Logging 8.0.0
- ✅ Added Serilog.Sinks.Console 6.0.0
- ✅ Added Serilog.Sinks.File 6.0.0
- ✅ Added Serilog.Sinks.Async 2.1.0
- ✅ Added Serilog.Enrichers.Thread 4.0.0
- ✅ Added Serilog.Enrichers.Environment 3.0.1
- ⚠️ Kept Spectre.Console (still used for potential UI elements, NOT for logging)

**PokeSharp.Game.csproj:**
- ✅ Added Microsoft.Extensions.Configuration 9.0.10
- ✅ Added Microsoft.Extensions.Configuration.Json 9.0.10
- ✅ Added Microsoft.Extensions.Configuration.EnvironmentVariables 9.0.10
- ✅ Added Serilog.Settings.Configuration 8.0.4

### 2. New Configuration Files

Created in `PokeSharp.Game/Config/`:

**appsettings.json** (Production settings):
- Default log level: Information
- Console sink with async writes
- File sink: `logs/pokesharp-.log`, 10MB max, 7 days retention
- Structured output templates
- Thread and machine enrichers

**appsettings.Development.json** (Development settings):
- Default log level: Debug
- More verbose output for all PokeSharp namespaces
- File sink: `logs/pokesharp-dev-.log`, 50MB max, 3 days retention
- Detailed debugging information

### 3. New Logging Infrastructure

**SerilogConfiguration.cs**:
- Centralized Serilog configuration
- Supports both configuration-based (appsettings.json) and programmatic setup
- Environment-aware configuration (Development vs Production)
- Creates ILoggerFactory instances for dependency injection

**GameLoggingExtensions.cs**:
- Game-specific structured logging extensions
- `LogEntityCreated()` - Logs entity creation with metadata
- `LogComponentAdded()` - Logs component additions
- `LogSystemExecution()` - Logs system timing with warnings for slow operations
- `LogWorkflow()` - Replaces LogWorkflowStatus with structured logging
- `BeginTimedOperation()` - Scoped timing with automatic logging
- `LogExceptionWithContext()` - Enhanced exception logging with context
- `LogAssetLoaded()` - Asset loading metrics

### 4. ServiceCollectionExtensions Updates

**Changes to `AddGameServices()`**:
```csharp
public static IServiceCollection AddGameServices(
    this IServiceCollection services,
    IConfiguration? configuration = null,
    string environment = "Production"
)
```

- Added optional `configuration` parameter for appsettings.json
- Added optional `environment` parameter (defaults to "Production")
- Calls new `ConfigureLogging()` method to set up Serilog
- Replaces all logging providers with Serilog

## Migration Steps for Code

### Step 1: Update Entry Point (Program.cs or Main)

**Before:**
```csharp
var services = new ServiceCollection();
services.AddGameServices();
```

**After:**
```csharp
var services = new ServiceCollection();

// Load configuration
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";

var basePath = AppDomain.CurrentDomain.BaseDirectory;
var configuration = SerilogConfiguration.LoadConfiguration(basePath, environment);

// Add services with logging configuration
services.AddGameServices(configuration, environment);
```

### Step 2: Replace Custom Logging Calls

**Old Pattern (Custom Logger):**
```csharp
logger?.LogInformation("WF  Template JSON loaded | count: {Count}, source: base", count);
```

**New Pattern (Serilog Structured):**
```csharp
logger?.LogWorkflow("Template JSON loaded", "completed", new Dictionary<string, object>
{
    ["Count"] = count,
    ["Source"] = "base"
});
```

### Step 3: Use Structured Logging for Entities

**Old Pattern:**
```csharp
logger?.LogInformation("Entity created: {EntityId}", entityId);
```

**New Pattern:**
```csharp
logger?.LogEntityCreated(entityId, "NPC", new Dictionary<string, object>
{
    ["Position"] = position,
    ["SpriteId"] = spriteId
});
```

### Step 4: Timed Operations

**Old Pattern:**
```csharp
var sw = Stopwatch.StartNew();
DoWork();
sw.Stop();
logger?.LogInformation("Work completed in {Time}ms", sw.ElapsedMilliseconds);
```

**New Pattern:**
```csharp
using (logger?.BeginTimedOperation("Work"))
{
    DoWork();
}
// Automatically logs elapsed time on disposal
```

### Step 5: Exception Logging

**Old Pattern:**
```csharp
logger?.LogExceptionWithContext(ex, "Operation failed", "arg1", "arg2");
```

**New Pattern:**
```csharp
logger?.LogExceptionWithContext(ex, "Operation failed", new Dictionary<string, object>
{
    ["Argument1"] = "arg1",
    ["Argument2"] = "arg2",
    ["CustomData"] = additionalInfo
});
```

## Configuration

### Log Levels by Namespace

Adjust in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning",
        "PokeSharp.Engine.Systems": "Debug",
        "PokeSharp.Game.Scripting": "Information"
      }
    }
  }
}
```

### Environment Variables

Override log settings using environment variables:

```bash
# Override minimum level
export POKESHARP_Serilog__MinimumLevel__Default=Debug

# Override specific namespace
export POKESHARP_Serilog__MinimumLevel__Override__PokeSharp.Game=Trace

# Change log file path
export POKESHARP_Serilog__WriteTo__1__Args__path=logs/custom-.log
```

### File Rotation

**Production** (appsettings.json):
- Max file size: 10 MB
- Retained files: 7 days
- Rolling interval: Daily

**Development** (appsettings.Development.json):
- Max file size: 50 MB
- Retained files: 3 days
- Rolling interval: Daily

## Benefits of Serilog

### 1. Structured Logging
- Log data as structured objects, not just strings
- Query logs efficiently with tools like Seq, Elasticsearch, Splunk
- Machine-readable format for automated analysis

### 2. Performance
- Async sinks minimize impact on game performance
- Efficient message templates
- Conditional logging based on level

### 3. Flexibility
- Easy to add new sinks (database, cloud services, etc.)
- Rich ecosystem of extensions and integrations
- Configuration can be changed without code changes

### 4. Production Ready
- Industry standard used by thousands of .NET applications
- Well-tested and maintained
- Excellent documentation and community support

## Backward Compatibility

### Deprecated (But Still Work)
The old logging extensions in `LoggerExtensions.cs` still work:
- `LogMemoryStats()`
- `LogMemoryStatsWithCollection()`
- `LogTimed()`

These use standard `ILogger` interface and work with Serilog.

### To Be Removed Eventually
- Custom `ConsoleLogger<T>` class
- Custom `ConsoleLoggerFactoryImpl` class
- `LogFormatting` helpers (Spectre.Console markup)

These are no longer needed but kept for gradual migration.

## Testing

### Development Environment
```bash
cd PokeSharp.Game
export DOTNET_ENVIRONMENT=Development
dotnet run
```

Check logs in:
- Console output (with color formatting)
- `logs/pokesharp-dev-YYYYMMDD.log`

### Production Environment
```bash
cd PokeSharp.Game
export DOTNET_ENVIRONMENT=Production
dotnet run
```

Check logs in:
- Console output (minimal)
- `logs/pokesharp-YYYYMMDD.log`

## Troubleshooting

### Issue: No logs appearing

**Solution:** Ensure configuration is loaded:
```csharp
var config = SerilogConfiguration.LoadConfiguration(basePath, environment);
services.AddGameServices(config, environment);
```

### Issue: Config file not found

**Solution:** Verify files are copied to output:
```xml
<None Include="Config\appsettings.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

### Issue: Too verbose logging

**Solution:** Adjust minimum level in appsettings.json or use environment variable:
```bash
export POKESHARP_Serilog__MinimumLevel__Default=Warning
```

### Issue: Logs not rotating

**Solution:** Check file permissions and ensure rolling interval is set:
```json
{
  "Name": "File",
  "Args": {
    "rollingInterval": "Day",
    "retainedFileCountLimit": 7
  }
}
```

## Next Steps

1. ✅ Update entry point to load configuration
2. ⏳ Gradually migrate custom logging calls to use new extensions
3. ⏳ Remove deprecated console logger classes
4. ⏳ Add additional sinks as needed (e.g., Seq for development)
5. ⏳ Configure log aggregation for production monitoring

## References

- [Serilog Documentation](https://serilog.net/)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Getting-Started)
- [Structured Logging Concepts](https://messagetemplates.org/)
- [Serilog Settings Configuration](https://github.com/serilog/serilog-settings-configuration)
