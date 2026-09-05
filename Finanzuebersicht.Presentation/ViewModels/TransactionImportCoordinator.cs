using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

/// <summary>
/// CSV import entry flow for the transactions list (license → pick → analyze → preview).
/// </summary>
public sealed class TransactionImportCoordinator(
    AnalyzeCsvImportUseCase analyzeCsvImportUseCase,
    IFilePicker filePicker,
    INavigationService navigationService,
    IDialogService dialogService,
    ILocalizationService localizationService,
    ILicenseService? licenseService = null,
    IImportSessionStore? importSessionStore = null,
    ILogger<TransactionImportCoordinator>? logger = null)
{
    private readonly AnalyzeCsvImportUseCase _analyzeCsvImportUseCase = analyzeCsvImportUseCase;
    private readonly IFilePicker _filePicker = filePicker;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly ILicenseService _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;
    private readonly IImportSessionStore? _importSessionStore = importSessionStore;
    private readonly ILogger<TransactionImportCoordinator>? _logger = logger;

    public async Task ImportCsvAsync(string? selectedAccountId)
    {
        if (!_licenseService.HasFeature(AppFeature.CsvImport))
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_ProErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            var picked = await _filePicker.PickAsync();
            if (picked == null) return;

            using var stream = await picked.OpenReadAsync();
            var preview = await _analyzeCsvImportUseCase.ExecuteAsync(stream, selectedAccountId);

            if (!preview.Success)
            {
                var errorDetail = string.IsNullOrWhiteSpace(preview.ErrorMessage)
                    ? "Unbekannter Fehler beim Import."
                    : _loc.GetString(preview.ErrorMessage);
                if (string.IsNullOrWhiteSpace(errorDetail) || errorDetail == preview.ErrorMessage)
                    errorDetail = preview.ErrorMessage ?? errorDetail;

                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_ImportFehlgeschlagen_Title),
                    errorDetail,
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            if (_importSessionStore == null)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtVerfuegbar_Title),
                    _loc.GetString(ResourceKeys.Msg_ImportVorschauNichtVerfuegbar_Body),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            _importSessionStore.Clear();
            _importSessionStore.SetActiveSession(preview);
            await _navigationService.GoToAsync(Routes.ImportPreview);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ImportCsv failed");
            var msg = ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : string.Empty);
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_ImportFehler_Title),
                msg,
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }
}
