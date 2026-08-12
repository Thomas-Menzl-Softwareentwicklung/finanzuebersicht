using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Services;

/// <summary>
/// Quick-expense create sheet via <see cref="ICreateFormModalService"/> (page sheet, no Toolkit Popup).
/// Widget deep links still use the Shell <c>QuickExpenseCapturePage</c>.
/// </summary>
public sealed class QuickExpenseCaptureSheetService(ICreateFormModalService createFormModalService)
    : IQuickExpenseCaptureSheetService
{
    public Task<bool> ShowAsync(QuickExpenseCaptureViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        return createFormModalService.ShowAsync(
            viewModel.PageTitle,
            () => new QuickExpenseFormView { BindingContext = viewModel },
            viewModel.TrySaveAsync,
            saveText: loc[ResourceKeys.Btn_Speichern]);
    }
}
