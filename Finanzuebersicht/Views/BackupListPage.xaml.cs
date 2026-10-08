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
        DesktopChrome.AttachPageActions(this, () =>
        {
            var loc = LocalizationResourceManager.Current;
            return (
                loc[ResourceKeys.Menu_Aktionen],
                [
                    new DesktopChrome.DesktopAction(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadBackupsCommand, "R")
                ]);
        });
    }
}