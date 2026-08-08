using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

/// <summary>
/// Category list load/delete/navigation for the Verwaltung tab.
/// </summary>
public sealed class CategoriesListCoordinator(
    LoadCategoriesUseCase loadCategoriesUseCase,
    DeleteCategoryUseCase deleteCategoryUseCase,
    CategoryDetailViewModel createCategoryViewModel,
    ICategoryCreateSheetService categoryCreateSheetService,
    ILocalizationService localizationService,
    INavigationService navigationService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILogger<CategoriesListCoordinator>? logger = null)
{
    private readonly LoadCategoriesUseCase _loadCategoriesUseCase = loadCategoriesUseCase;
    private readonly DeleteCategoryUseCase _deleteCategoryUseCase = deleteCategoryUseCase;
    private readonly CategoryDetailViewModel _createCategoryViewModel = createCategoryViewModel;
    private readonly ICategoryCreateSheetService _categoryCreateSheetService = categoryCreateSheetService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILogger<CategoriesListCoordinator>? _logger = logger;

    public async Task<ObservableCollection<Category>> LoadAsync()
    {
        var liste = await _loadCategoriesUseCase.ExecuteAsync();
        return new ObservableCollection<Category>(liste);
    }

    public async Task<bool> TryDeleteAsync(Category kategorie, ObservableCollection<Category> kategorien)
    {
        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_KategorieLoeschen),
            _loc.GetString(ResourceKeys.Dlg_KategorieLoeschenFrage, kategorie.Name),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return false;

        try
        {
            await _deleteCategoryUseCase.ExecuteAsync(kategorie.Id);
            kategorien.Remove(kategorie);
            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Geloescht));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoriesListCoordinator: TryDeleteAsync failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LoeschenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }
    }

    public async Task<bool> NavigateToCreateAsync()
    {
        _createCategoryViewModel.ResetForCreate();
        return await _categoryCreateSheetService.ShowAsync(_createCategoryViewModel);
    }

    public Task NavigateToDetailAsync(Category kategorie)
        => _navigationService.GoToAsync(Routes.CategoryDetail, new Dictionary<string, object>
        {
            [NavigationQueryKeys.CategoryId] = kategorie.Id
        });
}
