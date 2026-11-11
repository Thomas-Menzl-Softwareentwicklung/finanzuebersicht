using Finanzuebersicht.Core.AppLock;
using Finanzuebersicht.Presentation.Services.AppLock;

namespace Finanzuebersicht.Tests.AppLock;

public sealed class AppLockSceneRegistryTests
{
    [Fact]
    public async Task BackgroundsOnlyWhenEverySceneHasEnteredBackground()
    {
        var coordinator = Substitute.For<IAppLockCoordinator>();
        var registry = new AppLockSceneRegistry(coordinator);
        var first = new object();
        var second = new object();

        await registry.RegisterAsync(first, isInForeground: true, "Unlock");
        await registry.RegisterAsync(second, isInForeground: true, "Unlock");
        await registry.SetInForegroundAsync(first, isInForeground: false, "Unlock");

        coordinator.DidNotReceive().EnterBackground();

        await registry.SetInForegroundAsync(second, isInForeground: false, "Unlock");

        coordinator.Received(1).EnterBackground();
    }

    [Fact]
    public async Task ActivatesOnceWhenFirstSceneReturnsFromBackground()
    {
        var coordinator = Substitute.For<IAppLockCoordinator>();
        var registry = new AppLockSceneRegistry(coordinator);
        var first = new object();
        var second = new object();

        await registry.RegisterAsync(first, isInForeground: true, "Unlock");
        await registry.RegisterAsync(second, isInForeground: true, "Unlock");
        await registry.SetInForegroundAsync(first, isInForeground: false, "Unlock");
        await registry.SetInForegroundAsync(second, isInForeground: false, "Unlock");
        await registry.SetInForegroundAsync(first, isInForeground: true, "Unlock");
        await registry.SetInForegroundAsync(second, isInForeground: true, "Unlock");

        await coordinator.Received(1).ActivateAsync("Unlock", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NewForegroundSceneActivatesSessionAfterAllScenesWereBackgrounded()
    {
        var coordinator = Substitute.For<IAppLockCoordinator>();
        var registry = new AppLockSceneRegistry(coordinator);
        var first = new object();
        var next = new object();

        await registry.RegisterAsync(first, isInForeground: true, "Unlock");
        await registry.SetInForegroundAsync(first, isInForeground: false, "Unlock");
        await registry.RegisterAsync(next, isInForeground: true, "Unlock");

        await coordinator.Received(1).ActivateAsync("Unlock", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitialForegroundSceneDoesNotPretendToBeAResume()
    {
        var coordinator = Substitute.For<IAppLockCoordinator>();
        var registry = new AppLockSceneRegistry(coordinator);

        await registry.RegisterAsync(new object(), isInForeground: true, "Unlock");

        await coordinator.DidNotReceive().ActivateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        coordinator.DidNotReceive().EnterBackground();
    }

    [Fact]
    public async Task NewForegroundSceneAfterPriorSceneWasClosedStillChecksElapsedTimeout()
    {
        var coordinator = Substitute.For<IAppLockCoordinator>();
        var registry = new AppLockSceneRegistry(coordinator);
        var oldScene = new object();
        var newScene = new object();

        await registry.RegisterAsync(oldScene, isInForeground: true, "Unlock");
        await registry.SetInForegroundAsync(oldScene, isInForeground: false, "Unlock");
        registry.Unregister(oldScene);
        await registry.RegisterAsync(newScene, isInForeground: true, "Unlock");

        await coordinator.Received(1).ActivateAsync("Unlock", Arg.Any<CancellationToken>());
    }
}
