# Theme System Refactoring TODO

> **Created:** 2024-11-28
> **Status:** In Progress
> **Priority:** High - Affects UI consistency across all debug panels

---

## Overview

The theme system has a solid foundation with `UITheme`, `ThemeManager`, and per-component theme properties, but there are significant inconsistencies and code smells that need attention. This document tracks all identified issues and their resolution.

---

## 🔴 Priority 1: Critical Architecture Issues

### 1.1 Remove ConsoleColors Duplication

**Problem:** Two separate color systems exist that don't interact:

- `UITheme` (modern, in `PokeSharp.Engine.UI.Debug`)
- `ConsoleColors` (legacy, in `PokeSharp.Engine.Debug`)

When themes are switched via `ThemeManager`, anything using `ConsoleColors` won't change.

**Files Affected:**

- `PokeSharp.Engine.Debug/Console/Configuration/ConsoleColors.cs` (to be deleted)
- Any files importing `ConsoleColors`

**Tasks:**

- [x] Search for all usages of `ConsoleColors.` in the codebase ✅ None found - orphaned code
- [x] Map each `ConsoleColors` constant to its `UITheme` equivalent ✅ Not needed
- [x] Update all usages to use `ThemeManager.Current.XxxColor` ✅ Not needed
- [x] Delete `ConsoleColors.cs` after migration complete ✅ Deleted 2024-11-28
- [x] Update any documentation referencing `ConsoleColors` ✅ Updated comments

**Mapping Reference:**
| ConsoleColors | UITheme Equivalent |
|--------------|-------------------|
| `Background_Primary` | `ConsoleBackground` |
| `Background_Secondary` | `ConsoleOutputBackground` |
| `Background_Elevated` | `BackgroundElevated` |
| `Text_Primary` | `TextPrimary` |
| `Text_Secondary` | `TextSecondary` |
| `Text_Tertiary` | `TextDim` |
| `Primary` | `ConsolePrimary` |
| `Success` | `Success` |
| `Warning` | `Warning` |
| `Error` | `Error` |
| `Info` | `Info` |
| `Syntax_*` | `Syntax*` |
| `Output_*` | `ConsoleOutput*` |
| etc... | (complete mapping needed) |

---

### 1.2 Fix Hardcoded Color.White/Color.Red/etc

**Problem:** Several components use XNA's built-in colors instead of theme colors.

**Files with Hardcoded Colors:**

| File                     | Line      | Issue                                | Fix                                                       |
| ------------------------ | --------- | ------------------------------------ | --------------------------------------------------------- |
| `SuggestionsDropdown.cs` | 544       | `Color.White` for selected text      | Use `Theme.TextPrimary` or add `AutoCompleteSelectedText` |
| `ConsoleScene.cs`        | 1030-1102 | `Color.Transparent` for backgrounds  | OK for transparency, but verify                           |
| `HintBar.cs`             | 21        | Default `Color.Transparent`          | OK for optional background                                |
| `ConsoleSystem.cs`       | 332       | `Color.White` passed to AppendOutput | Use `ThemeManager.Current.ConsoleOutputDefault`           |
| `ConsoleLogger.cs`       | 106       | `Color.White` for Information level  | Use `ThemeManager.Current.TextPrimary`                    |
| `UIRenderer.cs`          | 46        | `Color.White` for pixel texture      | OK - internal implementation                              |

**Tasks:**

- [x] Fix `SuggestionsDropdown.cs:544` - selected text should use theme color ✅ Now uses `ThemeManager.Current.TextPrimary`
- [x] Fix `ConsoleSystem.cs:330-332` - use theme colors ✅ Now uses theme.TextSecondary, theme.TextDim, theme.TextPrimary
- [x] Fix `ConsoleLogger.cs:106` - use theme color for log level ✅ All log levels now use theme colors
- [x] Audit all `Color.White`, `Color.Black`, `Color.Red`, etc. usages ✅ Fixed all in UI components
- [x] Add missing theme properties if needed ✅ Existing theme properties were sufficient

---

## 🟠 Priority 2: Inconsistencies

### 2.1 Consolidate LineHeight Values

**Problem:** Multiple conflicting LineHeight values across the codebase.

| Source                                  | Value | Location                                                                  |
| --------------------------------------- | ----- | ------------------------------------------------------------------------- |
| `UITheme.LineHeight`                    | 20    | Theme definition                                                          |
| `ConsoleConstants.Rendering.LineHeight` | 16    | Legacy constants                                                          |
| Hardcoded                               | 20f   | `StatusBar.cs:100`                                                        |
| Hardcoded                               | 25f   | `EntityInspector.cs:99`, `StatsPanel.cs:67`, `LiveEntityInspector.cs:203` |
| Property default                        | 20    | `TextBuffer.cs:90`                                                        |

**Tasks:**

