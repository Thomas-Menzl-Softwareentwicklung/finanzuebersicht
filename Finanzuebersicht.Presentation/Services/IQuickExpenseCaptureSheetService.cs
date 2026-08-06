using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation.Services;

public interface IQuickExpenseCaptureSheetService
{
    Task<bool> ShowAsync(QuickExpenseCaptureViewModel viewModel);
}
