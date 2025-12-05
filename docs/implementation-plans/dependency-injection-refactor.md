# Dependency Injection Refactor - Implementation Plan

## Executive Summary

This plan addresses the **Service Locator anti-pattern** in `MapPopupScene` and establishes proper dependency injection patterns for the entire scene architecture.

**Current Issues:**
- `MapPopupScene` uses `Services.GetService()` to resolve dependencies at runtime (lines 319, 582, 228)
- `SceneBase` exposes `IServiceProvider` which encourages service locator usage
- Hidden dependencies make unit testing difficult
- Null checks required at runtime for critical dependencies
- No compile-time dependency validation

**Benefits of Refactor:**
- ✅ **Explicit dependencies** - Constructor clearly shows what each scene needs
- ✅ **Compile-time validation** - Missing dependencies caught at startup, not runtime
- ✅ **Testability** - Easy to mock dependencies for unit tests
- ✅ **Maintainability** - Clear dependency graph visible in code
- ✅ **No runtime failures** - No null checks needed for required dependencies

---

## 1. Constructor Injection

### 1.1 Current State Analysis

**MapPopupScene currently has:**
- **Explicit dependencies** (constructor-injected):
  - `GraphicsDevice` ✅
  - `IServiceProvider` ❌ (service locator)
  - `ILogger<MapPopupScene>` ✅
  - `IAssetProvider` ✅
  - `PopupBackgroundDefinition` ✅
  - `PopupOutlineDefinition` ✅
  - `string mapName` ✅

- **Hidden dependencies** (resolved via service locator):
  - `SceneManager` (line 319)
  - `Arch.Core.World` (line 582)
  - Second `ILogger<MapPopupScene>` (line 228 - duplicates constructor param!)

### 1.2 Proposed Constructor Signature

```csharp
public class MapPopupScene : SceneBase
{
    private readonly IAssetProvider _assetProvider;
    private readonly PopupBackgroundDefinition _backgroundDef;
    private readonly PopupOutlineDefinition _outlineDef;
    private readonly string _mapName;
    private readonly SceneManager _sceneManager;
    private readonly World _world;

    // Camera caching
    private Camera? _cachedCamera;
    private int _cameraRefreshCounter = 0;
    private const int CameraRefreshInterval = 30;

    public MapPopupScene(
        GraphicsDevice graphicsDevice,
        ILogger<MapPopupScene> logger,
        IAssetProvider assetProvider,
        PopupBackgroundDefinition backgroundDefinition,
        PopupOutlineDefinition outlineDefinition,
        string mapName,
        SceneManager sceneManager,  // ✅ NEW: Injected dependency
        World world                   // ✅ NEW: Injected dependency
    )
        : base(graphicsDevice, logger)  // ✅ No IServiceProvider!
    {
        ArgumentNullException.ThrowIfNull(assetProvider);
        ArgumentNullException.ThrowIfNull(backgroundDefinition);
        ArgumentNullException.ThrowIfNull(outlineDefinition);
        ArgumentException.ThrowIfNullOrEmpty(mapName);
        ArgumentNullException.ThrowIfNull(sceneManager);  // ✅ Validated at construction
        ArgumentNullException.ThrowIfNull(world);          // ✅ Validated at construction

        _assetProvider = assetProvider;
        _backgroundDef = backgroundDefinition;
        _outlineDef = outlineDefinition;
        _mapName = mapName;
        _sceneManager = sceneManager;  // ✅ Store reference
        _world = world;                 // ✅ Store reference

        // Configure scene behavior
        RenderScenesBelow = true;
        UpdateScenesBelow = true;
        ExclusiveInput = false;

        logger.LogDebug(
            "MapPopupScene created - Map: '{MapName}', Background: {BgId}, Outline: {OutlineId}",
            mapName,
            backgroundDefinition.Id,
            outlineDefinition.Id
        );
    }
}
```

**Rationale:**
- **8 explicit parameters** is acceptable for a leaf class (not a widely-used base)
- All dependencies are **required** for scene operation
- No need for facade pattern here (unlike `GameplayScene` with 11+ dependencies)
- Constructor validates all dependencies at creation time

---

## 2. Scene Factory Pattern

### 2.1 Problem: Who Creates Scenes?

Currently, `MapPopupService` (line 225-234) creates `MapPopupScene` directly:

