using Finanzuebersicht.Core.AppLock;
using Finanzuebersicht.Presentation.Services.AppLock;

namespace Finanzuebersicht.Tests.AppLock;

public class AppLockCoordinatorTests
{
    [Fact]
    public async Task InitializeAsync_DisabledByDefault_AllowsAccess()
    {
        var coordinator = CreateCoordinator(new AppLockOptions(false, 300));

        await coordinator.InitializeAsync();

        Assert.Equal(AppLockState.Disabled, coordinator.State);
        Assert.True(coordinator.CanAccessContent);
    }

    [Fact]
    public async Task InitializeAsync_Enabled_RequiresUnlockOnEveryProcessStart()
    {
        var settings = new TestSettingsStore(new AppLockOptions(true, 300));
        var coordinator = CreateCoordinator(settings);

        await coordinator.InitializeAsync();

        Assert.Equal(AppLockState.Locked, coordinator.State);
        Assert.False(coordinator.CanAccessContent);
        Assert.Equal(0, settings.SaveCount);
    }

    [Fact]
    public async Task InitializeAsync_InvalidStoredDelay_UsesImmediateLock()
    {
        var authenticator = new TestAuthenticator();
        var coordinator = CreateCoordinator(new AppLockOptions(true, 42), authenticator);
        await coordinator.InitializeAsync();
        await coordinator.UnlockAsync("Unlock");
        coordinator.EnterBackground();
        authenticator.Result = AuthenticationResult.Cancelled;

        await coordinator.ActivateAsync("Unlock");

        Assert.Equal(AppLockState.Locked, coordinator.State);
        Assert.Equal(2, authenticator.CallCount);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(60, 59, false)]
    [InlineData(60, 60, true)]
    [InlineData(300, 299, false)]
    [InlineData(300, 300, true)]
    [InlineData(900, 899, false)]
    [InlineData(900, 900, true)]
    public async Task ActivateAsync_UsesExactBackgroundDuration(int delaySeconds, int elapsedSeconds, bool shouldLock)
    {
        var clock = new FakeTimeProvider();
        var authenticator = new TestAuthenticator();
        var coordinator = CreateCoordinator(new AppLockOptions(true, delaySeconds), authenticator, clock);
        await coordinator.InitializeAsync();
        await coordinator.UnlockAsync("Unlock");
        coordinator.EnterBackground();
        clock.Advance(TimeSpan.FromSeconds(elapsedSeconds));
        if (shouldLock)
            authenticator.Result = AuthenticationResult.Cancelled;

        await coordinator.ActivateAsync("Unlock");

        Assert.Equal(shouldLock ? AppLockState.Locked : AppLockState.Unlocked, coordinator.State);
        Assert.Equal(!shouldLock, coordinator.CanAccessContent);
        Assert.Equal(shouldLock ? 2 : 1, authenticator.CallCount);
    }

    [Fact]
    public async Task ActivateAsync_WithoutBackground_DoesNotStartTimeout()
    {
        var coordinator = CreateCoordinator(new AppLockOptions(true, 0));
        await coordinator.InitializeAsync();
        await coordinator.UnlockAsync("Unlock");

        await coordinator.ActivateAsync("Unlock");

        Assert.Equal(AppLockState.Unlocked, coordinator.State);
        Assert.Equal(1, coordinator.Authenticator.CallCount);
    }

    [Fact]
    public async Task UnlockAsync_ConcurrentCalls_ShowOnlyOneAuthenticationPrompt()
    {
        var completion = new TaskCompletionSource<AuthenticationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authenticator = new TestAuthenticator { ResultTask = completion.Task, ResultSource = completion };
        var coordinator = CreateCoordinator(new AppLockOptions(true, 300), authenticator);
        await coordinator.InitializeAsync();

        var first = coordinator.UnlockAsync("Unlock");
        var second = coordinator.UnlockAsync("Unlock");
        completion.SetResult(AuthenticationResult.Succeeded);
        await Task.WhenAll(first, second);

        Assert.Equal(1, authenticator.CallCount);
        Assert.Equal(AppLockState.Unlocked, coordinator.State);
    }

    [Theory]
    [InlineData(AuthenticationResult.Cancelled)]
    [InlineData(AuthenticationResult.Unavailable)]
    [InlineData(AuthenticationResult.Failed)]
    public async Task UnlockAsync_NonSuccess_StaysLocked(AuthenticationResult result)
    {
        var coordinator = CreateCoordinator(new AppLockOptions(true, 300), new TestAuthenticator { Result = result });
        await coordinator.InitializeAsync();

        await coordinator.UnlockAsync("Unlock");

        Assert.Equal(AppLockState.Locked, coordinator.State);
        Assert.False(coordinator.CanAccessContent);
    }

    [Fact]
    public async Task EnterBackground_InvalidatesAuthenticationThatFinishesLate()
    {
        var completion = new TaskCompletionSource<AuthenticationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authenticator = new TestAuthenticator { ResultTask = completion.Task, ResultSource = completion };
        var coordinator = CreateCoordinator(new AppLockOptions(true, 300), authenticator);
        await coordinator.InitializeAsync();
        var unlock = coordinator.UnlockAsync("Unlock");

        coordinator.EnterBackground();
        completion.SetResult(AuthenticationResult.Succeeded);
        await unlock;

        Assert.Equal(AppLockState.Locked, coordinator.State);
        Assert.False(coordinator.CanAccessContent);
    }

