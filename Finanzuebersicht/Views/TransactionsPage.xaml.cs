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
        }
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