```csharp
// ❌ CURRENT: Service knows about scene constructor details
var popupScene = new MapPopupScene(
    _graphicsDevice,
    _services,
    _services.GetService(typeof(ILogger<MapPopupScene>)) as ILogger<MapPopupScene>
        ?? throw new InvalidOperationException("ILogger<MapPopupScene> not found in services"),
    _assetProvider,
    backgroundDef,
    outlineDef,
    displayName
);
```

**Problems:**
- Service is tightly coupled to scene constructor signature
- Changes to scene dependencies break the service
- `ILogger` resolution is verbose and error-prone
- No centralized scene creation logic

### 2.2 Solution: ISceneFactory Interface

Create a dedicated factory for scene creation with proper DI:

```csharp
namespace MonoBallFramework.Game.Engine.Scenes.Factories;

/// <summary>
/// Factory for creating game scenes with proper dependency injection.
/// Scenes should be created through factories, not directly instantiated.
/// </summary>
public interface ISceneFactory
{
    /// <summary>
    /// Creates a new MapPopupScene instance.
    /// </summary>
    MapPopupScene CreateMapPopupScene(
        PopupBackgroundDefinition backgroundDefinition,
        PopupOutlineDefinition outlineDefinition,
        string mapName
    );

    // Future: Add factory methods for other scenes as needed
    // GameplayScene CreateGameplayScene(GameplaySceneContext context);
    // LoadingScene CreateLoadingScene(string loadingMessage);
}
```

### 2.3 SceneFactory Implementation

```csharp
namespace MonoBallFramework.Game.Engine.Scenes.Factories;

/// <summary>
/// Default implementation of ISceneFactory.
/// Resolves scene dependencies from IServiceProvider.
/// </summary>
public class SceneFactory : ISceneFactory
{
    private readonly IServiceProvider _services;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IAssetProvider _assetProvider;
    private readonly SceneManager _sceneManager;
    private readonly World _world;

    public SceneFactory(
        IServiceProvider services,
        GraphicsDevice graphicsDevice,
        ILoggerFactory loggerFactory,
        IAssetProvider assetProvider,
        SceneManager sceneManager,
        World world
    )
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _assetProvider = assetProvider ?? throw new ArgumentNullException(nameof(assetProvider));
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public MapPopupScene CreateMapPopupScene(
        PopupBackgroundDefinition backgroundDefinition,
        PopupOutlineDefinition outlineDefinition,
        string mapName
    )
    {
        ArgumentNullException.ThrowIfNull(backgroundDefinition);
        ArgumentNullException.ThrowIfNull(outlineDefinition);
        ArgumentException.ThrowIfNullOrEmpty(mapName);

        var logger = _loggerFactory.CreateLogger<MapPopupScene>();

        return new MapPopupScene(
            _graphicsDevice,
            logger,
            _assetProvider,
            backgroundDefinition,
            outlineDefinition,
            mapName,
            _sceneManager,
            _world
        );
    }
}
```

### 2.4 DI Registration

Add to your DI container configuration:

```csharp
// In your Startup or DI configuration
services.AddSingleton<ISceneFactory, SceneFactory>();
```

### 2.5 Updated MapPopupService

```csharp
public class MapPopupService : IDisposable
{
    private readonly World _world;
    private readonly SceneManager _sceneManager;
    private readonly ISceneFactory _sceneFactory;  // ✅ Use factory instead of manual creation
    private readonly PopupRegistry _popupRegistry;
    private readonly GameData.Services.MapPopupService _mapPopupDataService;
    private readonly ILogger<MapPopupService> _logger;
    // ... other fields

    public MapPopupService(
        World world,
        SceneManager sceneManager,
        ISceneFactory sceneFactory,  // ✅ Inject factory
        PopupRegistry popupRegistry,
        IEventBus eventBus,
        ILogger<MapPopupService> logger
    )
    {
        // ... validation and initialization
        _sceneFactory = sceneFactory;
    }

    private void ShowPopupForMap(int mapId, string displayName, string? regionName)
    {
        // ... existing validation logic ...

        // ✅ CLEAN: Use factory to create scene
        var popupScene = _sceneFactory.CreateMapPopupScene(
            backgroundDef,
            outlineDef,
            displayName
        );

        _sceneManager.PushScene(popupScene);

        _logger.LogInformation(
            "Displayed map popup: '{DisplayName}' (Region: {RegionName}, Theme: {ThemeId})",
            displayName, regionName ?? "None", usedThemeId
        );
    }
}
```

