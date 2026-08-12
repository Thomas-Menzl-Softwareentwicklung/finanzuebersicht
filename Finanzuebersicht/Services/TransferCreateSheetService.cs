using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Services;

public sealed class TransferCreateSheetService(ICreateFormModalService createFormModalService)
    : ITransferCreateSheetService
{
    public Task<bool> ShowAsync(TransferDetailViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        return createFormModalService.ShowAsync(
            viewModel.PageTitle,
            () => new TransferFormView { BindingContext = viewModel },
            viewModel.TrySaveAsync,
            saveText: loc[ResourceKeys.Btn_Speichern]);
    }
}
