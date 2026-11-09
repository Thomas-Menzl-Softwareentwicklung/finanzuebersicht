using Finanzuebersicht.Core.AppLock;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Presentation.Services.AppLock;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels.Settings;

public class AppLockViewModelTests
{
    [Fact]
    public async Task InitializeAsync_UsesFiveMinuteDefaultAndAvailableHost()
    {
        var viewModel = Create();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(300, viewModel.SelectedDelaySeconds);
        Assert.False(viewModel.IsEnabled);
        Assert.True(viewModel.IsAvailable);
        Assert.Equal(new[] { 0, 60, 300, 900 }, viewModel.DelayOptions.Select(x => x.Seconds));
    }

    [Fact]
    public async Task ApplyAsync_AuthenticationCancel_DoesNotEnableAppLock()
    {
        var store = new TestStore(AppLockOptions.Default);
        var viewModel = Create(store, AuthenticationResult.Cancelled);
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.IsEnabled = true;

        await viewModel.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(AppLockOptions.Default, await store.LoadAsync());
        Assert.False(viewModel.IsEnabled);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task ApplyAsync_UnavailableHost_LeavesFeatureDisabled()
    {
        var store = new TestStore(AppLockOptions.Default);
        var viewModel = Create(store, isAvailable: false);
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.IsEnabled = true;

        await viewModel.ApplyCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsEnabled);
        Assert.Equal(0, store.Saves);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.StatusText));
    }

    private static AppLockViewModel Create(TestStore? store = null, AuthenticationResult result = AuthenticationResult.Succeeded, bool isAvailable = true)
    {
        store ??= new TestStore(AppLockOptions.Default);
        var authentication = new TestAuthentication(result, isAvailable);
        var coordinator = new AppLockCoordinator(store, authentication, TimeProvider.System);
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        return new AppLockViewModel(coordinator, store, authentication, localization);
    }

    private sealed class TestStore(AppLockOptions options) : IAppLockSettingsStore
    {
        public int Saves { get; private set; }
        public Task<AppLockOptions> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(options);
        public Task SaveAsync(AppLockOptions value, CancellationToken cancellationToken = default)
        {
            Saves++;
            options = value;
            return Task.CompletedTask;
        }
    }

    private sealed class TestAuthentication(AuthenticationResult result, bool available) : IDeviceAuthenticationService
    {
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(available);
        public Task<AuthenticationResult> AuthenticateAsync(string reason, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