**Benefits:**
- ✅ `MapPopupService` only knows about scene **contract**, not **implementation**
- ✅ Scene constructor changes don't break service
- ✅ Centralized scene creation logic
- ✅ Easy to add caching/pooling later if needed

---

## 3. Base Class Changes

### 3.1 Current SceneBase Analysis

```csharp
// ❌ CURRENT: Exposes IServiceProvider
public abstract class SceneBase : IScene
{
    protected SceneBase(
        GraphicsDevice graphicsDevice,
        IServiceProvider services,  // ❌ Service Locator enabler
        ILogger logger,
        string contentRootDirectory = "Content"
    )
    {
        Services = services;  // ❌ Protected property encourages anti-pattern
    }

    protected IServiceProvider Services { get; }  // ❌ Available to all derived classes
}
```

### 3.2 Proposed SceneBase Refactor

```csharp
/// <summary>
/// Base class for game scenes that provides common functionality.
/// Uses proper dependency injection - does NOT expose IServiceProvider.
/// </summary>
public abstract class SceneBase : IScene
{
    private readonly object _lock = new();
    private SceneState _state = SceneState.Uninitialized;

    /// <summary>
    /// Initializes a new instance of the SceneBase class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    /// <param name="logger">The logger for this scene.</param>
    /// <param name="contentRootDirectory">Root directory for content loading.</param>
    protected SceneBase(
        GraphicsDevice graphicsDevice,
        ILogger logger,
        string contentRootDirectory = "Content"
    )
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(contentRootDirectory);

        GraphicsDevice = graphicsDevice;
        Logger = logger;

        // ✅ Create ContentManager WITHOUT IServiceProvider
        // ContentManager only uses IServiceProvider for IGraphicsDeviceService
        // We can pass null since MonoGame's ContentManager will use GraphicsDevice directly
        Content = new ContentManager(new ServiceContainer(), contentRootDirectory);

        // Register GraphicsDevice service for ContentManager
        var serviceContainer = Content.ServiceProvider as IServiceContainer;
        serviceContainer?.AddService(typeof(IGraphicsDeviceService),
            new GraphicsDeviceServiceProvider(graphicsDevice));
    }

    protected ContentManager Content { get; }
    protected GraphicsDevice GraphicsDevice { get; }
    protected ILogger Logger { get; }

    // ✅ No IServiceProvider property!

    // ... rest of implementation ...
}
```

### 3.3 GraphicsDeviceServiceProvider Helper

```csharp
namespace MonoBallFramework.Game.Engine.Scenes;

/// <summary>
/// Minimal IGraphicsDeviceService implementation for ContentManager.
/// </summary>
internal class GraphicsDeviceServiceProvider : IGraphicsDeviceService
{
    public GraphicsDeviceServiceProvider(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    }

    public GraphicsDevice GraphicsDevice { get; }

#pragma warning disable CS0067 // Event never used (required by interface)
    public event EventHandler<EventArgs>? DeviceCreated;
    public event EventHandler<EventArgs>? DeviceDisposing;
    public event EventHandler<EventArgs>? DeviceReset;
    public event EventHandler<EventArgs>? DeviceResetting;
#pragma warning restore CS0067
}
```

### 3.4 Migration Strategy for Existing Scenes

**Option A: Clean Break (Recommended)**

Remove `IServiceProvider` parameter from `SceneBase` entirely:

```csharp
// Before
public GameplayScene(
    GraphicsDevice graphicsDevice,
    IServiceProvider services,  // ❌ Remove
    ILogger<GameplayScene> logger,
    GameplaySceneContext context
) : base(graphicsDevice, services, logger)

// After
public GameplayScene(
    GraphicsDevice graphicsDevice,
    ILogger<GameplayScene> logger,
    GameplaySceneContext context
) : base(graphicsDevice, logger)
```

**Required Changes:**
1. Update all scene constructors to remove `IServiceProvider`
2. Update `SceneManager` to not pass `IServiceProvider` to scenes
3. Update scene creation points (factories, services)

