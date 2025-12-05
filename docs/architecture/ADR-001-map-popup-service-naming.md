# ADR 001: MapPopupService Naming Strategy

**Status:** PROPOSED
**Date:** 2025-12-05
**Decision Makers:** Architecture Team
**Tags:** #naming #refactoring #architecture #services

---

## Context

Two classes named `MapPopupService` exist in different namespaces:

1. **`GameData.Services.MapPopupService`** - Data access layer for popup themes and map sections
2. **`Engine.Scenes.Services.MapPopupService`** - Orchestrates popup display during map transitions

This naming collision creates several problems:

### Problems

1. **Ambiguity** - Developers must mentally disambiguate which service is being referenced
2. **Fully Qualified Names Required** - Forces verbose code like:
   ```csharp
   private readonly GameData.Services.MapPopupService _mapPopupDataService;
   private Engine.Scenes.Services.MapPopupService? _mapPopupService;
   ```
3. **IntelliSense Confusion** - IDE suggestions are unclear
4. **Code Review Friction** - Reviewers must check namespace imports to understand references
5. **Documentation Overhead** - Extra clarification needed in all documentation

### Current Workarounds

- Fully qualified names in fields and properties
- Namespace aliases in some files
- Verbose comments explaining which service is which
- Increased cognitive load for developers

---

## Decision

**Rename the services to clearly distinguish their responsibilities:**

| Current Name | New Name | Namespace | Interface |
|--------------|----------|-----------|-----------|
| `MapPopupService` | **`MapPopupDataService`** | `GameData.Services` | `IMapPopupDataService` |
| `MapPopupService` | **`MapPopupOrchestrator`** | `Engine.Scenes.Services` | `IMapPopupOrchestrator` |

### Rationale for Names

#### MapPopupDataService
- **"Data"** clarifies this is the data access layer
- **"Service"** follows repository/service pattern conventions
- Accurately describes responsibility: querying EF Core and caching
- Fits existing naming patterns (`NpcDefinitionService`, `MapDefinitionService`)

#### MapPopupOrchestrator
- **"Orchestrator"** clearly indicates coordination/orchestration role
- Distinguishes from data services
- Accurately describes responsibility: event handling, scene management, coordination
- Follows established patterns (Command, Orchestrator, Coordinator)

### Interface Introduction

Both services will implement interfaces for:
- **Testability** - Easy to mock in unit tests
- **Flexibility** - Future implementations can be swapped
- **Clarity** - Explicit contracts define behavior
- **Dependency Injection** - Cleaner DI registration

---

## Consequences

### Positive

1. ✅ **No Ambiguity** - Names clearly indicate purpose
2. ✅ **Cleaner Code** - No fully qualified names needed
3. ✅ **Better IntelliSense** - Clear, meaningful suggestions
4. ✅ **Self-Documenting** - Code is easier to understand
5. ✅ **Testability** - Interfaces enable better testing
6. ✅ **SOLID Compliance** - Single Responsibility, Interface Segregation
7. ✅ **Improved Maintainability** - Clear separation of concerns

### Negative

1. ⚠️ **Breaking Change** - Code referencing old names must be updated
2. ⚠️ **Documentation Updates** - All docs must be revised
3. ⚠️ **Learning Curve** - Developers must learn new names
4. ⚠️ **External Mods** - Third-party code may break (if any exists)

### Mitigation

- **Comprehensive migration guide** for developers
- **All changes in single PR** to avoid confusion
- **Clear commit messages** documenting changes
- **Update all documentation** simultaneously
- **Thorough testing** before merge

### Neutral

- **File Renames** - 2 files renamed (IDE handles well)
- **Interface Files** - 2 new files created (minimal overhead)
- **Service Registration** - Updated to use interfaces (standard practice)

---

## Alternatives Considered

### Alternative 1: Keep Names, Use Namespaces
**Approach:** Keep both named `MapPopupService`, rely on namespaces

**Pros:**
- No code changes needed
- No breaking changes

**Cons:**
- Doesn't solve the ambiguity problem
- Still requires fully qualified names
- Continues developer confusion

**Decision:** ❌ Rejected - Doesn't address the core issue

---

### Alternative 2: Suffix-Based Naming
**Approach:** Add minimal suffixes (e.g., `MapPopupQueryService`, `MapPopupSceneService`)

**Pros:**
- Minimal change to names
- Easy to understand suffixes

**Cons:**
- Less descriptive than responsibility-based names
- "Query" doesn't capture caching responsibility
- "Scene" doesn't capture orchestration responsibility
- Missed opportunity for better naming

**Decision:** ❌ Rejected - Less expressive than chosen approach

---

### Alternative 3: Merge Services
**Approach:** Combine both into a single service

**Pros:**
- No naming collision
- Single service to understand

