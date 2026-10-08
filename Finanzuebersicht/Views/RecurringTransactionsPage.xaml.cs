using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class RecurringTransactionsPage : BaseContentPage
{
    public RecurringTransactionsPage(RecurringTransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        AttachDesktopToolbar(viewModel);
    }

    void AttachDesktopToolbar(RecurringTransactionsViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        DesktopChrome.AttachPageActions(
            this,
            loc[ResourceKeys.Menu_Aktionen],
            new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadDauerauftraegeCommand, "R"),
            new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.GoToDetailCommand, "N"));
    }
}
