using Finanzuebersicht.ViewModels;
#if IOS
using ObjCRuntime;
using UIKit;
#endif

namespace Finanzuebersicht.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsViewModel vm)
        {
            await vm.License.InitializeAsync();
            await vm.WidgetPresets.LoadAsync();
        }
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
