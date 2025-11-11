# PokeSharp.Engine.Systems.Tests

Unit tests for the `PokeSharp.Engine.Systems` project.

## Test Coverage

### Parallel System Execution
- **ParallelSystemManagerTests** - Tests for parallel system execution, dependency resolution, and execution planning.

### Performance Tracking
- **SystemPerformanceTrackerTests** - Tests for system performance monitoring, metrics collection, and slow system detection.

## Running Tests

```bash
# Run all tests in this project
dotnet test tests/PokeSharp.Engine.Systems.Tests

# Run with verbose output
dotnet test tests/PokeSharp.Engine.Systems.Tests --logger "console;verbosity=detailed"

# Run with code coverage
dotnet test tests/PokeSharp.Engine.Systems.Tests /p:CollectCoverage=true
```

## Test Structure

```
PokeSharp.Engine.Systems.Tests/
├── Parallel/
│   └── ParallelSystemManagerTests.cs    # Tests for parallel execution
└── Management/
    └── SystemPerformanceTrackerTests.cs # Tests for performance tracking
```

## Dependencies

- xUnit - Test framework
- FluentAssertions - Assertion library
- Moq - Mocking framework
- Arch.Core - ECS framework

