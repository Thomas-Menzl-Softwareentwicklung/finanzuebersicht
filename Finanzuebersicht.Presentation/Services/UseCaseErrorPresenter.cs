using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.Presentation.Services;

/// <summary>
/// Maps <see cref="UseCaseError"/> to localized UI strings and shows alerts.
/// </summary>
public static class UseCaseErrorPresenter
{
    public static string GetMessage(ILocalizationService loc, UseCaseError error)
    {
        ArgumentNullException.ThrowIfNull(loc);
        ArgumentNullException.ThrowIfNull(error);

        return error.Code switch
        {
            UseCaseErrorCode.AccountNotFound => loc.GetString(ResourceKeys.Err_KontoNichtGefunden),
            UseCaseErrorCode.AccountArchived => loc.GetString(ResourceKeys.Err_KontoArchiviert),
            UseCaseErrorCode.TransferAccountsRequired => loc.GetString(ResourceKeys.Err_TransferKontenErforderlich),
            UseCaseErrorCode.TransferAccountsMustDiffer => loc.GetString(ResourceKeys.Err_TransferKontenUnterschiedlich),
            UseCaseErrorCode.TransferAmountMustBePositive => loc.GetString(ResourceKeys.Err_BetragGroesserNull),
            UseCaseErrorCode.TransferMustUseTransferFlow => loc.GetString(ResourceKeys.Err_UmbuchungNichtBearbeitbar),
            UseCaseErrorCode.LicenseLimitReached => loc.GetString(
                ResourceKeys.Err_LimitErreicht,
                error.FormatArgs.ElementAtOrDefault(0) ?? 0,
                error.FormatArgs.ElementAtOrDefault(1) ?? 0),
            UseCaseErrorCode.BackupNotFound => loc.GetString(ResourceKeys.Err_BackupNichtGefunden),
            UseCaseErrorCode.BackupCorrupt => loc.GetString(ResourceKeys.Err_BackupBeschaedigt),
            UseCaseErrorCode.BackupSchemaIncompatible => loc.GetString(ResourceKeys.Err_BackupSchemaInkompatibel),
            UseCaseErrorCode.BackupDataInconsistent => loc.GetString(ResourceKeys.Msg_RestoreInconsistentDesc),
            UseCaseErrorCode.BackupRestoreFailed => loc.GetString(ResourceKeys.Msg_RestoreFailedTitle),
            UseCaseErrorCode.BackupExportFailed => loc.GetString(ResourceKeys.Msg_CSVExportFailedTitle),
            UseCaseErrorCode.BackupFailed => loc.GetString(ResourceKeys.Msg_BackupFailedTitle),
            _ => loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, error.Code.ToString())
        };
    }

    public static Task ShowAsync(IDialogService dialogService, ILocalizationService loc, UseCaseError error)
        => dialogService.ShowAlertAsync(
            loc.GetString(ResourceKeys.Err_Titel),
            GetMessage(loc, error),
            loc.GetString(ResourceKeys.Btn_OK));
}
