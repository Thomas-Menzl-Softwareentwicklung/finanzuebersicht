using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public partial class OnboardingViewModel(
    IOnboardingCoordinator onboardingCoordinator,
    INavigationService navigationService,
    ILocalizationService localizationService,
    IDisplayCurrencyService displayCurrency,
    LoadAccountsUseCase loadAccountsUseCase) : ObservableObject
{
    private readonly IOnboardingCoordinator _onboardingCoordinator = onboardingCoordinator;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDisplayCurrencyService _displayCurrency = displayCurrency;
    private readonly LoadAccountsUseCase _loadAccountsUseCase = loadAccountsUseCase;

    [ObservableProperty]
    private int currentStep;

    [ObservableProperty]
    private int selectedCurrencyIndex = displayCurrency.SelectedIndex;

    public int TotalSteps => 5;

    public string StepIndicator => $"{CurrentStep + 1} / {TotalSteps}";

    public bool ShowWelcomeSetup => CurrentStep == 0;
    public bool ShowAccountActions => CurrentStep == 1;
    public bool ShowTransactionActions => CurrentStep == 2;
    public bool ShowBackupActions => CurrentStep == 3;

    public string StepTitle => CurrentStep switch
    {
        0 => _loc.GetString(ResourceKeys.Onboarding_TitleWelcome),
        1 => _loc.GetString(ResourceKeys.Onboarding_TitleAccount),
        2 => _loc.GetString(ResourceKeys.Onboarding_TitleFirstTransaction),
        3 => _loc.GetString(ResourceKeys.Onboarding_TitleBackup),
        _ => _loc.GetString(ResourceKeys.Onboarding_TitleDone)
    };

    public string StepDescription => CurrentStep switch
    {
        0 => _loc.GetString(ResourceKeys.Onboarding_DescWelcome),
        1 => _loc.GetString(ResourceKeys.Onboarding_DescAccount),
        2 => _loc.GetString(ResourceKeys.Onboarding_DescFirstTransaction),
        3 => _loc.GetString(ResourceKeys.Onboarding_DescBackup),
        _ => _loc.GetString(ResourceKeys.Onboarding_DescDone)
    };

    public bool IsLastStep => CurrentStep >= TotalSteps - 1;
    public bool IsFirstStep => CurrentStep == 0;

    public string PrimaryButtonText => IsLastStep
        ? _loc.GetString(ResourceKeys.Onboarding_Btn_ZumDashboard)
        : _loc.GetString(ResourceKeys.Btn_Weiter);

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepDescription));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(IsFirstStep));
        OnPropertyChanged(nameof(PrimaryButtonText));
        OnPropertyChanged(nameof(StepIndicator));
        OnPropertyChanged(nameof(ShowWelcomeSetup));
        OnPropertyChanged(nameof(ShowAccountActions));
        OnPropertyChanged(nameof(ShowTransactionActions));
        OnPropertyChanged(nameof(ShowBackupActions));
    }

    partial void OnSelectedCurrencyIndexChanged(int value)
        => _displayCurrency.SelectedIndex = value;

    [RelayCommand]
    private void SetCurrency(string indexStr)
    {
        if (int.TryParse(indexStr, out var idx))
            SelectedCurrencyIndex = idx;
    }

    [RelayCommand]
    private void Next()
    {
        if (IsLastStep)
        {
            Complete();
            return;
        }

        CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    [RelayCommand]
    private void Skip()
        => Complete();

    [RelayCommand]
    private async Task OpenDefaultAccount()
    {
        var accounts = await _loadAccountsUseCase.ExecuteAsync();
        var account = accounts.FirstOrDefault(a => !a.IsArchived)
            ?? accounts.FirstOrDefault();
        if (account is null) return;

        await _navigationService.GoToAsync(Routes.AccountDetail, new Dictionary<string, object>
        {
            [NavigationQueryKeys.AccountId] = account.Id
        });
    }

    [RelayCommand]
    private async Task OpenNewTransaction()
    {
        CompleteNavigationPending();
        await _navigationService.GoToAsync(Routes.TransactionsTab);
        await _navigationService.GoToAsync(Routes.TransactionDetail);
    }

    [RelayCommand]
    private Task OpenSettings()
        => _navigationService.GoToAsync(Routes.Settings);

    private void CompleteNavigationPending()
    {
        _onboardingCoordinator.MarkCompleted();
        _ = _navigationService.GoBackAsync();
    }

    private void Complete()
    {
        _onboardingCoordinator.MarkCompleted();
        _ = _navigationService.GoBackAsync();
    }
}
