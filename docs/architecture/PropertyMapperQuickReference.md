# Property Mapper Quick Reference

## TL;DR

Property Mappers convert Tiled map properties to ECS components. Each mapper handles one component type. Just create a mapper class and register it - no changes to MapLoader needed.

## Adding a New Mapper (3 Steps)

### 1. Create Mapper Class

```csharp
using Arch.Core;
using PokeSharp.Core.Components.YourNamespace;

namespace PokeSharp.Core.Mapping;

public class YourComponentMapper : IEntityPropertyMapper<YourComponent>
{
    public bool CanMap(Dictionary<string, object> properties)
    {
        // Return true if your properties exist
        return properties.ContainsKey("your_property");
    }

    public YourComponent Map(Dictionary<string, object> properties)
    {
        if (!CanMap(properties))
            throw new InvalidOperationException("Cannot map properties");

        // Extract and validate properties
        var value = properties["your_property"];

        // Create and return component
        return new YourComponent(value);
    }

    public void MapAndAdd(World world, Entity entity, Dictionary<string, object> properties)
    {
        if (CanMap(properties))
        {
            var component = Map(properties);
            world.Add(entity, component);
        }
    }
}
```

### 2. Register in DI

Edit `/PokeSharp.Core/Mapping/PropertyMapperServiceExtensions.cs`:

```csharp
public static PropertyMapperRegistry CreatePropertyMapperRegistry(...)
{
    var registry = new PropertyMapperRegistry(logger);

    // Existing mappers...
    registry.RegisterMapper(new CollisionMapper());

    // Add your mapper
    registry.RegisterMapper(new YourComponentMapper());  // ← Add this

    return registry;
}
```

### 3. Create Tests

```csharp
using NUnit.Framework;

namespace PokeSharp.Tests.Mapping;

[TestFixture]
public class YourComponentMapperTests
{
    private YourComponentMapper _mapper = null!;
    private World _world = null!;

    [SetUp]
    public void Setup()
    {
        _mapper = new YourComponentMapper();
        _world = World.Create();
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    [Test]
    public void CanMap_WithYourProperty_ReturnsTrue()
    {
        var props = new Dictionary<string, object>
        {
            { "your_property", "value" }
        };

        Assert.That(_mapper.CanMap(props), Is.True);
    }

    [Test]
    public void Map_ValidProperties_CreatesComponent()
    {
        var props = new Dictionary<string, object>
        {
            { "your_property", "value" }
        };

        var component = _mapper.Map(props);

        Assert.That(component.YourField, Is.EqualTo("value"));
    }
}
```

**Done!** No changes to MapLoader required.

## Existing Mappers Reference

| Property in Tiled | Component | Mapper |
|-------------------|-----------|--------|
| `solid`, `collidable` | `Collision` | CollisionMapper |
| `ledge_direction` | `TileLedge` | LedgeMapper |
| `encounter_rate`, `encounter_table` | `EncounterZone` | EncounterZoneMapper |
| `terrain_type`, `footstep_sound` | `TerrainType` | TerrainTypeMapper |
| `script`, `on_step` | `TileScript` | ScriptMapper |
| `interaction_type`, `dialogue` | `Interaction` | InteractionMapper |
| `npcId`, `trainer` | `Npc` | NpcMapper |

## Common Patterns

### Boolean Properties
```csharp
public bool CanMap(Dictionary<string, object> properties)
{
    return properties.ContainsKey("solid");
}

public Collision Map(Dictionary<string, object> properties)
{
    var isSolid = properties["solid"] switch
    {
        bool b => b,
        string s => bool.TryParse(s, out var result) && result,
        _ => false
    };

    return new Collision(isSolid);
}
```

### Integer Properties with Validation
```csharp
public EncounterZone Map(Dictionary<string, object> properties)
{
    var rate = properties["encounter_rate"] switch
    {
        int i => i,
        string s when int.TryParse(s, out var result) => result,
        _ => throw new InvalidOperationException("Invalid encounter_rate")
    };

    if (rate < 0 || rate > 255)
        throw new InvalidOperationException("Rate must be 0-255");

    return new EncounterZone("", rate);
}
```

