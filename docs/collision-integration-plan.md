# CollisionSystem Integration Plan

**Agent**: Agent 2 (Collision Integration Specialist)
**Status**: ⏸️ BLOCKED - Waiting for Agent 1
**Date**: 2025-10-31

---

## Blocking Dependency

**CollisionSystem** must be created by Agent 1 before integration can proceed.

**Expected File**: `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Gameplay/Systems/CollisionSystem.cs`

**Required Interface**:
```csharp
namespace PokeSharp.Gameplay.Systems;

public class CollisionSystem : BaseSystem
{
    public override int Priority => 150; // Between Input and Movement

    // Required method for InputSystem integration
    public bool IsPositionWalkable(World world, int x, int y);
}
```

---

## Integration Tasks

### Task 1: Update InputSystem

**File**: `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Input/Systems/InputSystem.cs`

**Change Location**: Line 125-126 (inside StartMovement method)

**Current Code**:
```csharp
// TODO: Add collision detection here in future
// For now, allow all movement
```

**New Code**:
```csharp
// Check collision before allowing movement
if (!collisionSystem.IsPositionWalkable(world, targetX, targetY))
{
    return; // Blocked by collision, don't start movement
}
```

**Additional Changes Required**:

1. **Add CollisionSystem field**:
```csharp
public class InputSystem : BaseSystem
{
    private CollisionSystem? _collisionSystem;

    // ... existing code ...
}
```

2. **Update StartMovement signature to pass World**:
```csharp
// OLD:
private static void StartMovement(ref Position position, ref GridMovement movement, Direction direction)

// NEW:
private void StartMovement(World world, ref Position position, ref GridMovement movement, Direction direction)
```

3. **Resolve CollisionSystem from World in Update method**:
```csharp
public override void Update(World world, float deltaTime)
{
    EnsureInitialized();

    // Resolve CollisionSystem once per frame
    if (_collisionSystem == null)
    {
        // Get SystemManager from world and resolve CollisionSystem
        // OR: Pass CollisionSystem via constructor
    }

    // ... rest of Update method ...
}
```

4. **Update StartMovement call to pass world**:
```csharp
// Line 71: OLD
StartMovement(ref position, ref movement, input.PressedDirection);

// Line 71: NEW
StartMovement(world, ref position, ref movement, input.PressedDirection);
```

---

### Task 2: Register CollisionSystem in PokeSharpGame

**File**: `/mnt/c/Users/nate0/RiderProjects/foo/PokeSharp/PokeSharp.Game/PokeSharpGame.cs`

**Change Location**: Line 89-90 (in Initialize method)

**Current Code**:
```csharp
// TODO: Register CollisionSystem here (Priority: 150) when it's created
// _systemManager.RegisterSystem(new CollisionSystem());
```

**New Code**:
```csharp
// Register CollisionSystem (Priority: 150, between Input and Movement)
_systemManager.RegisterSystem(new CollisionSystem());
```

**Additional Dependency**:
Add using statement at top of file:
```csharp
using PokeSharp.Gameplay.Systems;
```

---

## System Execution Order (After Integration)

```
Priority 0:   InputSystem         ← Captures input, syncs Direction
Priority 150: CollisionSystem     ← NEW - Validates target positions
Priority 100: MovementSystem      ← Executes validated movements
Priority 800: AnimationSystem     ← Updates sprite frames
Priority 900: MapRenderSystem     ← Renders tile map
Priority 1000: RenderSystem       ← Renders sprites
```

**Note**: CollisionSystem priority of 150 places it logically between Input (0) and Movement (100), even though numerically it appears higher. The actual execution order is controlled by SystemManager's priority sorting.

---

## Testing Checklist (Post-Integration)

After integration is complete, verify:

- [ ] Build succeeds with 0 errors
- [ ] CollisionSystem is registered and initializes
- [ ] Player cannot walk through solid tiles (trees, buildings, water)
- [ ] Player can walk on walkable tiles (grass, paths)
- [ ] Collision check occurs BEFORE movement starts
- [ ] Direction updates even when collision blocks movement
- [ ] No console errors during gameplay

---

## Coordination Hooks

**Before Integration**:
```bash
npx claude-flow@alpha hooks pre-task --description "Integrate collision checking"
npx claude-flow@alpha hooks session-restore --session-id "swarm-phase2-completion"
```

**After Each File Edit**:
```bash
npx claude-flow@alpha hooks post-edit --file "[file-path]" --memory-key "swarm/collision/[step]"
```

**After Build**:
```bash
dotnet build
```

**After Integration Complete**:
```bash
npx claude-flow@alpha memory store swarm/collision/integrated "true" --namespace swarm
npx claude-flow@alpha hooks notify --message "Collision integrated into movement pipeline"
npx claude-flow@alpha hooks post-task --task-id "task-collision-integration"
```

---

## Dependencies

**Agent 1 Must Complete**:
1. Create `CollisionSystem.cs` in `/PokeSharp.Gameplay/Systems/`
2. Implement `IsPositionWalkable(World world, int x, int y)` method
3. Set Priority to 150
4. Query TileCollider components from map entities
5. Check tile collision flags based on grid coordinates

**Agent 2 Will Then Complete**:
1. Add CollisionSystem reference to InputSystem
2. Update StartMovement to check collisions
3. Register CollisionSystem in PokeSharpGame
4. Build and verify integration
5. Document completion

---

## Current Status

**Agent 2 Status**: ⏸️ **BLOCKED**

**Reason**: CollisionSystem file does not exist

**Next Action**: Wait for Agent 1 to signal completion via memory:
```bash
npx claude-flow@alpha memory query collision --namespace swarm
```

**Expected Memory Key**: `swarm/collision/status` with value `"created"`

---

## Notes

- CollisionSystem is already documented in Phase 2 completion report
- SystemPriority.Collision constant exists (value: 200) but we're using 150 per mission specs
- InputSystem uses `static` StartMovement which needs to become instance method
- World reference is already available in InputSystem.Update method
- TileCollider component already exists and is attached to map entity

---

**Report Created**: 2025-10-31
**Agent**: Collision Integration Specialist (Agent 2)
**Status**: Awaiting Agent 1 completion signal
