using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class SparZielePage : BaseContentPage
{
    public SparZielePage(SparZieleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        AttachDesktopToolbar(viewModel);
    }

    void AttachDesktopToolbar(SparZieleViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        DesktopChrome.AddToolbarItems(
            this,
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadSparZieleCommand, priority: 0),
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.OpenCreateFormCommand, priority: 1));
    }
}
