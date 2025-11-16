# Event ID Registry

Complete registry of all logging event IDs used in PokeSharp.

## Event ID Ranges

| Range      | Subsystem                  | Status      |
|------------|----------------------------|-------------|
| 1000-1999  | Movement & Collision       | In Use      |
| 2000-2999  | ECS & Entity Processing    | In Use      |
| 3000-3999  | Performance & Metrics      | In Use      |
| 4000-4999  | Asset Loading              | In Use      |
| 5000-5999  | System Initialization      | In Use      |
| 6000-6999  | Memory Management          | In Use      |
| 7000-7999  | Input Processing           | Reserved    |
| 8000-8999  | Rendering                  | Reserved    |
| 9000-9999  | Scripting & Hot Reload     | Reserved    |
| 10000-10999| Networking                 | Reserved    |
| 11000-11999| Audio                      | Reserved    |
| 12000-12999| Save/Load                  | Reserved    |

---

## Movement & Collision (1000-1999)

| Event ID | Level   | Defined In        | Message Template |
|----------|---------|-------------------|------------------|
| 1000     | Debug   | LogMessages.cs    | Movement blocked: out of bounds ({X}, {Y}) for map {MapId} |
| 1001     | Debug   | LogMessages.cs    | Ledge jump blocked: landing out of bounds ({X}, {Y}) |
| 1002     | Debug   | LogMessages.cs    | Ledge jump blocked: landing position blocked ({X}, {Y}) |
| 1003     | Debug   | LogMessages.cs    | Ledge jump: ({StartX}, {StartY}) -> ({EndX}, {EndY}) direction: {Direction} |
| 1004     | Trace   | LogMessages.cs    | Movement blocked by collision at ({X}, {Y}) from direction {Direction} |
| 1005-1099|         | **Reserved for Movement System** | |
| 1100-1199|         | **Reserved for Collision System** | |
| 1200-1299|         | **Reserved for Pathfinding** | |
| 1300-1399|         | **Reserved for Physics** | |

---

## ECS & Entity Processing (2000-2999)

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 2000     | Debug        | LogMessages.cs    | Processing {EntityCount} entities in {SystemName} |
| 2001     | Information  | LogMessages.cs    | Indexed {Count} static tiles into spatial hash |
| 2002     | Information  | LogMessages.cs    | Processing {Count} animated tiles |
| 2003     | Debug        | *Recommended*     | Created {Count} entities from template {TemplateId} |
| 2004     | Debug        | *Recommended*     | Entity {EntityId} returned to pool {PoolType} |
| 2005     | Debug        | *Recommended*     | Entity {EntityId} destroyed on map {MapId} |
| 2006-2099|              | **Reserved for Entity Lifecycle** | |
| 2100-2199|              | **Reserved for Component Operations** | |
| 2200-2299|              | **Reserved for Query Operations** | |
| 2300-2399|              | **Reserved for Spatial Systems** | |

---

## Performance & Metrics (3000-3999)

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 3000     | Warning      | LogMessages.cs    | Slow frame: {FrameTimeMs:F2}ms (target: {TargetMs:F2}ms) |
| 3001     | Warning      | *Recommended*     | Slow system: {SystemName} took {ElapsedMs:F2}ms (threshold: {ThresholdMs:F2}ms) |
| 3002     | Information  | LogMessages.cs    | Performance: Avg frame time: {AvgMs:F2}ms ({Fps:F1} FPS) \| Min: {MinMs:F2}ms \| Max: {MaxMs:F2}ms |
| 3003     | Information  | LogMessages.cs    | System {SystemName} - Avg: {AvgMs:F2}ms \| Max: {MaxMs:F2}ms \| Calls: {UpdateCount} |
| 3004-3099|              | **Reserved for Frame Timing** | |
| 3100-3199|              | **Reserved for System Performance** | |
| 3200-3299|              | **Reserved for Profiling** | |

---

## Asset Loading (4000-4999)

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 4000     | Debug        | LogMessages.cs    | Loaded texture '{TextureId}' in {TimeMs:F2}ms ({Width}x{Height}px) |
| 4001     | Warning      | LogMessages.cs    | Slow texture load: '{TextureId}' took {TimeMs:F2}ms |
| 4002     | Information  | *Recommended*     | Loaded map: {MapName} ({Width}x{Height}) with {TileCount} tiles, {ObjectCount} objects |
| 4003     | Debug        | *Recommended*     | Unloaded texture: {TextureId} |
| 4004     | Debug        | *Recommended*     | Loaded tileset: {TilesetId} ({Width}x{Height}px) with {TileCount} tiles |
| 4005     | Debug        | *Recommended*     | Loaded sprite sheet: {SpriteId} with {FrameCount} frames |
| 4006-4099|              | **Reserved for Texture Loading** | |
| 4100-4199|              | **Reserved for Map Loading** | |
| 4200-4299|              | **Reserved for Sprite Loading** | |
| 4300-4399|              | **Reserved for Audio Loading** | |
| 4400-4499|              | **Reserved for Data Loading** | |

