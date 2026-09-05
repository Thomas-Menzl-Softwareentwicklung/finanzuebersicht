using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation.Services;

public interface IAccountCreateSheetService
{
    Task<bool> ShowAsync(AccountDetailViewModel viewModel);
}
