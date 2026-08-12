using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation.Services;

public interface ISparZielCreateSheetService
{
    Task<bool> ShowAsync(SparZielDetailViewModel viewModel);
}
