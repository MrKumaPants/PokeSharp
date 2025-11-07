# Contributing to PokeSharp

## Code Organization Standards

This document defines the file organization and naming conventions for the PokeSharp codebase. Following these standards ensures consistency, maintainability, and ease of navigation.

---

## File Organization Rules

### Rule 1: One Type Per File

**Standard:** Each file should contain exactly ONE public type (class, interface, struct, or enum).

**✅ CORRECT:**
```csharp
// File: Position.cs
namespace PokeSharp.Core.Components;

public struct Position
{
    public int X { get; set; }
    public int Y { get; set; }
}
```

**❌ WRONG:**
```csharp
// File: Components.cs  
namespace PokeSharp.Core.Components;

public struct Position { ... }
public struct Velocity { ... }
public struct Direction { ... }  // VIOLATION: Multiple public types
```

**Exception:** Private nested classes are acceptable when they're implementation details:
```csharp
// File: EventBus.cs
public class EventBus : IEventBus
{
    // ✅ ACCEPTABLE: Private nested class
    private sealed class Subscription<TEvent> : IDisposable
    {
        ...
    }
}
```

---

### Rule 2: Filename Must Match Type Name

**Standard:** The file name must exactly match the primary public type name.

**✅ CORRECT:**
- `AssetManager.cs` contains `class AssetManager`
- `IWorldApi.cs` contains `interface IWorldApi`
- `Direction.cs` contains `enum Direction`

**❌ WRONG:**
- `WorldApiImplementation.cs` contains `class WorldApiImplementation` (see Rule 6)
- `CheckAssets.cs` contains `class AssetDiagnostics` (name mismatch)
- `CompilationDiagnostics.cs` contains `class CompilationDiagnostic` (plural vs singular)

---

### Rule 3: Use Singular Names for Types

**Standard:** Type names and their files should be singular unless they are static utility classes.

**✅ CORRECT:**
- `CompilationDiagnostic.cs` (singular)
- `TiledJsonLayer.cs` (singular)
- `Position.cs` (singular)

**Acceptable Plural Forms:**
- `LogTemplates.cs` (static class with template methods)
- `LogMessages.cs` (static class with message constants)
- `QueryExtensions.cs` (static extension methods class)

**❌ WRONG:**
- `CompilationDiagnostics.cs` containing `class CompilationDiagnostic`

---

### Rule 4: Namespace Must Match Folder Structure

**Standard:** Namespaces must exactly match the folder path from the project root.

**✅ CORRECT:**
```
File: PokeSharp.Rendering/Loaders/TiledJson/TiledJsonMap.cs
Namespace: PokeSharp.Rendering.Loaders.TiledJson
```

**❌ WRONG:**
```
File: PokeSharp.Rendering/Loaders/TiledJson/TiledJsonMap.cs
Namespace: PokeSharp.Rendering.Loaders  // Missing TiledJson!
```

---

### Rule 5: Use Subfolders for Related Types

**Standard:** When you have multiple related DTOs or types, group them in a subfolder.

**✅ CORRECT:**
```
PokeSharp.Rendering/
  Loaders/
    TiledJson/              (subfolder for TiledJson DTOs)
      TiledJsonMap.cs
      TiledJsonLayer.cs
      TiledJsonTileset.cs
      ...
    Tmx/                    (subfolder for Tmx DTOs)
      TmxDocument.cs
      TmxLayer.cs
      ...
    TiledMapLoader.cs       (loader that uses DTOs)
    MapLoader.cs
```

**❌ WRONG:**
```
PokeSharp.Rendering/
  Loaders/
    TiledJsonMap.cs         (contains 7 classes!)
    TmxDocument.cs          (contains 7 classes!)
    TiledMapLoader.cs
```

---

### Rule 6: Avoid Redundant Suffixes

**Standard:** Don't use redundant suffixes like "Implementation" or "Impl".

**✅ CORRECT:**
```csharp
// Interface
public interface IWorldApi { ... }

// Implementation - just name it without suffix
public class WorldApi : IWorldApi { ... }
```

**❌ WRONG:**
```csharp
public interface IWorldApi { ... }
public class WorldApiImplementation : IWorldApi { ... }  // Redundant suffix
public class WorldApiImpl : IWorldApi { ... }  // Abbreviation + redundant
```

**When Implementation Suffix is Acceptable:**
- When you have multiple implementations: `MemoryCacheService`, `RedisCacheService`
- When the interface is in a different layer: `DefaultWorldApi`, `ProductionWorldApi`

---

### Rule 7: Use Consistent Suffix Patterns

**Standard:** Use consistent suffixes across the codebase for similar concepts.

**Recommended Suffixes:**

| Suffix | Usage | Example |
|--------|-------|---------|
| `Service` | Business logic orchestration | `ScriptService`, `CacheService` |
| `Manager` | Coordinating multiple objects/systems | `SystemManager`, `AssetManager` |
| `System` | ECS systems that process components | `MovementSystem`, `CollisionSystem` |
| `Registry` | Stores and retrieves registered items | `TypeRegistry<T>`, `MapRegistry` |
| `Factory` | Creates instances | `EntityFactory`, `WatcherFactory` |
| `Builder` | Fluent object construction | `EntityBuilder`, `QueryBuilder` |
| `Loader` | Loads external data | `MapLoader`, `TiledMapLoader` |
| `Parser` | Parses text/data formats | `TmxParser`, `JsonParser` |
| `Component` | ECS component data | `Position`, `Velocity`, `GridMovement` |

