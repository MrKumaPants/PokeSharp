# Entity Browser Tab - Implementation Plan

**Created:** November 26, 2024
**Status:** 🔴 Not Started
**Estimated Total Time:** 25-35 hours

---

## 📊 Overview

A new debug console tab for browsing and inspecting all entities in the Arch ECS World.

| Component     | Description                                                |
| ------------- | ---------------------------------------------------------- |
| **Purpose**   | Debug entity state, component values, entity relationships |
| **Framework** | Arch ECS (Entity Component System)                         |
| **UI System** | PokeSharp.Engine.UI.Debug                                  |

---

## 🏗️ Existing Foundation

### Files to Leverage

| File                     | Purpose                                                   |
| ------------------------ | --------------------------------------------------------- |
| `LiveEntityInspector.cs` | Real-time entity inspector with auto-refresh              |
| `EntityInspector.cs`     | Basic entity property/component display                   |
| `EntityInfo.cs`          | Entity data model (ID, Name, Properties, Components, Tag) |
| `WatchPanel.cs`          | Reference for tab panel pattern                           |
| `VariablesPanel.cs`      | Reference for tree-view expansion pattern                 |

### ECS Integration Points

| Component              | Location                                        |
| ---------------------- | ----------------------------------------------- |
| `Arch.Core.World`      | Singleton in DI, holds all entities             |
| `EntityPoolManager`    | Entity pooling and statistics                   |
| `EntityFactoryService` | Template-based entity spawning                  |
| `EntityTemplate`       | Template definitions (pokemon, npc, item, etc.) |

---

## 📋 Implementation Phases

### Phase 1: Core Panel Structure (4-5 hours) ✅ COMPLETE

- [x] Create `EntitiesPanel.cs` following builder pattern
- [x] Create `EntitiesPanelBuilder.cs`
- [x] Add tab to `ConsoleScene.cs`
- [x] Wire up entity provider (Arch World integration done separately)
- [x] Basic entity list display (ID, name, component count)
- [x] Add Ctrl+5 keyboard shortcut for Entities tab
- [x] Update `tab` command to include Entities tab

### Phase 2: Entity Querying (3-4 hours) ✅ COMPLETE

- [x] Query all entities from World using `World.Query()`
- [x] Extract component types per entity by checking known component types
- [x] Handle entity refresh via entity provider pattern
- [x] Convert Arch entities to EntityInfo in ConsoleSystem
- [x] Wire up entity provider to console scene

### Phase 3: Filtering & Search (3-4 hours) ✅ COMPLETE

- [x] Tag filter (`entity filter tag <tag>`)
- [x] Component type filter (`entity filter component <name>`)
- [x] Text search by name/ID (`entity filter search <text>`, `entity find <text>`)
- [x] Filter methods in IConsoleContext and ConsoleContext
- [x] Filter UI in header showing active filters

### Phase 4: Entity Inspector (5-6 hours) ✅ COMPLETE

- [x] Select entity (`entity inspect <id>`, `SelectEntity()`)
- [x] Expand/collapse entity details (`entity expand/collapse <id>`)
- [x] Display all components with names
- [x] Show component property values (Position, TilePosition, Elevation, Direction, IsSolid)
- [x] Tree-view display with expand/collapse indicators

### Phase 5: Commands (2-3 hours) ✅ COMPLETE

- [x] Created `EntityCommand.cs`
- [x] `entity list` - List entities with filter info
- [x] `entity find <text>` - Find entities by name/ID
- [x] `entity inspect <id>` - Show entity details
- [x] `entity filter <type> <value>` - Set filters (tag, search, component)
- [x] `entity count` - Show entity statistics
- [x] `entity tags` - List all unique tags with counts
- [x] `entity components` - List all component names
- [x] `entity expand/collapse <id>` - Expand/collapse entities
- [x] `entity pin/unpin <id>` - Pin entities to top
- [x] `entity refresh` - Refresh entity list
- [x] `entity clear` - Clear all filters

### Phase 6: Live Updates (3-4 hours) ✅ COMPLETE

- [x] Auto-refresh toggle (`entity auto [on|off]`)
- [x] Configurable refresh interval (`entity interval <sec>`)
- [x] Configurable highlight duration (`entity highlight <sec>`)
- [x] Highlight new entities with bright green + "✨ [NEW]" marker
- [x] Track spawned/removed entities per session
- [x] Session stats display (`entity session`, `entity session clear`)
- [x] Auto-fade highlights after duration expires

### Phase 7: Polish (2-3 hours) ✅ COMPLETE

- [x] Keyboard navigation (Up/Down, Enter, P, Home/End, PageUp/PageDown)
- [x] Copy entity list to clipboard (`entity copy`, `entity copy csv`)
- [x] Copy selected entity (`entity copy selected`)
- [x] Export entity list to console (`entity export`)
- [x] Navigation hints in footer
- [x] Selection index tracking across pinned and regular entities

---

## 🔴 Phase 1: Core Panel Structure

### Files to Create

```
PokeSharp.Engine.UI.Debug/
├── Components/Debug/
│   ├── EntitiesPanel.cs
│   └── EntitiesPanelBuilder.cs
PokeSharp.Engine.Debug/
├── Commands/BuiltIn/
│   └── EntityCommand.cs
```

### EntitiesPanel.cs Skeleton

