using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;
using Finanzuebersicht.Views.Popups;

namespace Finanzuebersicht.Services;

public sealed class QuickExpenseCaptureSheetService : IQuickExpenseCaptureSheetService
{
    public async Task<bool> ShowAsync(QuickExpenseCaptureViewModel viewModel)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return false;

        var form = new QuickExpenseFormView { BindingContext = viewModel };

        return await page.ShowFormSheetAsync(
            viewModel.PageTitle,
            form,
            viewModel.TrySaveAsync,
            saveText: LocalizationResourceManager.Current[ResourceKeys.Btn_Speichern]);
    }
}
