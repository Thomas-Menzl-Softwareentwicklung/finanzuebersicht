using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Finanzuebersicht.ViewModels;

public partial class CategoryDetailViewModel(
    SaveCategoryDetailUseCase saveCategoryDetailUseCase,
    INavigationService navigationService,
    ILocalizationService localizationService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    IDialogService dialogService,
    SaveCategoryBudgetUseCase? saveCategoryBudgetUseCase = null,
    IBudgetRepository? budgetRepository = null,
    ILogger<CategoryDetailViewModel>? logger = null) : ObservableObject, IApplyQueryAttributes, ILocalizableViewModel
{
    private readonly SaveCategoryDetailUseCase _saveCategoryDetailUseCase = saveCategoryDetailUseCase;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly IDialogService _dialogService = dialogService;
    private readonly SaveCategoryBudgetUseCase? _saveCategoryBudgetUseCase = saveCategoryBudgetUseCase;
    private readonly IBudgetRepository? _budgetRepository = budgetRepository;
    private readonly ILogger<CategoryDetailViewModel>? _logger = logger;
    private Category? _existingCategory;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string icon = "💰";

    [ObservableProperty]
    private string color = "#007AFF";

    [ObservableProperty]
    private TransactionType typ = TransactionType.Ausgabe;

    [ObservableProperty]
    private TransactionTypeOption? selectedTypeOption;

    private List<TransactionTypeOption>? _verfuegbareTypen;

    public IReadOnlyList<TransactionTypeOption> VerfuegbareTypen =>
        _verfuegbareTypen ??= BuildTypeOptions();

    private List<TransactionTypeOption> BuildTypeOptions() =>
    [
        new(TransactionType.Einnahme, _loc.GetString(EnumResourceKeys.GetTransactionType(TransactionType.Einnahme))),
        new(TransactionType.Ausgabe, _loc.GetString(EnumResourceKeys.GetTransactionType(TransactionType.Ausgabe)))
    ];

    public void RefreshLocalizedStrings()
    {
        _verfuegbareTypen = null;
        OnPropertyChanged(nameof(VerfuegbareTypen));
        OnPropertyChanged(nameof(PageTitle));
        SelectedTypeOption = VerfuegbareTypen.FirstOrDefault(option => option.Value == Typ);
    }

    partial void OnTypChanged(TransactionType value)
    {
        SelectedTypeOption = VerfuegbareTypen.FirstOrDefault(option => option.Value == value);
    }

    partial void OnSelectedTypeOptionChanged(TransactionTypeOption? value)
    {
        if (value != null && Typ != value.Value)
            Typ = value.Value;
    }

    // String-backed property for culture-safe decimal input
    [ObservableProperty]
    private string monthlyBudgetText = string.Empty;

    public string PageTitle => _existingCategory == null 
        ? _loc.GetString(ResourceKeys.Title_NeueKategorie) 
        : _loc.GetString(ResourceKeys.Title_KategorieBearbeiten);

    public List<string> VerfuegbareIcons { get; } =
    [
        "🛒", "🚗", "🏠", "🎬", "💊", "💼", "📦",
        "🍽️", "☕", "🎮", "📱", "✈️", "🎓", "💰",
        "🏋️", "🎁", "👕", "🔧", "📰", "🐾"
    ];

    public List<string> VerfuegbareFarben { get; } =
    [
        "#007AFF", "#34C759", "#FF3B30", "#FF9500",
        "#AF52DE", "#5856D6", "#FF2D55", "#A2845E",
        "#00C7BE", "#32ADE6"
    ];

    public Category? Category
    {
        set
        {
            if (value != null)
            {
                _existingCategory = value;
                Name = value.Name;
                Icon = value.Icon;
                Color = value.Color;
                Typ = value.Typ;
                SelectedTypeOption = VerfuegbareTypen.FirstOrDefault(option => option.Value == Typ);
                _ = LoadBudgetAsync(value.Id);
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Category", out var val) && val is Category c)
            Category = c;
    }

    private async Task LoadBudgetAsync(string kategorieId)
    {
        try
        {
            if (_budgetRepository == null || string.IsNullOrEmpty(kategorieId)) return;
            // Always load the default budget (monat=null, jahr=null) — consistent with Save
            var budgets = await _budgetRepository.GetBudgetsAsync();
            var budget = budgets.FirstOrDefault(b => b.KategorieId == kategorieId && b.Monat == null && b.Jahr == null);
            var betrag = budget?.Betrag ?? 0;
            MonthlyBudgetText = betrag > 0 ? betrag.ToString("F2", CultureInfo.CurrentCulture) : string.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoryDetailViewModel: {Context}", nameof(LoadBudgetAsync));
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (await TrySaveAsync())
            await _navigationService.GoBackAsync();
    }

    public void ResetForCreate()
    {
        _existingCategory = null;
        Name = string.Empty;
        Icon = "💰";
        Color = "#007AFF";
        Typ = TransactionType.Ausgabe;
        MonthlyBudgetText = string.Empty;
        SelectedTypeOption = VerfuegbareTypen.FirstOrDefault(option => option.Value == Typ);
        OnPropertyChanged(nameof(PageTitle));
    }

    public async Task<bool> TrySaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_TitelErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }

        try
        {
            var savedCategory = await _saveCategoryDetailUseCase.ExecuteAsync(_existingCategory, Name, Icon, Color, Typ);
            _existingCategory = savedCategory;
            OnPropertyChanged(nameof(PageTitle));

            if (_saveCategoryBudgetUseCase != null && !string.IsNullOrEmpty(savedCategory.Id))
            {
                decimal.TryParse(MonthlyBudgetText, NumberStyles.Any, CultureInfo.CurrentCulture, out var budget);
                await _saveCategoryBudgetUseCase.ExecuteAsync(savedCategory.Id, budget);
            }

            _appEvents.NotifyDataChanged();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_Gespeichert));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CategoryDetailViewModel: {Context}", nameof(TrySaveAsync));
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return false;
        }
    }
}

public sealed record TransactionTypeOption(TransactionType Value, string DisplayName);