- [x] Decide on canonical LineHeight value (20 seems standard) ✅ LineHeight=20 for text, PanelRowHeight=25 for UI rows
- [x] Update all hardcoded `lineHeight = 25` to use theme ✅ Now uses `ThemeManager.Current.PanelRowHeight`
- [x] Remove `ConsoleConstants.Rendering.LineHeight` ✅ Removed (was value 16, unused)
- [x] Update `TextBuffer.LineHeight` default to reference theme ✅ Default matches theme value
- [x] Added `PanelRowHeight = 25` to UITheme for panel row items ✅ All 7 themes updated

---

### 2.2 Remove Duplicate Padding Constants

**Problem:** `ConsoleConstants.Rendering.Padding_*` duplicates `UITheme.Padding*`

**ConsoleConstants:**

```csharp
public const int Padding_Tiny = 2;
public const int Padding_Small = 4;
public const int Padding_Medium = 8;
public const int Padding_Large = 12;
public const int Padding_XLarge = 20;
```

**UITheme:**

```csharp
PaddingTiny = 2,
PaddingSmall = 4,
PaddingMedium = 8,
PaddingLarge = 12,
PaddingXLarge = 20,
```

**Tasks:**

- [x] Find all usages of `ConsoleConstants.Rendering.Padding_*` ✅ None found - orphaned code
- [x] Replace with `ThemeManager.Current.Padding*` ✅ Not needed
- [x] Remove the padding constants from `ConsoleConstants.Rendering` ✅ Removed all 5 Padding\_\* constants

---

### 2.3 Standardize Theme Access Pattern

**Problem:** Components access theme colors inconsistently.

**Pattern A (Preferred):** Nullable backing field with getter fallback

```csharp
private Color? _backgroundColor;
public Color BackgroundColor {
    get => _backgroundColor ?? ThemeManager.Current.BackgroundElevated;
    set => _backgroundColor = value;
}
```

**Pattern B (Avoid):** Direct property with hardcoded default

```csharp
public float Padding { get; set; } = 8f;
```

**Tasks:**

- [x] Audit all color properties in UI components ✅ All use nullable backing field pattern
- [x] Convert Pattern B properties to Pattern A where appropriate ✅ Colors use Pattern A; sizing uses simple defaults (acceptable)
- [ ] Document the standard pattern in code guidelines (optional, patterns are consistent)

---

### 2.4 Fix Unused Theme Parameter in UIContext

**Problem:** `UIContext` constructor accepts a theme but ignores it.

```csharp
public UIContext(GraphicsDevice graphicsDevice, UITheme? theme = null)
{
    _renderer = new UIRenderer(graphicsDevice);
    _theme = theme ?? UITheme.Dark;  // Stored but never used!
    ...
}

public UITheme Theme => ThemeManager.Current;  // Always uses global
```

**Tasks:**

- [x] Decide: Remove the parameter, or use it for per-context themes ✅ Removed (use ThemeManager for global theming)
- [x] If removing: Clean up constructor and `_theme` field ✅ Removed `_theme` field and `theme` parameter
- [x] Updated `SetFontSystem` to use `ThemeManager.Current.FontSize` ✅

---

## 🟡 Priority 3: Code Smells

### 3.1 Fix Magic Numbers in Components

**Components with hardcoded values that should be theme properties:**

| Component             | Hardcoded               | Should Use                                  |
| --------------------- | ----------------------- | ------------------------------------------- |
| `SuggestionsDropdown` | `ScrollbarWidth = 6f`   | `Theme.ScrollbarWidth` (10)                 |
| `SuggestionsDropdown` | `ScrollbarPadding = 4f` | New: `Theme.ScrollbarPadding`               |
| `SuggestionsDropdown` | `ItemHeight = 30`       | `Theme.DropdownItemHeight` (25 - mismatch!) |
| `SuggestionsDropdown` | `BorderThickness = 2`   | `Theme.BorderWidth` (1 - mismatch!)         |
| `TextBuffer`          | `ScrollbarWidth = 10`   | `Theme.ScrollbarWidth`                      |
| `TextBuffer`          | `ScrollbarPadding = 4`  | New: `Theme.ScrollbarPadding`               |
| `TextBuffer`          | `LineHeight = 20`       | `Theme.LineHeight`                          |
| Various panels        | `BorderThickness = 1`   | `Theme.BorderWidth`                         |

**Tasks:**

- [x] Add missing theme properties: `ScrollbarPadding` ✅ Added to all 7 themes
- [x] Update `SuggestionsDropdown` to use theme scrollbar values ✅ Now uses ThemeManager.Current
- [x] Document that `ItemHeight = 30` and `BorderThickness = 2` are component-specific overrides ✅
- [x] Update `TextBuffer` comments to note defaults match theme values ✅

---

### 3.2 Optimize Derived Color Computation

