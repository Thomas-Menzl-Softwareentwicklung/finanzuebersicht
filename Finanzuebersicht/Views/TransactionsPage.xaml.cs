using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.Logging;
#if MACCATALYST
using UIKit;
#endif

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
        DesktopChrome.AttachPageActions(
            this,
            loc[ResourceKeys.Menu_Aktionen],
            new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadTransaktionenCommand, "R"),
            new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Import], viewModel.ImportCsvCommand, "I"),
            new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Umbuchen], viewModel.GoToTransferCommand, "U"),
            new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.GoToDetailCommand, "N"));
    }

    /// <summary>Used by ⌘F / Menü „Suchen“ on Mac Catalyst.</summary>
    public void FocusSearchBar()
    {
        TransactionSearchBar.Focus();

#if MACCATALYST
        // MAUI SearchBar.Focus() often no-ops on Catalyst — activate the native field.
        if (TransactionSearchBar.Handler?.PlatformView is UIView platform)
            FindFirstResponderTarget(platform)?.BecomeFirstResponder();
#endif
    }

#if MACCATALYST
    static UIView? FindFirstResponderTarget(UIView root)
    {
        if (root is UITextField or UISearchBar or UITextView)
            return root;

        foreach (var child in root.Subviews)
        {
            var found = FindFirstResponderTarget(child);
            if (found is not null)
                return found;
        }

        return null;
    }
#endif

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
