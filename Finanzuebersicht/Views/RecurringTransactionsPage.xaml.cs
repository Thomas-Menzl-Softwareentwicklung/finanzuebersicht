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
        DesktopChrome.AddToolbarItems(
            this,
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadDauerauftraegeCommand, priority: 0),
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.GoToDetailCommand, priority: 1));
    }
}
