using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation.Services;

public interface ITransferCreateSheetService
{
    Task<bool> ShowAsync(TransferDetailViewModel viewModel);
}
