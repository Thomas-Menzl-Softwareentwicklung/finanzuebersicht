using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation.Services;

public interface ICategoryCreateSheetService
{
    Task<bool> ShowAsync(CategoryDetailViewModel viewModel);
}
