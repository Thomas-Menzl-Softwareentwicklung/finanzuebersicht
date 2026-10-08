using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class BackupListPage : BaseContentPage
{
    public BackupListPage(BackupListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        AttachDesktopToolbar(viewModel);
    }

    void AttachDesktopToolbar(BackupListViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        DesktopChrome.AttachPageActions(
            this,
            loc[ResourceKeys.Menu_Aktionen],
            new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadBackupsCommand, "R"));
    }
}