**Affected Files:**
- `/MonoBallFramework.Game/Engine/Scenes/SceneBase.cs` (base class)
- `/MonoBallFramework.Game/Engine/Scenes/Scenes/MapPopupScene.cs`
- `/MonoBallFramework.Game/Scenes/GameplayScene.cs`
- `/MonoBallFramework.Game/Engine/Scenes/Scenes/LoadingScene.cs`
- `/MonoBallFramework.Game/Engine/UI/Scenes/ConsoleScene.cs`
- All scene creation/factory code

**Option B: Deprecation Path (Low-Risk)**

Keep `IServiceProvider` parameter but mark as obsolete:

```csharp
protected SceneBase(
    GraphicsDevice graphicsDevice,
    [Obsolete("Use constructor without IServiceProvider")]
    IServiceProvider? services,  // ✅ Nullable + obsolete
    ILogger logger,
    string contentRootDirectory = "Content"
)
{
    // ... validation ...

    if (services != null)
    {
        Logger.LogWarning(
            "Scene {SceneType} is using obsolete IServiceProvider dependency. " +
            "Refactor to use constructor injection instead.",
            GetType().Name
        );
    }
}

// ✅ New preferred constructor
protected SceneBase(
    GraphicsDevice graphicsDevice,
    ILogger logger,
    string contentRootDirectory = "Content"
) : this(graphicsDevice, null, logger, contentRootDirectory)
{
}
```

**Recommendation:** Use **Option A (Clean Break)** - Only ~5 scenes exist, easier to fix now than later.

---

## 4. Camera Access Pattern

### 4.1 Current Anti-Pattern

```csharp
private Camera? GetGameCamera()
{
    // ❌ Service locator + ECS query
    var world = Services.GetService(typeof(Arch.Core.World)) as Arch.Core.World;
    if (world == null) return null;

    var cameraQuery = new QueryDescription().WithAll<Camera>();
    Camera? camera = null;
    world.Query(in cameraQuery, (Entity entity, ref Camera cam) => { camera = cam; });

    return camera;
}
```

### 4.2 Proposed Solutions

**Option 1: Camera Provider Interface (Recommended)**

Create abstraction for camera access:

```csharp
namespace MonoBallFramework.Game.Engine.Rendering.Services;

/// <summary>
/// Provides access to the active game camera.
/// Abstracts ECS implementation details from scenes.
/// </summary>
public interface ICameraProvider
{
    /// <summary>
    /// Gets the currently active camera, or null if no camera exists.
    /// </summary>
    Camera? GetActiveCamera();
}

/// <summary>
/// ECS-based camera provider with caching for performance.
/// </summary>
public class EcsCameraProvider : ICameraProvider
{
    private readonly World _world;
    private Camera? _cachedCamera;
    private int _cacheAge;
    private const int CacheInvalidationFrames = 30;

    public EcsCameraProvider(World world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public Camera? GetActiveCamera()
    {
        // Refresh cache every N frames
        if (_cacheAge++ >= CacheInvalidationFrames)
        {
            _cacheAge = 0;
            _cachedCamera = QueryCamera();
        }

        return _cachedCamera;
    }

    private Camera? QueryCamera()
    {
        var cameraQuery = new QueryDescription().WithAll<Camera>();
        Camera? camera = null;

        _world.Query(in cameraQuery, (Entity entity, ref Camera cam) =>
        {
            camera = cam;
        });

        return camera;
    }
}
```

**Updated MapPopupScene:**

```csharp
public class MapPopupScene : SceneBase
{
    private readonly ICameraProvider _cameraProvider;  // ✅ Injected

    public MapPopupScene(
        GraphicsDevice graphicsDevice,
        ILogger<MapPopupScene> logger,
        IAssetProvider assetProvider,
        PopupBackgroundDefinition backgroundDefinition,
        PopupOutlineDefinition outlineDefinition,
        string mapName,
        SceneManager sceneManager,
        ICameraProvider cameraProvider  // ✅ Inject camera provider
    )
        : base(graphicsDevice, logger)
    {
        // ... validation ...
        _cameraProvider = cameraProvider;
    }

    public override void Draw(GameTime gameTime)
    {
        // ✅ CLEAN: Use injected provider
        Camera? camera = _cameraProvider.GetActiveCamera();
        if (!camera.HasValue) return;

        // ... rendering code ...
    }
}
```

