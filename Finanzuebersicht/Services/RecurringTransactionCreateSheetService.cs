using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Services;

public sealed class RecurringTransactionCreateSheetService(ICreateFormModalService createFormModalService)
    : IRecurringTransactionCreateSheetService
{
    public Task<bool> ShowAsync(RecurringTransactionDetailViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        return createFormModalService.ShowAsync(
            viewModel.PageTitle,
            () => new RecurringTransactionFormView { BindingContext = viewModel },
            viewModel.TrySaveAsync,
            saveText: loc[ResourceKeys.Btn_Speichern]);
    }
}
