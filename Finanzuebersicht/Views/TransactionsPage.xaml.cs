using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Views;

public partial class TransactionsPage : BaseContentPage
{
    public TransactionsPage(TransactionsViewModel viewModel, ILogger<TransactionsPage> logger)
    {
        InitializeComponent();

        if (viewModel == null)
        {
            logger?.LogError("TransactionsPage: injected TransactionsViewModel is null. DI may have failed.");
            BindingContext = new object();
        }
        else
        {
            BindingContext = viewModel;
            AttachDesktopToolbar(viewModel);
        }
    }

    void AttachDesktopToolbar(TransactionsViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        DesktopChrome.AddToolbarItems(
            this,
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadTransaktionenCommand, priority: 0),
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Import], viewModel.ImportCsvCommand, priority: 1),
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Umbuchen], viewModel.GoToTransferCommand, priority: 2),
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.GoToDetailCommand, priority: 3));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Widget inbox / saves — only Transactions needs live reload (not all BaseContentPage tabs).
        AppEvents.DataChanged += OnDataChanged;
    }

    protected override void OnDisappearing()
    {
        if (CachedAppEvents is not null)
            CachedAppEvents.DataChanged -= OnDataChanged;

        base.OnDisappearing();
    }

    private void OnDataChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (BindingContext is IAutoLoadViewModel vm && vm.ShouldAutoLoad)
                vm.AutoLoadCommand.Execute(null);
        });
    }
}