**Benefits:**
- ✅ Scenes don't know about ECS internals
- ✅ Easy to mock for testing (`MockCameraProvider`)
- ✅ Caching logic centralized
- ✅ Can swap implementations (network camera, replay camera, etc.)

**Option 2: Pass Camera Directly (Alternative)**

For popup scenes that render on top of gameplay, pass camera as parameter:

```csharp
public class MapPopupScene : SceneBase
{
    private readonly Camera _camera;  // ✅ Stored at construction

    public MapPopupScene(
        GraphicsDevice graphicsDevice,
        ILogger<MapPopupScene> logger,
        // ... other deps ...
        Camera camera  // ✅ Current camera snapshot
    )
    {
        _camera = camera;
    }

    public override void Draw(GameTime gameTime)
    {
        // ✅ Use stored camera
        GraphicsDevice.Viewport = new Viewport(_camera.VirtualViewport);
        // ... rendering ...
    }
}
```

**Trade-offs:**
- ✅ Simplest implementation
- ✅ No caching complexity
- ❌ Camera changes during popup won't be reflected
- ❌ For map popup, this might be acceptable (short-lived scene)

**Recommendation:** Use **Option 1 (ICameraProvider)** for flexibility.

---

## 5. Testing Strategy

### 5.1 Unit Testing MapPopupScene

With proper DI, testing becomes trivial:

```csharp
namespace MonoBallFramework.Engine.Scenes.Tests;

public class MapPopupSceneTests
{
    private readonly Mock<ILogger<MapPopupScene>> _mockLogger;
    private readonly Mock<IAssetProvider> _mockAssetProvider;
    private readonly Mock<SceneManager> _mockSceneManager;
    private readonly Mock<ICameraProvider> _mockCameraProvider;
    private readonly GraphicsDevice _graphicsDevice;

    public MapPopupSceneTests()
    {
        _mockLogger = new Mock<ILogger<MapPopupScene>>();
        _mockAssetProvider = new Mock<IAssetProvider>();
        _mockSceneManager = new Mock<SceneManager>();
        _mockCameraProvider = new Mock<ICameraProvider>();

        // Create real GraphicsDevice for testing
        _graphicsDevice = TestHelpers.CreateGraphicsDevice();
    }

    [Fact]
    public void Constructor_ValidatesAllDependencies()
    {
        // ✅ Each null parameter should throw
        Assert.Throws<ArgumentNullException>(() =>
            CreateScene(graphicsDevice: null!));

        Assert.Throws<ArgumentNullException>(() =>
            CreateScene(logger: null!));

        Assert.Throws<ArgumentNullException>(() =>
            CreateScene(assetProvider: null!));

        // ... test all parameters ...
    }

    [Fact]
    public void Update_CompletesAnimation_PopsSelfFromSceneStack()
    {
        // Arrange
        var scene = CreateScene();
        scene.LoadContent();

        var gameTime = new GameTime(
            TimeSpan.FromSeconds(4), // Total (> SlideIn + Display + SlideOut)
            TimeSpan.FromSeconds(0.016) // 60 FPS
        );

        // Act - Run animation to completion
        for (int i = 0; i < 200; i++) // ~3.3 seconds at 60fps
        {
            scene.Update(gameTime);
        }

        // Assert - Verify PopScene was called
        _mockSceneManager.Verify(
            sm => sm.PopScene(),
            Times.Once,
            "Scene should pop itself after animation completes"
        );
    }

    [Fact]
    public void Draw_WithoutCamera_DoesNotCrash()
    {
        // Arrange
        var scene = CreateScene();
        scene.LoadContent();

        _mockCameraProvider
            .Setup(cp => cp.GetActiveCamera())
            .Returns((Camera?)null);  // ✅ No camera

        var gameTime = new GameTime();

        // Act & Assert - Should not throw
        scene.Draw(gameTime);
    }

    [Fact]
    public void Draw_WithValidCamera_RendersPopup()
    {
        // Arrange
        var scene = CreateScene();
        scene.LoadContent();

        var testCamera = new Camera
        {
            VirtualViewport = new Rectangle(0, 0, 240 * 3, 160 * 3)
        };

        _mockCameraProvider
            .Setup(cp => cp.GetActiveCamera())
            .Returns(testCamera);

        var gameTime = new GameTime();

        // Act
        scene.Draw(gameTime);

        // Assert - Verify rendering happened (check asset provider calls)
        _mockAssetProvider.Verify(
            ap => ap.HasTexture(It.IsAny<string>()),
            Times.AtLeastOnce
        );
    }

    private MapPopupScene CreateScene(
        GraphicsDevice? graphicsDevice = null,
        ILogger<MapPopupScene>? logger = null,
        IAssetProvider? assetProvider = null,
        SceneManager? sceneManager = null,
        ICameraProvider? cameraProvider = null
    )
    {
        return new MapPopupScene(
            graphicsDevice ?? _graphicsDevice,
            logger ?? _mockLogger.Object,
            assetProvider ?? _mockAssetProvider.Object,
            CreateMockBackgroundDef(),
            CreateMockOutlineDef(),
            "Test Map",
            sceneManager ?? _mockSceneManager.Object,
            cameraProvider ?? _mockCameraProvider.Object
        );
    }

    private PopupBackgroundDefinition CreateMockBackgroundDef() => new()
    {
        Id = "test_bg",
        TexturePath = "Assets/test.png"
    };

    private PopupOutlineDefinition CreateMockOutlineDef() => new()
    {
        Id = "test_outline",
        TexturePath = "Assets/outline.png",
        BorderWidth = 8
    };
}
```