---

## System Initialization (5000-5999)

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 5000     | Information  | LogMessages.cs    | Initializing {Count} systems |
| 5001     | Debug        | LogMessages.cs    | Initializing system: {SystemName} |
| 5002     | Information  | LogMessages.cs    | All systems initialized successfully |
| 5003     | Debug        | LogMessages.cs    | Registered system: {SystemName} (Priority: {Priority}) |
| 5004     | Information  | *Recommended*     | Game initialized in {ElapsedMs:F2}ms |
| 5005     | Error        | *Recommended*     | System initialization failed: {SystemName} \| Reason: {Reason} |
| 5006-5099|              | **Reserved for System Lifecycle** | |
| 5100-5199|              | **Reserved for Service Registration** | |
| 5200-5299|              | **Reserved for Configuration** | |

---

## Memory Management (6000-6999)

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 6000     | Information  | LogMessages.cs    | Memory: {MemoryMb:F2}MB \| GC Collections - Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2} |
| 6001     | Warning      | LogMessages.cs    | High memory usage: {MemoryMb:F2}MB (threshold: {ThresholdMb:F2}MB) |
| 6002     | Debug        | *Recommended*     | Memory after GC: {AfterMb:F2}MB (freed {FreedMb:F2}MB from {BeforeMb:F2}MB) |
| 6003     | Debug        | *Recommended*     | Entity pool created: {PoolType} with capacity {Capacity} |
| 6004     | Debug        | *Recommended*     | Component pool resized: {ComponentType} from {OldSize} to {NewSize} |
| 6005     | Warning      | *Recommended*     | Memory leak detected: {Count} unreleased {ResourceType} |
| 6006-6099|              | **Reserved for Memory Tracking** | |
| 6100-6199|              | **Reserved for Pool Management** | |
| 6200-6299|              | **Reserved for Garbage Collection** | |

---

## Input Processing (7000-7999) - Reserved

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 7000-7099|              | **Reserved for Input State** | |
| 7100-7199|              | **Reserved for Input Buffer** | |
| 7200-7299|              | **Reserved for Input Mapping** | |

---

## Rendering (8000-8999) - Reserved

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 8000-8099|              | **Reserved for Render Pipeline** | |
| 8100-8199|              | **Reserved for Sprite Rendering** | |
| 8200-8299|              | **Reserved for Tile Rendering** | |
| 8300-8399|              | **Reserved for Camera System** | |
| 8400-8499|              | **Reserved for Particle Effects** | |

---

## Scripting & Hot Reload (9000-9999) - Reserved

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 9000-9099|              | **Reserved for Script Compilation** | |
| 9100-9199|              | **Reserved for Script Execution** | |
| 9200-9299|              | **Reserved for Hot Reload** | |
| 9300-9399|              | **Reserved for File Watching** | |

---

## Networking (10000-10999) - Reserved

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 10000-10099|            | **Reserved for Connection Management** | |
| 10100-10199|            | **Reserved for Packet Processing** | |
| 10200-10299|            | **Reserved for Synchronization** | |

---

## Audio (11000-11999) - Reserved

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 11000-11099|            | **Reserved for Music System** | |
| 11100-11199|            | **Reserved for Sound Effects** | |
| 11200-11299|            | **Reserved for Audio Mixing** | |

---

## Save/Load (12000-12999) - Reserved

| Event ID | Level        | Defined In        | Message Template |
|----------|--------------|-------------------|------------------|
| 12000-12099|            | **Reserved for Save Operations** | |
| 12100-12199|            | **Reserved for Load Operations** | |
| 12200-12299|            | **Reserved for Serialization** | |

---

## Adding New Event IDs

When adding new event IDs, follow these rules:

1. **Choose Appropriate Range**: Select the range that matches your subsystem
2. **Reserve Sub-Ranges**: Document sub-range usage (e.g., 2100-2199 for component ops)
3. **Update Registry**: Add entry to this file with all required fields
4. **Use Source Generators**: Define in `LogMessages.cs` using `[LoggerMessage]`
5. **Document in Standards**: Reference in `logging-standards.md`

### Example Addition

```csharp
// In LogMessages.cs
[LoggerMessage(
    EventId = 7000,
    Level = LogLevel.Debug,
    Message = "Input received: {InputType} with value {Value}"
)]
public static partial void LogInputReceived(this ILogger logger, string inputType, object value);
```

```markdown
# In event-id-registry.md
| Event ID | Level   | Defined In        | Message Template |
|----------|---------|-------------------|------------------|
| 7000     | Debug   | LogMessages.cs    | Input received: {InputType} with value {Value} |
```

---

## Notes

- Event IDs are globally unique across the entire application
- Each subsystem has 1000 IDs allocated (e.g., 1000-1999)
- Sub-ranges within each subsystem reserve 100 IDs (e.g., 1000-1099)
- Reserved ranges are for future expansion
- Never reuse or reassign event IDs once deployed
