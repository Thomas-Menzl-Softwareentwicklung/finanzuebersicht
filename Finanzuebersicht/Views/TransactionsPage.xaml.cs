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
    private int _phoneActionRows = 1;
    private bool _updatingPhoneActions;

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
        DesktopChrome.AttachPageActions(this, () =>
        {
            var loc = LocalizationResourceManager.Current;
            return (
                loc[ResourceKeys.Menu_Aktionen],
                [
                    new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadTransaktionenCommand, "R"),
                    new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Import], viewModel.ImportCsvCommand, "I"),
                    new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Umbuchen], viewModel.GoToTransferCommand, "U"),
                    new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.GoToDetailCommand, "N")
                ]);
        });
    }

    private void OnPhoneActionSizeChanged(object? sender, EventArgs e) => UpdatePhoneActionLayout();

    private void OnToggleSearchClicked(object? sender, EventArgs e)
    {
        if (TransactionSearchContainer.IsVisible)
        {
            TransactionSearchBar.Unfocus();
            TransactionSearchContainer.IsVisible = false;
        }
        else
        {
            TransactionSearchContainer.IsVisible = true;
            Dispatcher.Dispatch(() => TransactionSearchBar.Focus());
        }
    }

    private void OnTransactionSearchSubmitted(object? sender, EventArgs e)
    {
        TransactionSearchBar.Unfocus();
        if (DesktopChrome.IsPhone)
            TransactionSearchContainer.IsVisible = false;
    }

    private void UpdatePhoneActionLayout()
    {
        if (!DesktopChrome.IsPhone || _updatingPhoneActions || PhoneActionsGrid.Width <= 0)
            return;

        _updatingPhoneActions = true;
        try
        {
            // Measure the native, scaled text before choosing a row arrangement.
            var importWidth = ((IView)ImportActionButton).Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
            var transferWidth = ((IView)TransferActionButton).Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
            var addWidth = ((IView)AddActionButton).Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
            var width = PhoneActionsGrid.Width;
            var spacing = PhoneActionsGrid.ColumnSpacing;
            var rows = importWidth + transferWidth + addWidth + 2 * spacing <= width
                ? 1
                : 2 * Math.Max(importWidth, transferWidth) + spacing <= width ? 2 : 3;

            if (_phoneActionRows == rows)
                return;

            _phoneActionRows = rows;
            PhoneActionsGrid.RowDefinitions.Clear();
            for (var row = 0; row < rows; row++)
                PhoneActionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            PhoneActionsGrid.ColumnDefinitions.Clear();
            var columns = rows == 1 ? 3 : rows == 2 ? 2 : 1;
            for (var column = 0; column < columns; column++)
            {
                PhoneActionsGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = rows == 1 && column < 2 ? GridLength.Auto : GridLength.Star
                });
            }

            Grid.SetRow(TransferActionButton, rows == 3 ? 1 : 0);
            Grid.SetColumn(TransferActionButton, rows == 3 ? 0 : 1);
            Grid.SetRow(AddActionButton, rows - 1);
            Grid.SetColumn(AddActionButton, rows == 1 ? 2 : 0);
            Grid.SetColumnSpan(AddActionButton, rows == 2 ? 2 : 1);
        }
        finally
        {
            _updatingPhoneActions = false;
        }
    }

    /// <summary>Used by ⌘F / Menü „Suchen“ on Mac Catalyst.</summary>
    public void FocusSearchBar()
    {
        TransactionSearchContainer.IsVisible = true;
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
        Dispatcher.Dispatch(UpdatePhoneActionLayout);
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