### 5.2 Integration Testing with SceneFactory

```csharp
public class SceneFactoryIntegrationTests
{
    [Fact]
    public void CreateMapPopupScene_WithRealDependencies_CreatesValidScene()
    {
        // Arrange - Build real DI container
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton<GraphicsDevice>(TestHelpers.CreateGraphicsDevice());
        services.AddSingleton<IAssetProvider, AssetManager>();
        services.AddSingleton<SceneManager>();
        services.AddSingleton<World>(CreateTestWorld());
        services.AddSingleton<ICameraProvider, EcsCameraProvider>();
        services.AddSingleton<ISceneFactory, SceneFactory>();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ISceneFactory>();

        // Act
        var scene = factory.CreateMapPopupScene(
            CreateBackgroundDef(),
            CreateOutlineDef(),
            "Integration Test Map"
        );

        // Assert
        Assert.NotNull(scene);
        Assert.False(scene.IsDisposed);
        Assert.False(scene.IsInitialized); // Not initialized yet
    }
}
```

### 5.3 Testing Benefits

**Before DI (Service Locator):**
```csharp
// ❌ HARD TO TEST
var scene = new MapPopupScene(graphicsDevice, services, logger, ...);
// Need to mock entire IServiceProvider
// Need to set up GetService() for SceneManager, World, etc.
// Runtime failures if mocks not configured correctly
```

**After DI (Constructor Injection):**
```csharp
// ✅ EASY TO TEST
var scene = new MapPopupScene(
    graphicsDevice,
    mockLogger.Object,
    mockAssetProvider.Object,
    backgroundDef,
    outlineDef,
    "Test",
    mockSceneManager.Object,
    mockCameraProvider.Object
);
// All dependencies explicit and mockable
// Compile-time validation
// No runtime surprises
```

---

## 6. Migration Path

### 6.1 Incremental Refactor Steps

**Phase 1: Infrastructure (Low Risk)**
1. ✅ Create `ISceneFactory` interface
2. ✅ Create `SceneFactory` implementation
3. ✅ Create `ICameraProvider` interface and `EcsCameraProvider`
4. ✅ Register in DI container
5. ✅ **No breaking changes** - new code doesn't affect existing code

**Phase 2: MapPopupScene (Medium Risk)**
6. ✅ Update `MapPopupScene` constructor to accept new dependencies
7. ✅ Remove service locator calls from `MapPopupScene`
8. ✅ Update `MapPopupService` to use `ISceneFactory`
9. ✅ Write unit tests for `MapPopupScene`
10. ✅ **Test thoroughly** - this is a visible user-facing feature

**Phase 3: SceneBase Refactor (Higher Risk)**
11. ✅ Create new `SceneBase` constructor without `IServiceProvider`
12. ✅ Update `GameplayScene` to use new constructor
13. ✅ Update `LoadingScene` to use new constructor
14. ✅ Update `ConsoleScene` to use new constructor
15. ✅ **Deprecate** old constructor with `[Obsolete]` attribute
16. ✅ Run full regression testing

