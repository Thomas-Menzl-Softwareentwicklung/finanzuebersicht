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
        DesktopChrome.AttachPageActions(this, () =>
        {
            var loc = LocalizationResourceManager.Current;
            return (
                loc[ResourceKeys.Menu_Aktionen],
                [
                    new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadSparZieleCommand, "R"),
                    new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.OpenCreateFormCommand, "N")
                ]);
        });
    }
}
