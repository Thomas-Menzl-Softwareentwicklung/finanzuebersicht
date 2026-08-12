using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Services;

public sealed class AccountCreateSheetService(ICreateFormModalService createFormModalService) : IAccountCreateSheetService
{
    public Task<bool> ShowAsync(AccountDetailViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        return createFormModalService.ShowAsync(
            viewModel.PageTitle,
            () => new AccountFormView { BindingContext = viewModel },
            viewModel.TrySaveAsync,
            saveText: loc[ResourceKeys.Btn_Speichern]);
    }
}
