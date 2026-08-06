using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public partial class QuickExpenseCaptureViewModel(
    CaptureQuickExpenseUseCase captureQuickExpenseUseCase,
    ILocalizationService localizationService,
    IDialogService dialogService,
    INavigationService navigationService,
    IFeedbackService feedbackService,
    IAppEvents appEvents,
    ILicenseService? licenseService = null) : ObservableObject, IApplyQueryAttributes
{
    private readonly CaptureQuickExpenseUseCase _captureQuickExpenseUseCase = captureQuickExpenseUseCase;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly IAppEvents _appEvents = appEvents;
    private readonly ILicenseService _licenseService =
        licenseService ?? UnrestrictedLicenseService.Instance;

    [ObservableProperty]
    private string betragText = string.Empty;

    [ObservableProperty]
    private string titel = string.Empty;

    public string PageTitle => _loc.GetString(ResourceKeys.Title_SchnellAusgabe);

    public string Hinweis => _loc.GetString(ResourceKeys.Lbl_SchnellAusgabeHinweis);

    public void Reset()
    {
        BetragText = string.Empty;
        Titel = string.Empty;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationQueryKeys.Amount, out var amountObj))
            BetragText = amountObj?.ToString() ?? string.Empty;

        if (query.TryGetValue(NavigationQueryKeys.Title, out var titleObj))
            Titel = titleObj?.ToString() ?? string.Empty;
    }

    public bool EnsureProAccess()
    {
        return _licenseService.HasFeature(AppFeature.QuickExpenseCapture);
    }

    public async Task ShowProRequiredAsync()
    {
        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Err_Titel),
            _loc.GetString(ResourceKeys.Err_ProErforderlich),
            _loc.GetString(ResourceKeys.Btn_OK));
    }

    public async Task<bool> TrySaveAsync()
    {
        if (!EnsureProAccess())
        {
            await ShowProRequiredAsync();
            return false;
        }

        try
        {
            var result = await _captureQuickExpenseUseCase.ExecuteAsync(
                BetragText,
                Titel,
                CultureInfo.CurrentCulture);

            if (!result.Success)
            {
                var message = result.ValidationError switch
                {
                    TransactionInputError.InvalidAmountFormat => _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                    TransactionInputError.AmountMustBePositive => _loc.GetString(ResourceKeys.Err_BetragGroesserNull),
                    TransactionInputError.TitleRequired => _loc.GetString(ResourceKeys.Err_TitelErforderlich),
                    _ => _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, string.Empty)
                };

                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Err_Titel),
                    message,
                    _loc.GetString(ResourceKeys.Btn_OK));
                return false;
            }

            return true;
        }
        catch (FeatureGateException)
        {
            await ShowProRequiredAsync();
            return false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!await TrySaveAsync())
            return;

        _appEvents.NotifyDataChanged();
        await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_SchnellAusgabeGespeichert));
        await _navigationService.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await _navigationService.GoToAsync("..");
    }
}
