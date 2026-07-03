using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;
using Finanzuebersicht.Views.Popups;

namespace Finanzuebersicht.Services;

public sealed class RecurringTransactionCreateSheetService : IRecurringTransactionCreateSheetService
{
    public async Task<bool> ShowAsync(RecurringTransactionDetailViewModel viewModel)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return false;

        var form = new RecurringTransactionFormView { BindingContext = viewModel };

        while (true)
        {
            var result = await page.ShowFormSheetAsync(
                viewModel.PageTitle,
                form,
                saveText: LocalizationResourceManager.Current[ResourceKeys.Btn_Speichern]);

            if (result == FormSheetResult.Cancelled)
                return false;

            if (await viewModel.TrySaveAsync())
                return true;
        }
    }
}
