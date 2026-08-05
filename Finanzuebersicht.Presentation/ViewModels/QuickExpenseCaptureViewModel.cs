using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public partial class QuickExpenseCaptureViewModel(
    CaptureQuickExpenseUseCase captureQuickExpenseUseCase,
    ILocalizationService localizationService,
    IDialogService dialogService,
    ILicenseService? licenseService = null) : ObservableObject
{
    private readonly CaptureQuickExpenseUseCase _captureQuickExpenseUseCase = captureQuickExpenseUseCase;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILicenseService _licenseService =
        licenseService ?? UnrestrictedLicenseService.Instance;

    [ObservableProperty]
    private string betragText = string.Empty;

    [ObservableProperty]
    private string titel = string.Empty;

    public string PageTitle => _loc.GetString(ResourceKeys.Title_SchnellAusgabe);

    public void Reset()
    {
        BetragText = string.Empty;
        Titel = string.Empty;
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
}
