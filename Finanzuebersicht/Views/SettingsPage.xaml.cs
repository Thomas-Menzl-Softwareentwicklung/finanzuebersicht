using Finanzuebersicht.ViewModels;

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
}