**Problem:** Some colors are computed on every property access:

```csharp
public Color HoverColor {
    get => _hoverColor ?? new Color(
        ThemeManager.Current.BackgroundElevated.R,
        ThemeManager.Current.BackgroundElevated.G,
        ThemeManager.Current.BackgroundElevated.B,
        (byte)100
    );
    set => _hoverColor = value;
}
```

**Files Affected:**

- `TextBuffer.cs:84-86` - `HoverColor`, `CursorLineColor`

**Tasks:**

- [x] Option A: Add pre-computed derived colors to `UITheme` ✅ Implemented
  - Added `HoverBackground` (BackgroundElevated with alpha 100)
  - Added `CursorLineHighlight` (Info with alpha 80)
- [x] Apply chosen solution to `TextBuffer.cs` ✅ Now uses `ThemeManager.Current.HoverBackground` and `.CursorLineHighlight`

---

### 3.3 Consider Simplifying UITheme

**Problem:** `UITheme` has 100+ properties, with potential duplication.

**Potential Duplicates:**
| Property A | Property B | Same in Most Themes? |
|------------|------------|---------------------|
| `TextPrimary` | `ConsoleOutputDefault` | Yes |
| `ConsolePrimary` | `Prompt` | Usually |
| `Success` | `ConsoleOutputSuccess` | Yes |
| `Error` | `ConsoleOutputError` | Yes |
| `Warning` | `ConsoleOutputWarning` | Yes |
| `Info` | `ConsoleOutputInfo` | Sometimes different |

**Tasks:**

- [x] Analyze which console-specific colors are actually different from base colors ✅ Analysis complete

**Analysis Results:**
| Console Property | Base Equivalent | Same in All Themes? |
|-----------------|-----------------|---------------------|
| `ConsoleOutputDefault` | `TextPrimary` | ✅ YES - always identical |
| `ConsoleOutputSuccess` | `Success` | ✅ YES - always identical |
| `ConsoleOutputWarning` | `Warning` | ✅ YES - always identical |
| `ConsoleOutputError` | `Error` | ✅ YES - always identical |
| `ConsoleOutputInfo` | `Info` | ❌ NO - sometimes uses cyan instead of blue |

**Decision:** Keep separate properties.

- **Reason 1:** `ConsoleOutputInfo` intentionally differs from `Info` in some themes (cyan vs blue for better console readability)
- **Reason 2:** Consolidating would require migrating all usages - risk not worth the minimal gain
- **Reason 3:** Current system is flexible and works correctly

---

## 🟢 Priority 4: Future Improvements

### 4.1 Add Light Theme Support

**Current State:** All 7 themes are dark themes.

**Tasks:**

- [x] Create `UITheme.SolarizedLight` theme ✅ Added with proper light background colors
- [x] Register in ThemeManager as "solarized-light" ✅
- [x] Update ThemeCommand usage string ✅
- [ ] Consider adding more light themes (GithubLight, etc.) - future enhancement
- [ ] Test contrast ratios for accessibility - future enhancement

---

### 4.2 Theme Persistence

**Tasks:**

- [x] Save selected theme to user preferences ✅ Saves to `%APPDATA%/PokeSharp/theme_preference.json`
- [x] Load theme on startup ✅ ThemeManager loads saved preference on initialization
- [x] Persists automatically when `SetTheme()` is called ✅

---

### 4.3 Theme Hot-Reload

**Current State:** `ThemeManager.ThemeChanged` event exists but may not trigger all components to update.

**Tasks:**

- [x] Verify all components respond to theme changes ✅ All use dynamic `ThemeManager.Current` access
- [x] No refresh mechanism needed - components read theme on every render frame ✅
- [ ] Test theme switching in-game - manual testing recommended

---

## Testing Checklist

After each set of changes:

- [ ] Switch between all 7 themes and verify visual consistency
- [ ] Check console panel, logs panel, watch panel, variables panel, entities panel
- [ ] Verify autocomplete dropdown colors
- [ ] Check parameter hints tooltip
- [ ] Test search highlighting
- [ ] Verify scrollbar colors
- [ ] Check selection highlighting
- [ ] Test in different console sizes (small/medium/full)

---

## Notes

### Files to Delete After Migration

- `PokeSharp.Engine.Debug/Console/Configuration/ConsoleColors.cs`

### Files Most Affected

- `PokeSharp.Engine.UI.Debug/Core/UITheme.cs`
- `PokeSharp.Engine.UI.Debug/Core/ThemeManager.cs`
- `PokeSharp.Engine.Debug/Console/Configuration/ConsoleConstants.cs`
- All files in `PokeSharp.Engine.UI.Debug/Components/`

### Related Documentation

- `docs/planning/REFACTORING_ACTION_PLAN.md` (mentions ConsoleColors migration)
