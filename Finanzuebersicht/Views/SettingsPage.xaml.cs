using Finanzuebersicht.Helpers;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.DependencyInjection;
#if IOS
using ObjCRuntime;
using UIKit;
#endif

namespace Finanzuebersicht.Views;

public partial class SettingsPage : ContentPage
{
    private IAppEvents? _appEvents;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _appEvents ??= Handler?.MauiContext?.Services.GetService<IAppEvents>()
            ?? Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<IAppEvents>();
        if (_appEvents is not null)
            _appEvents.LanguageChanged += OnLanguageChanged;
        // Clear stale tab Aktionen (Settings has none); Shell.Navigated may not always cover this path.
        DesktopChrome.PublishPageActions(this);
        if (BindingContext is SettingsViewModel vm)
        {
            vm.AppLock.RefreshLocalizedStrings();
            await vm.AppLock.LoadCommand.ExecuteAsync(null);
            await vm.License.InitializeAsync();
            await vm.WidgetPresets.LoadAsync();
        }
    }

    protected override void OnDisappearing()
    {
        if (_appEvents is not null)
            _appEvents.LanguageChanged -= OnLanguageChanged;
        base.OnDisappearing();
    }

    private void OnLanguageChanged()
    {
        if (BindingContext is SettingsViewModel vm)
            vm.AppLock.RefreshLocalizedStrings();
    }

    /// <summary>
    /// Resign first responder so Entry TwoWay bindings flush before Save snapshots slot values.
    /// </summary>
    private async void OnWidgetPresetsSaveClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not SettingsViewModel vm)
            return;

#if IOS
        UIApplication.SharedApplication.SendAction(
            new Selector("resignFirstResponder"),
            null,
            null,
            null);
        await Task.Delay(50);
#endif

        if (vm.WidgetPresets.SaveCommand.CanExecute(null))
            await vm.WidgetPresets.SaveCommand.ExecuteAsync(null);
    }
}