```csharp
using Arch.Core;
using PokeSharp.Engine.UI.Debug.Components.Base;
using PokeSharp.Engine.UI.Debug.Components.Controls;
using PokeSharp.Engine.UI.Debug.Core;
using PokeSharp.Engine.UI.Debug.Models;

namespace PokeSharp.Engine.UI.Debug.Components.Debug;

public class EntitiesPanel : Panel
{
    private readonly TextBuffer _entityListBuffer;
    private World? _world;
    private List<EntityInfo> _entities = new();
    private List<EntityInfo> _filteredEntities = new();

    // Filters
    private string _tagFilter = "";
    private string _searchFilter = "";
    private Type? _componentFilter;

    // Selection
    private int? _selectedEntityId;
    private HashSet<int> _expandedEntities = new();

    // Auto-refresh
    private bool _autoRefresh = true;
    private float _refreshInterval = 1.0f;
    private float _timeSinceRefresh = 0f;

    internal EntitiesPanel(TextBuffer entityListBuffer)
    {
        _entityListBuffer = entityListBuffer;
    }

    public void SetWorld(World world) => _world = world;

    public void RefreshEntities()
    {
        if (_world == null) return;

        _entities.Clear();
        // Query all entities from World
        // Build EntityInfo for each

        ApplyFilters();
        UpdateDisplay();
    }

    public void SetTagFilter(string tag);
    public void SetSearchFilter(string search);
    public void SetComponentFilter(Type? componentType);
    public void ClearFilters();

    public void SelectEntity(int entityId);
    public void ExpandEntity(int entityId);
    public void CollapseEntity(int entityId);

    private void ApplyFilters();
    private void UpdateDisplay();
}
```

### Integration with ConsoleScene

```csharp
// In ConsoleScene.cs

private EntitiesPanel? _entitiesPanel;
private World? _world; // Injected or accessed via service

// In LoadContent():
_entitiesPanel = new EntitiesPanelBuilder()
    .WithWorld(_world)
    .Build();

_tabContainer.AddTab("Entities", _entitiesPanel);

// In Update():
_entitiesPanel?.Update(gameTime);
```

---

## 📊 EntityInfo Model

Already exists at `PokeSharp.Engine.UI.Debug/Models/EntityInfo.cs`:

```csharp
public class EntityInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
    public List<string> Components { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? Tag { get; set; }
}
```

May need to extend with:

```csharp
// Additional properties for entity browser
public Entity ArchEntity { get; set; }  // Reference to actual Arch entity
public Type[] ComponentTypes { get; set; } = Array.Empty<Type>();
public DateTime LastUpdated { get; set; }
public bool IsPooled { get; set; }
```

---

## 🎯 Entity Commands

### EntityCommand.cs

```csharp
[ConsoleCommand("entity", "Browse and inspect ECS entities")]
public class EntityCommand : IConsoleCommand
{
    public string Usage => @"entity <subcommand>
  entity list [tag]     - List all entities (optionally filter by tag)
  entity find <text>    - Find entity by name or ID
  entity inspect <id>   - Show entity details
  entity filter <tag>   - Set tag filter
  entity clear          - Clear all filters
  entity count          - Show entity statistics
  entity spawn <tmpl>   - Spawn entity from template
  entity destroy <id>   - Destroy entity";

    public async Task ExecuteAsync(IConsoleContext context, string[] args)
    {
        if (args.Length == 0)
        {
            context.WriteLine(Usage);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                // List entities
                break;
            case "find":
                // Find entity
                break;
            case "inspect":
                // Inspect entity
                break;
            // ... etc
        }
    }
}
```

---

## 🔧 Technical Considerations

### Performance

- **Virtual Scrolling:** Only render visible entities (important for 1000+ entities)
- **Lazy Component Loading:** Don't reflect component values until expanded
- **Throttled Refresh:** Don't query World every frame
- **Caching:** Cache component type lists

### Thread Safety

- Arch World operations should be on main thread
- Use similar pattern to WatchEvaluator if background work needed

### Arch ECS Specifics

```csharp
// Getting all entities
var allEntities = new List<Entity>();
_world.GetEntities(allEntities);

// Getting components for an entity
var componentTypes = entity.GetComponentTypes();

// Getting component value
if (entity.Has<Transform>())
{
    ref var transform = ref entity.Get<Transform>();
}

// Query with archetype
var query = new QueryDescription().WithAll<Transform, Sprite>();
_world.Query(in query, (Entity e) => { ... });
```

---

## ✅ Acceptance Criteria

### Phase 1 Complete When:

- [ ] Entities tab visible in debug console
- [ ] Can see list of entities with IDs
- [ ] Tab switches work correctly

### Phase 2 Complete When:

- [ ] All entities from World displayed
- [ ] Component types shown per entity
- [ ] Manual refresh works

### Phase 3 Complete When:

- [ ] Can filter by tag
- [ ] Can search by name/ID
- [ ] Filter state shown in header

### Phase 4 Complete When:

- [ ] Can click to select entity
- [ ] Can expand to see component values
- [ ] Tree-view navigation works

### Phase 5 Complete When:

- [ ] All entity commands functional
- [ ] Commands integrate with panel

### Phase 6 Complete When:

- [ ] Auto-refresh works
- [ ] New/removed entities highlighted
- [ ] Performance acceptable with many entities

---

## 📝 Notes

- Leverage existing `LiveEntityInspector` patterns
- Follow same builder pattern as other panels
- Match formatting style of VariablesPanel
- Keep consistent with `tab entity` command pattern
