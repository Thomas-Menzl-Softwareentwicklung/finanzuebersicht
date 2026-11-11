using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Core.AppLock;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Presentation.Services.AppLock;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public sealed record AppLockDelayOption(int Seconds, string Name);

public partial class AppLockViewModel : ObservableObject, ILocalizableViewModel
{
    private readonly IAppLockCoordinator _coordinator;
    private readonly IAppLockSettingsStore _settings;
    private readonly IDeviceAuthenticationService _authentication;
    private readonly ILocalizationService _localization;
    private AppLockOptions _currentOptions = AppLockOptions.Default;
    private string? _statusResourceKey;

    [ObservableProperty] private bool isEnabled;
    [ObservableProperty] private int selectedDelaySeconds = 300;
    [ObservableProperty] private AppLockDelayOption? selectedDelayOption;
    [ObservableProperty] private bool isAvailable;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusText = string.Empty;

    public IReadOnlyList<AppLockDelayOption> DelayOptions { get; private set; } = [];

    partial void OnSelectedDelaySecondsChanged(int value)
        => SelectedDelayOption = DelayOptions.FirstOrDefault(x => x.Seconds == value);

    partial void OnSelectedDelayOptionChanged(AppLockDelayOption? value)
    {
        if (value is not null)
            SelectedDelaySeconds = value.Seconds;
    }

    public AppLockViewModel(
        IAppLockCoordinator coordinator,
        IAppLockSettingsStore settings,
        IDeviceAuthenticationService authentication,
        ILocalizationService localization)
    {
        _coordinator = coordinator;
        _settings = settings;
        _authentication = authentication;
        _localization = localization;
        RefreshLocalizedStrings();
    }

    public void RefreshLocalizedStrings()
    {
        DelayOptions =
        [
            new(0, _localization.GetString(ResourceKeys.AppLock_Immediately)),
            new(60, _localization.GetString(ResourceKeys.AppLock_OneMinute)),
            new(300, _localization.GetString(ResourceKeys.AppLock_FiveMinutes)),
            new(900, _localization.GetString(ResourceKeys.AppLock_FifteenMinutes))
        ];
        OnPropertyChanged(nameof(DelayOptions));
        SelectedDelayOption = DelayOptions.FirstOrDefault(x => x.Seconds == SelectedDelaySeconds);
        if (_statusResourceKey is not null)
            StatusText = _localization.GetString(_statusResourceKey);
    }

    [RelayCommand]
    private async Task Load()
    {
        IsBusy = true;
        ClearStatus();
        try
        {
            await _coordinator.InitializeAsync();
            _currentOptions = await _settings.LoadAsync();
            _currentOptions = Normalize(_currentOptions);
            IsEnabled = _currentOptions.Enabled;
            SelectedDelaySeconds = _currentOptions.DelaySeconds;
            IsAvailable = await _authentication.IsAvailableAsync();
            if (!IsAvailable)
                SetStatus(ResourceKeys.AppLock_AuthenticationUnavailable);
        }
        catch
        {
            IsAvailable = false;
            IsEnabled = _currentOptions.Enabled;
            SetStatus(ResourceKeys.AppLock_SettingsLoadError);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Apply()
    {
        if (IsBusy)
            return;
        if (IsEnabled && !IsAvailable)
        {
            IsEnabled = _currentOptions.Enabled;
            SetStatus(ResourceKeys.AppLock_AuthenticationUnavailable);
            return;
        }

        IsBusy = true;
        ClearStatus();
        try
        {
            var next = new AppLockOptions(IsEnabled, SelectedDelaySeconds);
            if (!await _coordinator.ChangeOptionsAsync(next, _localization.GetString(ResourceKeys.AppLock_UnlockReason)))
            {
                IsEnabled = _currentOptions.Enabled;
                SelectedDelaySeconds = _currentOptions.DelaySeconds;
                SetStatus(ResourceKeys.AppLock_ChangeCancelled);
                return;
            }
            _currentOptions = next;
            SetStatus(ResourceKeys.AppLock_SettingsSaved);
        }
        catch (Exception)
        {
            IsEnabled = _currentOptions.Enabled;
            SelectedDelaySeconds = _currentOptions.DelaySeconds;
            SetStatus(ResourceKeys.AppLock_SaveError);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static AppLockOptions Normalize(AppLockOptions value)
        => AppLockOptions.AllowedDelaysSeconds.Contains(value.DelaySeconds)
            ? value
            : value with { DelaySeconds = 0 };

    private void SetStatus(string resourceKey)
    {
        _statusResourceKey = resourceKey;
        StatusText = _localization.GetString(resourceKey);
    }

    private void ClearStatus()
    {
        _statusResourceKey = null;
        StatusText = string.Empty;
    }
}
