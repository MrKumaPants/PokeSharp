# Property Mapper Architecture

## Overview

The Property Mapper system separates concerns for converting Tiled map properties to ECS components using the **Strategy Pattern**. This architecture follows SOLID principles to create an extensible, maintainable property mapping system.

## Architecture Decision Record (ADR)

### Context

The MapLoader previously had hardcoded property-to-component conversion logic in the `ProcessTileProperties` method. This created several issues:

1. **Violation of Open/Closed Principle**: Adding new component types required modifying MapLoader
2. **Poor Testability**: Property mapping logic was intertwined with map loading logic
3. **Code Duplication**: Similar property parsing logic repeated across methods
4. **Difficult Maintenance**: Changes to one component's mapping could affect others

### Decision

We implemented the **Strategy Pattern** with the following components:

#### 1. Core Interfaces

```csharp
// Base mapper interface
public interface IPropertyMapper<TComponent>
{
    bool CanMap(Dictionary<string, object> properties);
    TComponent Map(Dictionary<string, object> properties);
}

// Extended interface for entity modification
public interface IEntityPropertyMapper<TComponent> : IPropertyMapper<TComponent>
{
    void MapAndAdd(World world, Entity entity, Dictionary<string, object> properties);
}
```

#### 2. Concrete Mappers

Each mapper handles **one component type** with **single responsibility**:

- **CollisionMapper** - Maps "solid" and "collidable" properties → `Collision`
- **LedgeMapper** - Maps "ledge_direction" property → `TileLedge`
- **EncounterZoneMapper** - Maps "encounter_rate" and "encounter_table" → `EncounterZone`
- **TerrainTypeMapper** - Maps "terrain_type" and "footstep_sound" → `TerrainType`
- **ScriptMapper** - Maps "script" and "on_step" properties → `TileScript`
- **InteractionMapper** - Maps interaction-related properties → `Interaction`
- **NpcMapper** - Maps NPC-related properties → `Npc`

#### 3. Registry Pattern

`PropertyMapperRegistry` manages all mappers and provides centralized access:

```csharp
public class PropertyMapperRegistry
{
    public void RegisterMapper<TComponent>(IPropertyMapper<TComponent> mapper);
    public IEnumerable<IPropertyMapper<TComponent>> GetMappers<TComponent>();
    public int MapAndAddAll(World world, Entity entity, Dictionary<string, object> properties);
}
```

## Component Interaction Diagram

```
┌─────────────────┐
│   MapLoader     │
└────────┬────────┘
         │ uses
         ▼
┌─────────────────────────┐
│ PropertyMapperRegistry  │
│ ┌─────────────────────┐ │
│ │ List<IMapper>       │ │
│ └─────────────────────┘ │
└────────┬────────────────┘
         │ iterates
         ▼
┌──────────────────────────────────┐
│  IEntityPropertyMapper<T>        │
├──────────────────────────────────┤
│  + CanMap(props): bool           │
│  + Map(props): T                 │
│  + MapAndAdd(world, entity, ...) │
└──────────────────────────────────┘
         △
         │ implements
    ┌────┴────┬────────┬──────────┬─────────┐
    │         │        │          │         │
┌───▼───┐ ┌──▼───┐ ┌──▼────┐ ┌───▼────┐ ┌──▼────┐
│Collision│Ledge │Encounter│Terrain │ Script│
│ Mapper  │Mapper│ Mapper  │ Mapper │ Mapper│
└─────────┘└──────┘└─────────┘└────────┘└───────┘
```

## Data Flow

### 1. Mapper Registration (Startup)

```
Program.cs / DI Container
  │
  ├─> Create PropertyMapperRegistry
  │
  ├─> Register CollisionMapper
  ├─> Register LedgeMapper
  ├─> Register EncounterZoneMapper
  ├─> Register TerrainTypeMapper
  ├─> Register ScriptMapper
  └─> Inject Registry into MapLoader
```

### 2. Map Loading (Runtime)

```
MapLoader.LoadMapEntities
  │
  ├─> For each tile with properties
  │     │
  │     ├─> Call Registry.MapAndAddAll(world, entity, props)
  │     │     │
  │     │     ├─> For each registered mapper
  │     │     │     │
  │     │     │     ├─> mapper.CanMap(props)?
  │     │     │     │   Yes: mapper.MapAndAdd(world, entity, props)
  │     │     │     │        │
  │     │     │     │        ├─> component = Map(props)
  │     │     │     │        └─> world.Add(entity, component)
  │     │     │     │
  │     │     │     └─> Next mapper
  │     │     │
  │     │     └─> Return component count
  │     │
  │     └─> Tile entity now has all applicable components
  │
  └─> Map loaded
```

## Quality Attributes

### Maintainability
- **Single Responsibility**: Each mapper handles one component type
- **Separation of Concerns**: Property parsing separate from map loading
- **Clear Abstractions**: Interfaces define clear contracts

### Extensibility
- **Open/Closed Principle**: Add new mappers without modifying existing code
- **Strategy Pattern**: New component types = new mapper class
- **Plug-and-Play**: Register/unregister mappers via DI

### Testability
- **Unit Testable**: Each mapper can be tested independently
- **Mock-Friendly**: Interfaces enable easy mocking
- **Focused Tests**: Test property parsing separate from ECS operations

### Performance
- **Minimal Overhead**: Dictionary lookups are O(1)
- **Early Exit**: `CanMap()` avoids unnecessary processing
- **Batch Processing**: Registry processes all mappers in one pass