**Phase 4: Cleanup**
17. ✅ Remove obsolete constructors
18. ✅ Remove `protected IServiceProvider Services` from `SceneBase`
19. ✅ Update documentation
20. ✅ Final validation

### 6.2 Testing Checkpoints

**After Phase 1:**
- [ ] Project compiles
- [ ] DI container resolves `ISceneFactory` and `ICameraProvider`
- [ ] Existing scenes still work

**After Phase 2:**
- [ ] Map popups display correctly
- [ ] Animation works (slide in, display, slide out)
- [ ] Scene pops from stack after animation
- [ ] No null reference exceptions
- [ ] Unit tests pass

**After Phase 3:**
- [ ] All scenes load correctly
- [ ] Scene transitions work
- [ ] Scene stacking works (pause menu, console, popups)
- [ ] No runtime errors

**After Phase 4:**
- [ ] No obsolete warnings
- [ ] All tests pass
- [ ] Code coverage >80%
- [ ] Documentation updated

### 6.3 Rollback Strategy

Each phase is independent:
- **Phase 1 failure:** Delete new files, no impact
- **Phase 2 failure:** Revert `MapPopupScene` and `MapPopupService`, keep infrastructure
- **Phase 3 failure:** Keep `IServiceProvider` constructor, mark new one as experimental
- **Phase 4 failure:** Keep both constructors, extend deprecation period

---

## 7. File Changes Summary

### Files to Create

```
/MonoBallFramework.Game/Engine/Scenes/Factories/
  ├── ISceneFactory.cs                    ✨ NEW
  └── SceneFactory.cs                     ✨ NEW

/MonoBallFramework.Game/Engine/Rendering/Services/
  ├── ICameraProvider.cs                  ✨ NEW
  └── EcsCameraProvider.cs                ✨ NEW

/MonoBallFramework.Game/Engine/Scenes/
  └── GraphicsDeviceServiceProvider.cs    ✨ NEW

/tests/MonoBallFramework.Engine.Scenes.Tests/
  ├── MapPopupSceneTests.cs               ✨ NEW
  ├── SceneFactoryTests.cs                ✨ NEW
  └── CameraProviderTests.cs              ✨ NEW
```

### Files to Modify

```
/MonoBallFramework.Game/Engine/Scenes/
  ├── SceneBase.cs                        🔧 MODIFY - Remove IServiceProvider
  ├── Scenes/MapPopupScene.cs             🔧 MODIFY - Constructor injection
  └── Services/MapPopupService.cs         🔧 MODIFY - Use ISceneFactory

/MonoBallFramework.Game/Scenes/
  └── GameplayScene.cs                    🔧 MODIFY - Remove IServiceProvider

/MonoBallFramework.Game/Engine/Scenes/Scenes/
  ├── LoadingScene.cs                     🔧 MODIFY - Remove IServiceProvider
  └── ConsoleScene.cs                     🔧 MODIFY - Remove IServiceProvider

/MonoBallFramework.Game/Initialization/
  └── [DI Registration File]              🔧 MODIFY - Register new services
```

### Lines of Code Impact

- **New code:** ~400 lines (interfaces, implementations, tests)
- **Modified code:** ~200 lines (constructor updates, service locator removal)
- **Deleted code:** ~50 lines (service locator calls, null checks)
- **Net change:** ~+550 lines
- **Complexity reduction:** Significant (hidden dependencies → explicit)

---

## 8. Risk Assessment

### Low Risk Changes ✅
- Creating `ISceneFactory`, `ICameraProvider` interfaces
- Creating `SceneFactory`, `EcsCameraProvider` implementations
- Adding new constructors alongside old ones
- Writing new unit tests

### Medium Risk Changes ⚠️
- Updating `MapPopupScene` constructor
- Removing service locator from `MapPopupScene`
- Updating `MapPopupService` to use factory
- **Mitigation:** Comprehensive testing, feature is isolated

### High Risk Changes 🔴
- Removing `IServiceProvider` from `SceneBase`
- Updating all scene constructors
- **Mitigation:** Incremental rollout, deprecation warnings, extensive testing

### Failure Modes

