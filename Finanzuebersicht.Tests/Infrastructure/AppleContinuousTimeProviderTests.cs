using System.Runtime.InteropServices;
using Finanzuebersicht.Core.AppLock;
using Finanzuebersicht.Infrastructure.Services;
using Finanzuebersicht.Presentation.Services.AppLock;

namespace Finanzuebersicht.Tests.Infrastructure;

public sealed class AppleContinuousTimeProviderTests
{
    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(60, 59, false)]
    [InlineData(60, 60, true)]
    [InlineData(300, 299, false)]
    [InlineData(300, 300, true)]
    [InlineData(900, 899, false)]
    [InlineData(900, 900, true)]
    [InlineData(300, 28_800, true)]
    public async Task ResumeAfterDeviceSleep_EnforcesConfiguredBackgroundDeadline(int delay, int elapsedSeconds, bool shouldLock)
    {
        ulong awakeNanoseconds = 0;
        ulong asleepNanoseconds = 0;
        // Native boundary double: Darwin clock 4 includes sleep, clock 8 omits it.
        var clock = new AppleContinuousTimeProvider(clockId => clockId switch
        {
            4 => awakeNanoseconds + asleepNanoseconds,
            8 => awakeNanoseconds,
            _ => throw new ArgumentOutOfRangeException(nameof(clockId))
        });
        var store = Substitute.For<IAppLockSettingsStore>();
        store.LoadAsync(Arg.Any<CancellationToken>()).Returns(new AppLockOptions(true, delay));
        var authentication = Substitute.For<IDeviceAuthenticationService>();
        authentication.IsSupported.Returns(true);
        authentication.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
        authentication.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AuthenticationResult.Succeeded);
        var coordinator = new AppLockCoordinator(store, authentication, clock);
        await coordinator.InitializeAsync();
        await coordinator.UnlockAsync("Unlock");
        authentication.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(AuthenticationResult.Cancelled);
        coordinator.EnterBackground();

        awakeNanoseconds += 1_000_000_000;
        asleepNanoseconds += (ulong)(elapsedSeconds - 1) * 1_000_000_000;
        await coordinator.ActivateAsync("Unlock");

        Assert.Equal(shouldLock ? AppLockState.Locked : AppLockState.Unlocked, coordinator.State);
        Assert.Equal(!shouldLock, coordinator.CanAccessContent);
    }

    [Fact]
    public void NativeTimestampOnMac_IsInContinuousClockNanoseconds()
    {
        if (!OperatingSystem.IsMacOSVersionAtLeast(10, 12))
            return;

        var before = ContinuousNanoseconds(4);
        var timestamp = new AppleContinuousTimeProvider().GetTimestamp();
        var after = ContinuousNanoseconds(4);

        Assert.InRange(timestamp, checked((long)before), checked((long)after));
    }

    [DllImport("/usr/lib/libSystem.dylib", EntryPoint = "clock_gettime_nsec_np")]
    private static extern ulong ContinuousNanoseconds(int clockId);
}
