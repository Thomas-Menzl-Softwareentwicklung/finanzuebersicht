using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation.Services;

public interface IRecurringTransactionCreateSheetService
{
    Task<bool> ShowAsync(RecurringTransactionDetailViewModel viewModel);
}