### Enum Properties
```csharp
public TileLedge Map(Dictionary<string, object> properties)
{
    var directionString = properties["ledge_direction"].ToString()!;

    var direction = directionString.ToLower() switch
    {
        "down" => Direction.Down,
        "up" => Direction.Up,
        "left" => Direction.Left,
        "right" => Direction.Right,
        _ => throw new InvalidOperationException($"Invalid direction: {directionString}")
    };

    return new TileLedge(direction);
}
```

### Optional Properties
```csharp
public TerrainType Map(Dictionary<string, object> properties)
{
    var terrainType = properties["terrain_type"].ToString()!;

    // Optional property with default
    var footstepSound = properties.TryGetValue("footstep_sound", out var sound)
        ? sound?.ToString() ?? ""
        : "";

    return new TerrainType(terrainType, footstepSound);
}
```

## Testing Checklist

For each mapper, test:

- [ ] `CanMap` returns true for valid properties
- [ ] `CanMap` returns false without required properties
- [ ] `Map` creates correct component with valid data
- [ ] `Map` throws on invalid/missing data
- [ ] `Map` handles type conversions (string → int, etc.)
- [ ] `Map` is case-insensitive where appropriate
- [ ] `MapAndAdd` adds component to entity
- [ ] `MapAndAdd` doesn't add if `CanMap` is false
- [ ] Edge cases (null, empty, whitespace, out-of-range)

## Debugging Tips

### Check if mapper is registered
```csharp
var mappers = _registry.GetMappers<YourComponent>();
Console.WriteLine($"Registered: {mappers.Count()}");
```

### Log when components are added
```csharp
var count = _registry.MapAndAddAll(world, entity, props);
Console.WriteLine($"Added {count} components");
```

### Check entity components
```csharp
Console.WriteLine($"Has YourComponent: {world.Has<YourComponent>(entity)}");
if (world.Has<YourComponent>(entity))
{
    var component = world.Get<YourComponent>(entity);
    Console.WriteLine($"Value: {component.YourField}");
}
```

## Architecture Files

- **Full Architecture**: `docs/architecture/PropertyMapperArchitecture.md`
- **DI Setup**: `docs/architecture/DependencyInjectionSetup.md`
- **Implementation Summary**: `docs/architecture/PropertyMapperImplementationSummary.md`
- **This Quick Ref**: `docs/architecture/PropertyMapperQuickReference.md`

## Common Questions

**Q: Where do I put my mapper?**
A: `/PokeSharp.Core/Mapping/YourComponentMapper.cs`

**Q: Where do I put tests?**
A: `/PokeSharp.Tests/Mapping/YourComponentMapperTests.cs`

**Q: Do I need to modify MapLoader?**
A: No! Just create mapper and register it.

**Q: Can multiple mappers apply to the same tile?**
A: Yes! Registry applies ALL matching mappers.

**Q: What if my property doesn't exist?**
A: `CanMap()` returns false, mapper is skipped.

**Q: Can I use the same property for multiple components?**
A: Yes! Multiple mappers can check the same property.

**Q: How do I handle complex property parsing?**
A: Put parsing logic in the `Map()` method. Keep it focused.

**Q: Should I validate in `CanMap()` or `Map()`?**
A: `CanMap()`: Check if properties exist
   `Map()`: Validate values and throw on invalid

**Q: Is backward compatibility maintained?**
A: Yes! PropertyMapperRegistry is optional in MapLoader.

## Performance Notes

- Mapper instances created once at startup
- `CanMap()` should be fast (just dictionary lookups)
- No reflection during mapping (only during registration)
- Registry iterates all mappers per tile (very fast)

## Real-World Example: Pokemon Grass Tile

**Tiled Properties**:
```json
{
  "terrain_type": "grass",
  "footstep_sound": "footstep_grass",
  "encounter_rate": 20,
  "encounter_table": "route_1_grass",
  "on_step": "scripts/grass_animation.lua"
}
```

**Mappers Applied**:
1. TerrainTypeMapper → `TerrainType("grass", "footstep_grass")`
2. EncounterZoneMapper → `EncounterZone("route_1_grass", 20)`
3. ScriptMapper → `TileScript("scripts/grass_animation.lua")`

**Result**: Entity with 3 components from 1 tile!

## Conclusion

Property Mappers make it trivial to add new tile behaviors. Just:
1. Create mapper class
2. Register in DI
3. Write tests

The system handles the rest. MapLoader stays clean and focused.