## Trade-offs

### Pros
✅ Easy to add new component types
✅ Each mapper is independently testable
✅ Clear, focused responsibilities
✅ No modification to MapLoader for new components
✅ Type-safe component creation

### Cons
⚠️ More classes (one per component type)
⚠️ Slight runtime overhead from reflection in registry
⚠️ Requires DI setup for registration

## Migration Strategy

### Phase 1: Create Mappers ✅
- [x] Define IPropertyMapper interfaces
- [x] Implement CollisionMapper
- [x] Implement LedgeMapper
- [x] Implement EncounterZoneMapper
- [x] Implement TerrainTypeMapper
- [x] Implement ScriptMapper

### Phase 2: Update MapLoader
- [ ] Inject PropertyMapperRegistry
- [ ] Replace ProcessTileProperties with Registry.MapAndAddAll
- [ ] Remove hardcoded property parsing
- [ ] Maintain backward compatibility

### Phase 3: DI Registration
- [ ] Register PropertyMapperRegistry in DI
- [ ] Register all mappers in DI container
- [ ] Update Program.cs or ServiceConfiguration

### Phase 4: Testing
- [ ] Unit tests for each mapper
- [ ] Integration tests for registry
- [ ] End-to-end tests for MapLoader

## Example Usage

### Adding a New Component Type

```csharp
// 1. Create new component
public struct WarpPoint
{
    public string TargetMap { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
}

// 2. Create mapper
public class WarpPointMapper : IEntityPropertyMapper<WarpPoint>
{
    public bool CanMap(Dictionary<string, object> properties)
    {
        return properties.ContainsKey("warp_target");
    }

    public WarpPoint Map(Dictionary<string, object> properties)
    {
        var target = properties["warp_target"].ToString()!;
        var parts = target.Split(',');

        return new WarpPoint
        {
            TargetMap = parts[0],
            TargetX = int.Parse(parts[1]),
            TargetY = int.Parse(parts[2])
        };
    }

    public void MapAndAdd(World world, Entity entity, Dictionary<string, object> properties)
    {
        if (CanMap(properties))
        {
            var warpPoint = Map(properties);
            world.Add(entity, warpPoint);
        }
    }
}

// 3. Register in DI
services.AddSingleton<IEntityPropertyMapper<WarpPoint>, WarpPointMapper>();
```

That's it! No changes to MapLoader required.

## Testing Strategy

### Unit Tests (Per Mapper)

```csharp
[Test]
public void CollisionMapper_ShouldMapSolidProperty()
{
    // Arrange
    var mapper = new CollisionMapper();
    var props = new Dictionary<string, object>
    {
        { "solid", true }
    };

    // Act
    var collision = mapper.Map(props);

    // Assert
    Assert.That(collision.IsSolid, Is.True);
}

[Test]
public void LedgeMapper_ShouldMapDownDirection()
{
    // Arrange
    var mapper = new LedgeMapper();
    var props = new Dictionary<string, object>
    {
        { "ledge_direction", "down" }
    };

    // Act
    var ledge = mapper.Map(props);

    // Assert
    Assert.That(ledge.JumpDirection, Is.EqualTo(Direction.Down));
}
```

### Integration Tests (Registry)

```csharp
[Test]
public void Registry_ShouldApplyAllMatchingMappers()
{
    // Arrange
    var world = World.Create();
    var entity = world.Create();
    var registry = new PropertyMapperRegistry();

    registry.RegisterMapper(new CollisionMapper());
    registry.RegisterMapper(new LedgeMapper());

    var props = new Dictionary<string, object>
    {
        { "solid", true },
        { "ledge_direction", "down" }
    };

    // Act
    var count = registry.MapAndAddAll(world, entity, props);

    // Assert
    Assert.That(count, Is.EqualTo(2));
    Assert.That(world.Has<Collision>(entity), Is.True);
    Assert.That(world.Has<TileLedge>(entity), Is.True);
}
```

## Benefits Realized

### Before (Hardcoded)
```csharp
private void ProcessTileProperties(World world, Entity entity, Dictionary<string, object>? props)
{
    if (props == null) return;

    // Hardcoded collision check
    if (props.TryGetValue("solid", out var solidValue))
    {
        var isSolid = solidValue switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var result) && result,
            _ => false
        };
        if (isSolid)
            world.Add(entity, new Collision(true));
    }

    // Hardcoded terrain check
    if (props.TryGetValue("terrain_type", out var terrainValue) && terrainValue is string terrainType)
    {
        var footstepSound = props.TryGetValue("footstep_sound", out var soundValue)
            ? soundValue.ToString() ?? ""
            : "";
        world.Add(entity, new TerrainType(terrainType, footstepSound));
    }

    // ... more hardcoded checks
}
```

### After (Extensible)
```csharp
private void ProcessTileProperties(World world, Entity entity, Dictionary<string, object>? props)
{
    if (props == null) return;

    // Delegate to registry - automatically applies ALL matching mappers
    _propertyMapperRegistry.MapAndAddAll(world, entity, props);
}
```

## Conclusion

The Property Mapper architecture successfully separates concerns, improves testability, and enables extensibility without modifying core map loading logic. This follows the **Open/Closed Principle** and **Single Responsibility Principle**, resulting in a maintainable, clean architecture for property-to-component mapping.

## References

- Strategy Pattern: Gang of Four Design Patterns
- SOLID Principles: Robert C. Martin
- Entity Component System: Unity ECS, Arch.NET
- Tiled Map Editor: https://www.mapeditor.org/
