# Code Review TODO - Debug Console System

**Date:** 2024-11-27
**Scope:** Recent changes to debug console, entity browser, and related systems
**Status:** Planning

---

## Overview

This document tracks improvements identified during code review of the debug console system, focusing on code quality, architecture, and UX consistency.

---

## 🔴 High Priority - ✅ COMPLETED

### 1. ✅ Split IConsoleContext Interface (ISP Violation)

**File:** `PokeSharp.Engine.Debug/Commands/IConsoleContext.cs`
**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Created `IConsoleOutput` - WriteLine, Clear, Theme, Close
- [x] Created `IConsoleLogging` - Logging enable/disable, log level
- [x] Created `IConsoleCommands` - GetAllCommands, GetCommand
- [x] Created `IConsoleHistory` - Command history operations
- [x] Created `IConsoleAliases` - Alias management
- [x] Created `IConsoleScripts` - Script operations
- [x] Created `IConsoleBookmarks` - Bookmark operations
- [x] Created `IConsoleNavigation` - Tab navigation
- [x] Created `IConsoleExport` - Console output export
- [x] Created `IWatchOperations` - Watch panel operations
- [x] Created `ILogOperations` - Log panel operations
- [x] Created `IVariableOperations` - Variables panel operations
- [x] Created `IEntityOperations` - Entity panel operations
- [x] `IConsoleContext` now extends all interfaces as composite

**New files created:**

- `Commands/Interfaces/IConsoleOutput.cs`
- `Commands/Interfaces/IConsoleLogging.cs`
- `Commands/Interfaces/IConsoleCommands.cs`
- `Commands/Interfaces/IConsoleHistory.cs`
- `Commands/Interfaces/IConsoleAliases.cs`
- `Commands/Interfaces/IConsoleScripts.cs`
- `Commands/Interfaces/IConsoleBookmarks.cs`
- `Commands/Interfaces/IConsoleNavigation.cs`
- `Commands/Interfaces/IConsoleExport.cs`
- `Commands/Interfaces/IWatchOperations.cs`
- `Commands/Interfaces/ILogOperations.cs`
- `Commands/Interfaces/IVariableOperations.cs`
- `Commands/Interfaces/IEntityOperations.cs`

---

### 2. ✅ Reduce ConsoleContext Constructor Parameters

**File:** `PokeSharp.Engine.Debug/Commands/ConsoleContext.cs`
**Status:** COMPLETED (2024-11-27)

**Solution Implemented:** Option A - aggregate classes

- [x] Created `ConsoleServices` aggregate class containing all managers
- [x] Created `ConsoleLoggingCallbacks` for logging-related callbacks
- [x] Added new constructor with aggregated parameters (4 params vs 13)
- [x] Kept old constructor for backward compatibility (marked `[Obsolete]`)
- [x] Updated `ConsoleSystem` to use new constructor

**New file created:**

- `Commands/ConsoleServices.cs` (contains both `ConsoleServices` and `ConsoleLoggingCallbacks`)

---

### 3. ✅ Centralize Tab Definitions

**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Created `ConsoleTabs` class in `PokeSharp.Engine.UI.Debug/Core/ConsoleTabs.cs`
- [x] Includes: Index, Name, Aliases, Keyboard shortcut for each tab
- [x] Updated `TabCommand` to use centralized definitions
- [x] Updated `ConsoleScene.HandleTabShortcuts()` to use `ConsoleTabs`
- [x] Added re-export in `ConsoleConstants.Tabs` for backward compatibility

**New file created:**

- `PokeSharp.Engine.UI.Debug/Core/ConsoleTabs.cs`

---

## 🟡 Medium Priority

### 4. ✅ Reduce Pass-Through Delegation Boilerplate

**Files:**

- `PokeSharp.Engine.Debug/Commands/ConsoleContext.cs`
- `PokeSharp.Engine.UI.Debug/Scenes/ConsoleScene.cs`

**Status:** COMPLETED (2024-11-27)

**Solution Implemented (Option A):**

- [x] Created panel operation interfaces in `PokeSharp.Engine.UI.Debug/Interfaces/`:
  - `IEntityOperations.cs` - Entity browser operations
  - `IWatchOperations.cs` - Watch panel operations
  - `IVariableOperations.cs` - Variables panel operations
  - `ILogOperations.cs` - Logs panel operations
- [x] Panels now implement these interfaces directly (e.g., `EntitiesPanel : IEntityOperations`)
- [x] `IConsoleContext` exposes panel properties: `Entities`, `Watches`, `Variables`, `Logs`
- [x] Commands updated to use new syntax: `context.Entities.Refresh()` instead of `context.RefreshEntities()`
- [x] Expression-based watch operations (`AddWatch(name, expression)`) kept on IConsoleContext since they need script evaluation