**Avoid:**
- `Helper`, `Utility`, `Common` (too vague)
- `Implementation`, `Impl` (redundant with interface)
- `Manager` for everything (be specific)

---

### Rule 8: Acronym Casing

**Standard:** Follow .NET acronym casing rules.

**2-Letter Acronyms:** All uppercase in PascalCase
- `IOHelper` (not `IoHelper`)
- `DBContext` (not `DbContext`) - *Note: EF Core uses `DbContext` as exception*

**3-Letter Acronyms:** Pascal case
- `NPC` should be `NPC` (not `Npc`) - well-known acronym
- `API` can be `Api` or `API` - both acceptable, but be consistent

**Current Standard in PokeSharp:**
- Use `Api` (not `API`): `IWorldApi`, `IPlayerApi`
- Use `NPC` (not `Npc`): `NPCComponent`, `NPCBehaviorSystem`

---

### Rule 9: Folder Naming

**Standard:** All folders use PascalCase.

**✅ CORRECT:**
- `Components/`
- `Systems/`
- `HotReload/`
- `Diagnostics/`

**❌ WRONG:**
- `components/`
- `diagnostics/` (lowercase)
- `hot_reload/` (snake_case)
- `hot-reload/` (kebab-case)

---

### Rule 10: Test Project Organization

**Standard:** Mirror the source project structure in test projects.

**Example:**
```
PokeSharp.Rendering/
  Systems/
    AnimationSystem.cs
    CameraFollowSystem.cs
  Loaders/
    MapLoader.cs

PokeSharp.Rendering.Tests/
  Systems/
    AnimationSystemTests.cs
    CameraFollowSystemTests.cs
  Loaders/
    MapLoaderTests.cs
```

**Test File Naming:**
- Add `Tests` suffix to the type name
- `AnimationSystem` → `AnimationSystemTests.cs`
- `MapLoader` → `MapLoaderTests.cs`

---

## File Organization Checklist

Before committing code, verify:

- [ ] Each file contains only one public type
- [ ] Filename matches the type name exactly
- [ ] Namespace matches folder structure
- [ ] Related types are grouped in subfolders
- [ ] Folders use PascalCase
- [ ] No redundant suffixes (Implementation, Impl)
- [ ] Acronyms follow casing rules (NPC, not Npc)
- [ ] Test files mirror source structure
- [ ] No `<Compile Remove>` entries in .csproj files

---

## Common Refactoring Patterns

### Splitting Multi-Class Files

**Before:**
```csharp
// File: Models.cs
namespace MyApp;

public class User { ... }
public class Product { ... }
public class Order { ... }
```

**After:**
```
Models/
  User.cs
  Product.cs
  Order.cs
```

**In each file:**
```csharp
// File: Models/User.cs
namespace MyApp.Models;

public class User { ... }
```

### Extracting Enums

**Before:**
```csharp
// File: IScriptWatcher.cs
public interface IScriptWatcher { ... }
public enum WatcherStatus { ... }  // VIOLATION
```

**After:**
```
IScriptWatcher.cs    (interface only)
WatcherStatus.cs     (enum only)
```

### Organizing Related DTOs

**When you have 5+ related DTOs:**

**Before:**
```
TiledJsonMap.cs (contains 7 classes)
```

**After:**
```
TiledJson/
  TiledJsonMap.cs
  TiledJsonLayer.cs
  TiledJsonTileset.cs
  ...
```

---

## Project Structure Best Practices

### Project Dependencies

**Rule:** Projects should have clear dependency hierarchy.

```
PokeSharp.Game (executable)
  ↓ depends on
PokeSharp.Core, PokeSharp.Rendering, PokeSharp.Input, PokeSharp.Scripting
  ↓ all depend on
PokeSharp.Core (foundation)
```

**Never:**
- Create circular dependencies
- Have Core depend on higher-level projects
- Add unused project references

### Empty Projects

**Rule:** Don't keep empty projects in the solution.

**Options:**
1. **Remove completely** - Recommended
2. **Add README.md** - Explaining future purpose
3. **Implement basic structure** - If starting soon

**Never leave empty projects without documentation.**

---

## .NET 9.0 Best Practices

### Use Primary Constructors

**Recommended (.NET 9.0):**
```csharp
public class EventBus(ILogger<EventBus>? logger = null) : IEventBus
{
    private readonly ILogger<EventBus> _logger = logger ?? NullLogger<EventBus>.Instance;
}
```

### Use Collection Expressions

**Recommended (.NET 8+):**
```csharp
return ["tile/base", "tile/ground", "tile/wall"];
```

**Old Style:**
```csharp
return new[] { "tile/base", "tile/ground", "tile/wall" };
```

### Use `file` Scoped Types

**For types only used in one file:**
```csharp
// In EventBus.cs
file class Subscription<TEvent> : IDisposable  // Only visible in this file
{
    ...
}
```

---

## Tools and Automation

### Using Rider/Visual Studio

**Extract to New File:**
1. Right-click on class name
2. Select "Refactor" → "Move to New File"
3. IDE will create new file and update all references

**Rename Type:**
1. Right-click on type name
2. Select "Refactor" → "Rename"
3. IDE will update all references automatically

### Pre-commit Checks

**Recommended Git hook:**
```bash
# Check for files with multiple public types
dotnet format --verify-no-changes
```

---

## Questions?

If you're unsure about organization for a specific case, refer to:
1. Microsoft's .NET coding conventions
2. Existing code in the same project
3. This CONTRIBUTING.md file

When in doubt, **one type per file** is always safe.

