using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;

namespace Finanzuebersicht.ViewModels;

public partial class SettingsViewModel(
    AppearanceViewModel appearance,
    StorageViewModel storage,
    BackupViewModel backup,
    AboutViewModel about,
    LicenseViewModel license,
    QuickExpenseWidgetPresetsViewModel widgetPresets,
    INavigationService navigationService) : ObservableObject
{
    public AppearanceViewModel Appearance { get; } = appearance;
    public StorageViewModel Storage { get; } = storage;
    public BackupViewModel Backup { get; } = backup;
    public AboutViewModel About { get; } = about;
    public LicenseViewModel License { get; } = license;
    public QuickExpenseWidgetPresetsViewModel WidgetPresets { get; } = widgetPresets;

    [RelayCommand]
    private Task ShowOnboardingAgain()
        => navigationService.GoToAsync(Routes.Onboarding);
}
