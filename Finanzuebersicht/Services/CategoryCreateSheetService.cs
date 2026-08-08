using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Services;

public sealed class CategoryCreateSheetService(ICreateFormModalService createFormModalService) : ICategoryCreateSheetService
{
    public Task<bool> ShowAsync(CategoryDetailViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        return createFormModalService.ShowAsync(
            viewModel.PageTitle,
            () => new CategoryFormView { BindingContext = viewModel },
            viewModel.TrySaveAsync,
            saveText: loc[ResourceKeys.Btn_Speichern]);
    }
}