    [Fact]
    public async Task InitializeAsync_SettingsFailure_FailsClosed()
    {
        var coordinator = CreateCoordinator(new TestSettingsStore { LoadException = new IOException("broken") });

        await Assert.ThrowsAsync<IOException>(() => coordinator.InitializeAsync());

        Assert.False(coordinator.CanAccessContent);
        Assert.Equal(AppLockState.Locked, coordinator.State);
    }

    [Fact]
    public async Task ChangeOptionsAsync_Enabling_RequiresAuthentication()
    {
        var settings = new TestSettingsStore(AppLockOptions.Default);
        var authenticator = new TestAuthenticator { Result = AuthenticationResult.Cancelled };
        var coordinator = CreateCoordinator(settings, authenticator);
        await coordinator.InitializeAsync();

        var changed = await coordinator.ChangeOptionsAsync(new AppLockOptions(true, 300), "Enable lock");

        Assert.False(changed);
        Assert.Equal(0, settings.SaveCount);
        Assert.Equal(AppLockState.Disabled, coordinator.State);
        Assert.True(coordinator.CanAccessContent);
    }

    [Fact]
    public async Task ChangeOptionsAsync_ShorterDelay_DoesNotRequireAuthentication()
    {
        var settings = new TestSettingsStore(new AppLockOptions(true, 900));
        var authenticator = new TestAuthenticator { Result = AuthenticationResult.Cancelled };
        var coordinator = CreateCoordinator(settings, authenticator);
        await coordinator.InitializeAsync();
        await coordinator.UnlockAsync("Unlock");

        var changed = await coordinator.ChangeOptionsAsync(new AppLockOptions(true, 300), "Shorter delay");

        Assert.True(changed);
        Assert.Equal(1, authenticator.CallCount);
        Assert.Equal(AppLockState.Unlocked, coordinator.State);
    }

    [Fact]
    public async Task ChangeOptionsAsync_SaveFailure_LeavesCurrentPolicyUntouched()
    {
        var settings = new TestSettingsStore(new AppLockOptions(true, 300)) { SaveException = new IOException("read-only") };
        var coordinator = CreateCoordinator(settings);
        await coordinator.InitializeAsync();
        await coordinator.UnlockAsync("Unlock");

        await Assert.ThrowsAsync<IOException>(() => coordinator.ChangeOptionsAsync(new AppLockOptions(false, 300), "Disable lock"));

        Assert.Equal(AppLockState.Unlocked, coordinator.State);
        Assert.True(coordinator.CanAccessContent);
    }

    [Fact]
    public async Task ActivateAsync_UsesMonotonicTimeWhenWallClockChanges()
    {
        var clock = new FakeTimeProvider();
        var coordinator = CreateCoordinator(new AppLockOptions(true, 300), clock: clock);
        await coordinator.InitializeAsync();
        await coordinator.UnlockAsync("Unlock");
        coordinator.EnterBackground();
        clock.SetUtcNow(DateTimeOffset.UtcNow.AddDays(30));

        await coordinator.ActivateAsync("Unlock");

        Assert.Equal(AppLockState.Unlocked, coordinator.State);
    }

    private static TestCoordinator CreateCoordinator(AppLockOptions options, TestAuthenticator? authenticator = null, FakeTimeProvider? clock = null)
        => CreateCoordinator(new TestSettingsStore(options), authenticator, clock);

    private static TestCoordinator CreateCoordinator(TestSettingsStore settings, TestAuthenticator? authenticator = null, FakeTimeProvider? clock = null)
        => new(settings, authenticator ?? new TestAuthenticator(), clock ?? new FakeTimeProvider());

    private sealed class TestCoordinator(TestSettingsStore settings, TestAuthenticator authenticator, FakeTimeProvider clock)
        : AppLockCoordinator(settings, authenticator, clock)
    {
        public TestAuthenticator Authenticator { get; } = authenticator;
    }

    private sealed class TestSettingsStore(AppLockOptions? options = null) : IAppLockSettingsStore
    {
        private AppLockOptions _options = options ?? AppLockOptions.Default;
        public Exception? LoadException { get; init; }
        public Exception? SaveException { get; init; }
        public int SaveCount { get; private set; }
        public Task<AppLockOptions> LoadAsync(CancellationToken cancellationToken = default)
            => LoadException is null ? Task.FromResult(_options) : Task.FromException<AppLockOptions>(LoadException);
        public Task SaveAsync(AppLockOptions value, CancellationToken cancellationToken = default)
        {
            if (SaveException is not null)
                return Task.FromException(SaveException);
            SaveCount++;
            _options = value;
            return Task.CompletedTask;
        }
    }

    private sealed class TestAuthenticator : IDeviceAuthenticationService
    {
        public AuthenticationResult Result { get; set; } = AuthenticationResult.Succeeded;
        public Task<AuthenticationResult>? ResultTask { get; init; }
        public TaskCompletionSource<AuthenticationResult> ResultSource { get; init; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CallCount { get; private set; }
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<AuthenticationResult> AuthenticateAsync(string reason, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ResultTask ?? Task.FromResult(Result);
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private long _timestamp;
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
        public override long GetTimestamp() => _timestamp;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public void Advance(TimeSpan duration)
        {
            _timestamp += duration.Ticks;
            _utcNow += duration;
        }
    }
}
