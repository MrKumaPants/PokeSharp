using Arch.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PokeSharp.Core.Systems;
using PokeSharp.Game.Tests.TestFixtures;
using Xunit;

namespace PokeSharp.Game.Tests.Systems;

/// <summary>
/// Tests for SystemManager to verify system registration, priority ordering, and execution.
/// </summary>
public class SystemManagerTests : IClassFixture<GameTestFixture>
{
    private readonly GameTestFixture _fixture;

    public SystemManagerTests(GameTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RegisterSystem_AddsSystemToManager()
    {
        // Arrange
        var manager = new SystemManager();
        var mockSystem = new Mock<ISystem>();
        mockSystem.Setup(x => x.Priority).Returns(100);

        // Act
        manager.RegisterSystem(mockSystem.Object);

        // Assert
        manager.SystemCount.Should().Be(1);
        manager.Systems.Should().Contain(mockSystem.Object);
    }

    [Fact]
    public void RegisterSystem_ThrowsOnDuplicateSystem()
    {
        // Arrange
        var manager = new SystemManager();
        var mockSystem = new Mock<ISystem>();
        mockSystem.Setup(x => x.Priority).Returns(100);
        manager.RegisterSystem(mockSystem.Object);

        // Act & Assert
        var action = () => manager.RegisterSystem(mockSystem.Object);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void RegisterSystem_ThrowsOnNullSystem()
    {
        // Arrange
        var manager = new SystemManager();

        // Act & Assert
        var action = () => manager.RegisterSystem(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterSystem_SortsByPriority()
    {
        // Arrange
        var manager = new SystemManager();
        var system1 = new Mock<ISystem>();
        var system2 = new Mock<ISystem>();
        var system3 = new Mock<ISystem>();

        system1.Setup(x => x.Priority).Returns(300);
        system2.Setup(x => x.Priority).Returns(100);
        system3.Setup(x => x.Priority).Returns(200);

        // Act - register in wrong order
        manager.RegisterSystem(system1.Object);
        manager.RegisterSystem(system2.Object);
        manager.RegisterSystem(system3.Object);

        // Assert - should be sorted by priority
        var systems = manager.Systems;
        systems[0].Should().Be(system2.Object, "system with priority 100 should be first");
        systems[1].Should().Be(system3.Object, "system with priority 200 should be second");
        systems[2].Should().Be(system1.Object, "system with priority 300 should be third");
    }

    [Fact]
    public void UnregisterSystem_RemovesSystem()
    {
        // Arrange
        var manager = new SystemManager();
        var mockSystem = new Mock<ISystem>();
        mockSystem.Setup(x => x.Priority).Returns(100);
        manager.RegisterSystem(mockSystem.Object);

        // Act
        manager.UnregisterSystem(mockSystem.Object);

        // Assert
        manager.SystemCount.Should().Be(0);
        manager.Systems.Should().NotContain(mockSystem.Object);
    }

    [Fact]
    public void Initialize_CallsInitializeOnAllSystems()
    {
        // Arrange
        using var world = World.Create();
        var manager = new SystemManager();
        var system1 = new Mock<ISystem>();
        var system2 = new Mock<ISystem>();

        system1.Setup(x => x.Priority).Returns(100);
        system2.Setup(x => x.Priority).Returns(200);

        manager.RegisterSystem(system1.Object);
        manager.RegisterSystem(system2.Object);

        // Act
        manager.Initialize(world);

        // Assert
        system1.Verify(x => x.Initialize(world), Times.Once);
        system2.Verify(x => x.Initialize(world), Times.Once);
    }

    [Fact]
    public void Initialize_ThrowsIfCalledTwice()
    {
        // Arrange
        using var world = World.Create();
        var manager = new SystemManager();
        manager.Initialize(world);

        // Act & Assert
        var action = () => manager.Initialize(world);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been initialized*");
    }

    [Fact]
    public void Update_ThrowsIfNotInitialized()
    {
        // Arrange
        using var world = World.Create();
        var manager = new SystemManager();

        // Act & Assert
        var action = () => manager.Update(world, 0.016f);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void Update_CallsUpdateOnEnabledSystems()
    {
        // Arrange
        using var world = World.Create();
        var manager = new SystemManager();
        var system1 = new Mock<ISystem>();
        var system2 = new Mock<ISystem>();

        system1.Setup(x => x.Priority).Returns(100);
        system1.Setup(x => x.Enabled).Returns(true);
        system2.Setup(x => x.Priority).Returns(200);
        system2.Setup(x => x.Enabled).Returns(true);

        manager.RegisterSystem(system1.Object);
        manager.RegisterSystem(system2.Object);
        manager.Initialize(world);

        // Act
        manager.Update(world, 0.016f);

        // Assert
        system1.Verify(x => x.Update(world, 0.016f), Times.Once);
        system2.Verify(x => x.Update(world, 0.016f), Times.Once);
    }

    [Fact]
    public void Update_SkipsDisabledSystems()
    {
        // Arrange
        using var world = World.Create();
        var manager = new SystemManager();
        var enabledSystem = new Mock<ISystem>();
        var disabledSystem = new Mock<ISystem>();

        enabledSystem.Setup(x => x.Priority).Returns(100);
        enabledSystem.Setup(x => x.Enabled).Returns(true);
        disabledSystem.Setup(x => x.Priority).Returns(200);
        disabledSystem.Setup(x => x.Enabled).Returns(false);

        manager.RegisterSystem(enabledSystem.Object);
        manager.RegisterSystem(disabledSystem.Object);
        manager.Initialize(world);

        // Act
        manager.Update(world, 0.016f);

        // Assert
        enabledSystem.Verify(x => x.Update(world, 0.016f), Times.Once);
        disabledSystem.Verify(x => x.Update(world, It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void Update_ContinuesAfterSystemException()
    {
        // Arrange
        using var world = World.Create();
        var mockLogger = new Mock<ILogger<SystemManager>>();
        var manager = new SystemManager(mockLogger.Object);

        var throwingSystem = new Mock<ISystem>();
        var normalSystem = new Mock<ISystem>();

        throwingSystem.Setup(x => x.Priority).Returns(100);
        throwingSystem.Setup(x => x.Enabled).Returns(true);
        throwingSystem.Setup(x => x.Update(It.IsAny<World>(), It.IsAny<float>()))
            .Throws(new InvalidOperationException("Test exception"));

        normalSystem.Setup(x => x.Priority).Returns(200);
        normalSystem.Setup(x => x.Enabled).Returns(true);

        manager.RegisterSystem(throwingSystem.Object);
        manager.RegisterSystem(normalSystem.Object);
        manager.Initialize(world);

        // Act
        manager.Update(world, 0.016f);

        // Assert - should continue to next system despite exception
        normalSystem.Verify(x => x.Update(world, 0.016f), Times.Once);
    }

    [Fact]
    public void GetMetrics_ReturnsPerformanceData()
    {
        // Arrange
        using var world = World.Create();
        var manager = new SystemManager();
        var mockSystem = new Mock<ISystem>();

        mockSystem.Setup(x => x.Priority).Returns(100);
        mockSystem.Setup(x => x.Enabled).Returns(true);

        manager.RegisterSystem(mockSystem.Object);
        manager.Initialize(world);
        manager.Update(world, 0.016f);

        // Act
        var metrics = manager.GetMetrics();

        // Assert
        metrics.Should().ContainKey(mockSystem.Object);
        var systemMetrics = metrics[mockSystem.Object];
        systemMetrics.UpdateCount.Should().Be(1);
        systemMetrics.TotalTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ResetMetrics_ClearsPerformanceData()
    {
        // Arrange
        using var world = World.Create();
        var manager = new SystemManager();
        var mockSystem = new Mock<ISystem>();

        mockSystem.Setup(x => x.Priority).Returns(100);
        mockSystem.Setup(x => x.Enabled).Returns(true);

        manager.RegisterSystem(mockSystem.Object);
        manager.Initialize(world);
        manager.Update(world, 0.016f);

        // Act
        manager.ResetMetrics();
        var metrics = manager.GetMetrics();

        // Assert
        var systemMetrics = metrics[mockSystem.Object];
        systemMetrics.UpdateCount.Should().Be(0);
        systemMetrics.TotalTimeMs.Should().Be(0);
        systemMetrics.LastUpdateMs.Should().Be(0);
        systemMetrics.MaxUpdateMs.Should().Be(0);
    }

    [Fact]
    public void SystemCount_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new SystemManager();

        // Act & Assert - empty
        manager.SystemCount.Should().Be(0);

        // Add systems
        var system1 = new Mock<ISystem>();
        system1.Setup(x => x.Priority).Returns(100);
        manager.RegisterSystem(system1.Object);
        manager.SystemCount.Should().Be(1);

        var system2 = new Mock<ISystem>();
        system2.Setup(x => x.Priority).Returns(200);
        manager.RegisterSystem(system2.Object);
        manager.SystemCount.Should().Be(2);
    }

    [Fact]
    public void Systems_ReturnsReadOnlyList()
    {
        // Arrange
        var manager = new SystemManager();
        var mockSystem = new Mock<ISystem>();
        mockSystem.Setup(x => x.Priority).Returns(100);
        manager.RegisterSystem(mockSystem.Object);

        // Act
        var systems = manager.Systems;

        // Assert
        systems.Should().BeAssignableTo<IReadOnlyList<ISystem>>();
        systems.Should().HaveCount(1);
    }
}