**Cons:**
- Violates Single Responsibility Principle
- Mixes data access with orchestration
- Creates tight coupling
- Makes testing harder
- Reduces flexibility

**Decision:** ❌ Rejected - Architecturally unsound

---

### Alternative 4: Layer-Based Naming
**Approach:** Use `MapPopupRepository` and `MapPopupDisplayService`

**Pros:**
- "Repository" is accurate for data layer
- Clear layer distinction

**Cons:**
- "Repository" might be too strict (service does more than just data access)
- "DisplayService" is less specific than "Orchestrator"
- Doesn't align as well with existing patterns

**Decision:** ⚠️ Viable alternative, but chosen approach is more descriptive

---

## Implementation Plan

See detailed implementation plan: [`docs/architecture/map-popup-service-refactoring-plan.md`](./map-popup-service-refactoring-plan.md)

**Summary:**
1. **Phase 1:** Create interfaces (non-breaking)
2. **Phase 2:** Rename data service
3. **Phase 3:** Rename orchestrator
4. **Phase 4:** Update documentation
5. **Phase 5:** Validate and test

**Estimated Effort:** 4-6 hours
**Risk Level:** Low (compiler catches all references)

---

## Architectural Principles Applied

### 1. Single Responsibility Principle (SRP)
Each service has one clear responsibility:
- **Data Service:** Data access and caching
- **Orchestrator:** Event handling and scene coordination

### 2. Interface Segregation Principle (ISP)
Interfaces define clear contracts:
- **`IMapPopupDataService`:** Data operations only
- **`IMapPopupOrchestrator`:** Orchestration only (marker interface)

### 3. Dependency Inversion Principle (DIP)
Dependencies on abstractions (interfaces), not concretions

### 4. Open/Closed Principle (OCP)
Open for extension (new implementations), closed for modification (stable interfaces)

### 5. Naming Convention Best Practices
- **Descriptive** - Names describe purpose
- **Consistent** - Follows project patterns
- **Unambiguous** - No confusion possible
- **Memorable** - Easy to recall and use

---

## Validation Criteria

### Success Metrics

1. ✅ **Zero Ambiguity** - No confusion about which service is being referenced
2. ✅ **No Fully Qualified Names** - Clean, readable code throughout
3. ✅ **Zero Compilation Errors** - All references updated correctly
4. ✅ **All Tests Pass** - No functional regressions
5. ✅ **Documentation Current** - All docs accurately reflect new names
6. ✅ **Performance Maintained** - No degradation from changes

### Acceptance Tests

1. **Compile Check:**
   ```bash
   dotnet build --no-incremental
   # Expected: 0 errors, 0 warnings
   ```

2. **Reference Check:**
   ```bash
   grep -r "GameData\.Services\.MapPopupService" --include="*.cs"
   # Expected: 0 matches (except in migration docs)
   ```

3. **Functional Test:**
   - Load game → Verify popup shows
   - Transition map → Verify popup shows with correct theme
   - Check logs → Verify data service loads themes/sections

4. **Performance Benchmark:**
   - 10,000 `GetPopupDisplayInfo()` calls < 5ms
   - No memory leaks in event subscriptions

---

## Related Documents

- [Map Popup Service Refactoring Plan](./map-popup-service-refactoring-plan.md) - Detailed implementation guide
- [Map Popup Themes and Sections](../features/map-popup-themes-sections.md) - Original feature documentation
- [Prevent Double Popups](../bugfixes/prevent-double-popups.md) - Related bug fix
- [Popup Map Name Formatting](../bugfixes/popup-map-name-formatting.md) - Related bug fix

---

## References

### Design Patterns
- **Repository Pattern** - Data access abstraction
- **Orchestrator Pattern** - Coordination of multiple services
- **Event-Driven Architecture** - Decoupled communication

### Naming Resources
- Martin Fowler - [Naming Conventions](https://martinfowler.com/bliki/TwoHardThings.html)
- Uncle Bob - Clean Code, Chapter 2: Meaningful Names
- Microsoft - [.NET Naming Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-12-05 | Proposed ADR | Naming collision causing developer friction |
| TBD | Accepted/Rejected | Pending architecture team review |

---

## Stakeholder Sign-off

- [ ] **Architecture Team Lead** - Approves design approach
- [ ] **Senior Developer** - Reviews implementation feasibility
- [ ] **QA Lead** - Approves testing strategy
- [ ] **Tech Lead** - Final approval to proceed

---

**ADR Status:** PROPOSED - Awaiting Review
**Next Steps:**
1. Architecture team review
2. Team discussion/feedback
3. Approval/rejection decision
4. Implementation (if approved)

---

*This ADR follows the format described in Michael Nygard's article "Documenting Architecture Decisions"*