**Benefits achieved:**

- Adding new panel operations only requires changes in one place (the panel)
- Clearer ownership - panel operations live on the panel interface
- Reduced boilerplate in ConsoleContext

---

### 5. ✅ Make Component Detection Extensible

**File:** `PokeSharp.Engine.Debug/Systems/ConsoleSystem.cs`
**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Created `DebugComponentRegistry` class for component type registration
- [x] Created `DebugComponentRegistryFactory` to register all known game components
- [x] Components registered with display names, categories, priorities
- [x] Property readers auto-extract values from components (Position, GridMovement, etc.)
- [x] ConsoleSystem now uses registry instead of hardcoded checks

**New files created:**

- `PokeSharp.Engine.Debug/Entities/DebugComponentRegistry.cs`
- `PokeSharp.Engine.Debug/Entities/DebugComponentRegistryFactory.cs`

---

### 6. ✅ Standardize Null-Check Patterns

**Files:** `ConsoleScene.cs`, `ConsoleContext.cs`
**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Standardized on null-conditional pattern for simple cases
- [x] Fixed `ExpandVariable()` to use consistent pattern
- [x] Fixed `ExportWatchConfiguration()` to use simpler null-conditional
- [x] Kept explicit null checks for methods with multiple operations (e.g., `ImportWatchConfiguration`)

---

## 🟢 Low Priority / Future Improvements

### 7. ✅ Add Missing Keyboard Shortcuts to Help Text

**Files:**

- `TabCommand.cs` (already documented)
- `EntityCommand.cs`

**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Added "Keyboard Shortcuts" section to `EntityCommand.Usage`
- [x] Documents: Ctrl+5 (tab switch), Up/Down, Enter, P (pin), Home/End/PageUp/PageDown

---

### 8. ✅ Add EntitiesPanel.Update() to Framework Update Loop

**File:** `PokeSharp.Engine.UI.Debug/Components/Debug/EntitiesPanel.cs`
**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Added auto-refresh logic to `OnRenderContainer()` (same pattern as WatchPanel)
- [x] Uses `context.Input.GameTime` to track elapsed time
- [x] Auto-refresh, highlight clearing handled in render loop
- [x] `Update()` method kept for manual scenarios

---

### 9. ✅ Extract EntityCommand Subcommand Handlers

**File:** `PokeSharp.Engine.Debug/Commands/BuiltIn/EntityCommand.cs`
**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Extracted inline cases to private methods: `HandleExpand`, `HandleCollapse`, `HandlePin`, `HandleUnpin`, `HandleInterval`, `HandleHighlight`
- [x] Switch statement now only contains method calls, no inline logic
- [x] Consistent pattern across all subcommands

---

### 10. ✅ Improve Entity Properties Display in Panel

**File:** `PokeSharp.Engine.UI.Debug/Components/Debug/EntitiesPanel.cs`
**Status:** COMPLETED (2024-11-27)

**Solution Implemented:**

- [x] Added `RenderProperty()` method with type-aware coloring
- [x] Added `GetPropertyValueColor()` with pattern matching for:
  - Boolean values (green for true, dim for false)
  - Position/coordinate patterns (light blue)
  - Integer and float values (pale blue)
  - Direction/facing properties (yellow/orange)
  - Movement-related properties (orange)
- [x] Added `GetComponentColor()` for component-specific coloring:
  - Player (gold), NPC (light blue)
  - Movement components (green)
  - Rendering components (purple)
  - Tile components (cyan)
  - NPC components (blue)

---

## 📊 Implementation Order Recommendation

1. ✅ **Tab Centralization** (#3) - COMPLETED
2. ✅ **Keyboard Shortcuts in Help** (#7) - COMPLETED
3. ✅ **Null-Check Standardization** (#6) - COMPLETED
4. ✅ **Interface Segregation** (#1) - COMPLETED
5. ✅ **Constructor Parameters** (#2) - COMPLETED
6. ✅ **Component Detection** (#5) - COMPLETED
7. ✅ **EntitiesPanel Auto-Refresh** (#8) - COMPLETED
8. ✅ **EntityCommand Handlers** (#9) - COMPLETED
9. ✅ **Entity Properties Display** (#10) - COMPLETED
10. ✅ **Pass-Through Delegation** (#4) - COMPLETED (Option A: panel interfaces)

---

## Notes

- Changes to `IConsoleContext` will require updates to all command implementations
- Consider feature flags or gradual rollout for breaking changes
- Unit tests should be added alongside refactoring work

---

## Related Documents

- `docs/planning/CONSOLE_TODO.md` - General console feature TODO
- `docs/planning/ENTITY_BROWSER_TODO.md` - Entity browser specific features
