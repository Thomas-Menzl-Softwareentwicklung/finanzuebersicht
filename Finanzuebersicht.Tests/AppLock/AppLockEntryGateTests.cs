using Finanzuebersicht.Core.AppLock;
using Finanzuebersicht.Presentation.Services.AppLock;

namespace Finanzuebersicht.Tests.AppLock;

public class AppLockEntryGateTests
{
    [Fact]
    public async Task RunIfAllowedAsync_WhenLocked_DoesNotRunAction()
    {
        var coordinator = Substitute.For<IAppLockCoordinator>();
        coordinator.CanAccessContent.Returns(false);
        var called = false;
        var gate = new AppLockEntryGate(coordinator);

        var ran = await gate.RunIfAllowedAsync(() =>
        {
            called = true;
            return Task.CompletedTask;
        });

        Assert.False(ran);
        Assert.False(called);
    }

    [Fact]
    public async Task RunIfAllowedAsync_WhenUnlocked_RunsActionOnce()
    {
        var coordinator = Substitute.For<IAppLockCoordinator>();
        coordinator.CanAccessContent.Returns(true);
        var calls = 0;
        var gate = new AppLockEntryGate(coordinator);

        var ran = await gate.RunIfAllowedAsync(() =>
        {
            calls++;
            return Task.CompletedTask;
        });

        Assert.True(ran);
        Assert.Equal(1, calls);
    }
}
