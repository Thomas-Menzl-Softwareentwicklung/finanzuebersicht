using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class CashflowPage : BaseContentPage
{
    public CashflowPage(CashflowViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        AttachDesktopToolbar(viewModel);
    }

    void AttachDesktopToolbar(CashflowViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        DesktopChrome.AddToolbarItems(
            this,
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadCashflowCommand, priority: 0));
    }
}