**Compilation Errors:**
- Missing dependencies in scene constructors
- **Detection:** Immediate at build time
- **Fix:** Add missing parameters

**Runtime Errors:**
- Null reference when accessing injected dependencies
- **Detection:** Unit tests, integration tests
- **Fix:** Add null validation in constructors

**Visual Bugs:**
- Camera not rendering correctly
- Popup positioning wrong
- **Detection:** Manual QA, screenshot tests
- **Fix:** Adjust camera provider logic

**Performance:**
- ECS camera queries too frequent
- **Detection:** Performance profiling
- **Fix:** Adjust cache invalidation interval

---

## 9. Success Criteria

### Functional Requirements ✅
- [ ] Map popups display correctly with proper animation
- [ ] Scenes stack properly (gameplay → popup → console)
- [ ] Camera viewport applied correctly to popups
- [ ] Scene lifecycle (Initialize → LoadContent → Update → Draw → Dispose)
- [ ] No null reference exceptions

### Code Quality Requirements ✅
- [ ] No `Services.GetService()` calls in scenes
- [ ] All dependencies injected via constructor
- [ ] `SceneBase` does not expose `IServiceProvider`
- [ ] All scene constructors validate dependencies
- [ ] Nullable reference warnings resolved

### Testing Requirements ✅
- [ ] Unit tests for `MapPopupScene` (>80% coverage)
- [ ] Unit tests for `SceneFactory`
- [ ] Unit tests for `EcsCameraProvider`
- [ ] Integration tests for scene creation
- [ ] Manual QA of map popup display

### Documentation Requirements ✅
- [ ] Code comments explain DI patterns
- [ ] Architecture decision documented (this file)
- [ ] Migration guide for future scenes
- [ ] Testing examples provided

---

## 10. Future Improvements

### Scene Pooling
Once scenes use factories, add pooling for frequently-used scenes:

```csharp
public interface ISceneFactory
{
    MapPopupScene CreateMapPopupScene(...);
    void ReturnMapPopupScene(MapPopupScene scene);  // Pool return
}
```

### Scene Configuration
With explicit dependencies, scenes can accept configuration objects:

```csharp
public class MapPopupSceneConfig
{
    public float SlideInDuration { get; init; } = 0.4f;
    public float DisplayDuration { get; init; } = 2.5f;
    public float SlideOutDuration { get; init; } = 0.4f;
}
```

### Async Scene Loading
Factories can support async initialization:

```csharp
public interface ISceneFactory
{
    Task<MapPopupScene> CreateMapPopupSceneAsync(...);
}
```

### Scene Composition
With proper DI, scenes can be composed of smaller components:

```csharp
public class MapPopupScene : SceneBase
{
    private readonly IAnimationController _animation;
    private readonly IPopupRenderer _renderer;
    private readonly IPopupLayout _layout;

    // Components injected and testable independently
}
```

---

## 11. References

### Design Patterns Used
- **Dependency Injection** - Constructor injection for explicit dependencies
- **Factory Pattern** - `ISceneFactory` for centralized scene creation
- **Provider Pattern** - `ICameraProvider` for camera access abstraction
- **Service Locator (Removed)** - Anti-pattern being eliminated

### Related Documentation
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Service Locator Anti-Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [MonoGame Scene Management Best Practices](https://community.monogame.net/t/best-practices-for-scene-management/12345)

### Code Examples
- `GameplayScene` - Uses facade pattern for complex dependencies
- `MapPopupService` - Service that creates scenes
- `SceneManager` - Manages scene lifecycle

---

## 12. Conclusion

This refactor transforms the scene architecture from a **service locator anti-pattern** to **proper dependency injection**, delivering:

✅ **Explicit, compile-time validated dependencies**
✅ **100% testable scenes with mockable dependencies**
✅ **No runtime null checks for required dependencies**
✅ **Centralized scene creation through factories**
✅ **Clear separation of concerns**

The migration path is **incremental and low-risk**, allowing rollback at any phase. The final architecture will be more maintainable, testable, and align with modern .NET best practices.

**Estimated Effort:** 8-12 hours
- Phase 1: 2 hours
- Phase 2: 3 hours
- Phase 3: 4 hours
- Phase 4: 1 hour
- Testing & validation: 2 hours

**Recommended Start:** Phase 1 (infrastructure) - zero risk, immediate value
