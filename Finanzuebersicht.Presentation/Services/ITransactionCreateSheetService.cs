using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation.Services;

public interface ITransactionCreateSheetService
{
    Task<bool> ShowAsync(TransactionDetailViewModel viewModel);
}
