using Finanzuebersicht.Application.UseCases.Backup;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public sealed record LicenseCloudSyncState(bool SyncEnabled, string StatusLine);

public sealed record LicenseCloudSyncToggleOutcome(
    bool SwitchEnabled,
    string? StatusLine,
    bool? MetadataSyncEnabled,
    bool RefreshLicenseLabels);

/// <summary>
/// Enable, replace-local, disable, and status text for the license screen.
/// Metadata and orchestrator access stay in application use cases.
/// </summary>
public sealed class LicenseCloudSyncCoordinator(
    ILicenseService licenseService,
    ILocalizationService localizationService,
    IDialogService dialogService,
    EnableCloudSyncUseCase enableCloudSyncUseCase,
    ClearLocalSyncedDataUseCase clearLocalSyncedDataUseCase,
    DisableCloudSyncUseCase disableCloudSyncUseCase,
    CreateBackupUseCase createBackupUseCase,
    GetCloudSyncStatusUseCase getCloudSyncStatusUseCase,
    RecordCloudSyncErrorUseCase recordCloudSyncErrorUseCase,
    StartCloudSyncUseCase startCloudSyncUseCase,
    IAppEvents appEvents)
{
    private readonly ILicenseService _licenseService = licenseService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly EnableCloudSyncUseCase _enableCloudSyncUseCase = enableCloudSyncUseCase;
    private readonly ClearLocalSyncedDataUseCase _clearLocalSyncedDataUseCase = clearLocalSyncedDataUseCase;
    private readonly DisableCloudSyncUseCase _disableCloudSyncUseCase = disableCloudSyncUseCase;
    private readonly CreateBackupUseCase _createBackupUseCase = createBackupUseCase;
    private readonly GetCloudSyncStatusUseCase _getCloudSyncStatusUseCase = getCloudSyncStatusUseCase;
    private readonly RecordCloudSyncErrorUseCase _recordCloudSyncErrorUseCase = recordCloudSyncErrorUseCase;
    private readonly StartCloudSyncUseCase _startCloudSyncUseCase = startCloudSyncUseCase;
    private readonly IAppEvents _appEvents = appEvents;

    public async Task<LicenseCloudSyncState?> TryReadStateAsync(CancellationToken cancellationToken = default)
    {
        if (!_licenseService.IsCloudSyncImplemented)
            return null;

        return ToState(await _getCloudSyncStatusUseCase.ExecuteAsync(cancellationToken));
    }

    public async Task<LicenseCloudSyncToggleOutcome> ToggleAsync(
        bool enable,
        bool lastKnownEnabled,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (enable)
                return await EnableAsync(cancellationToken);

            await _disableCloudSyncUseCase.ExecuteAsync(cancellationToken);
            return await EnabledOutcomeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _recordCloudSyncErrorUseCase.ExecuteAsync(ex.Message, cancellationToken);
            var snapshot = await _getCloudSyncStatusUseCase.ExecuteAsync(cancellationToken);
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Sync_Error, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
            return new LicenseCloudSyncToggleOutcome(
                lastKnownEnabled,
                BuildStatusLine(snapshot),
                null,
                false);
        }
    }

    private async Task<LicenseCloudSyncToggleOutcome> EnableAsync(CancellationToken cancellationToken)
    {
        var result = await _enableCloudSyncUseCase.ExecuteAsync(cancellationToken);
        if (result.Status == EnableCloudSyncStatus.Enabled)
            return await FinishEnableAsync(cancellationToken);

        if (result.Status == EnableCloudSyncStatus.BlockedBothHaveData)
            return await OfferReplaceLocalWithCloudAsync(cancellationToken);

        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Err_Titel),
            _loc.GetString(GetBlockedMessageKey(result.Status)),
            _loc.GetString(ResourceKeys.Btn_OK));
        return RevertedSwitch();
    }

    private async Task<LicenseCloudSyncToggleOutcome> OfferReplaceLocalWithCloudAsync(CancellationToken cancellationToken)
    {
        var replace = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Sync_ReplaceLocalWithCloudTitle),
            _loc.GetString(ResourceKeys.Sync_ReplaceLocalWithCloudMessage),
            _loc.GetString(ResourceKeys.Sync_ReplaceLocalWithCloudAccept),
            _loc.GetString(ResourceKeys.Btn_Abbrechen));
        if (!replace)
            return RevertedSwitch();

        var backup = await _createBackupUseCase.ExecuteAsync(cancellationToken: cancellationToken);
        if (!backup.IsSuccess)
        {
            await UseCaseErrorPresenter.ShowAsync(_dialogService, _loc, backup.Error!);
            return RevertedSwitch();
        }

        await _clearLocalSyncedDataUseCase.ExecuteAsync(cancellationToken);
        _appEvents.NotifyDataChanged();

        var result = await _enableCloudSyncUseCase.ExecuteAsync(cancellationToken);
        if (result.Status == EnableCloudSyncStatus.Enabled)
        {
            var outcome = await FinishEnableAsync(cancellationToken);
            _appEvents.NotifyDataChanged();
            return outcome;
        }

        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Err_Titel),
            _loc.GetString(GetBlockedMessageKey(result.Status)),
            _loc.GetString(ResourceKeys.Btn_OK));
        return RevertedSwitch();
    }

    private async Task<LicenseCloudSyncToggleOutcome> FinishEnableAsync(CancellationToken cancellationToken)
    {
        var outcome = await EnabledOutcomeAsync(cancellationToken);
        await _startCloudSyncUseCase.ExecuteAsync(cancellationToken);
        return outcome;
    }

    private async Task<LicenseCloudSyncToggleOutcome> EnabledOutcomeAsync(CancellationToken cancellationToken)
    {
        var state = ToState(await _getCloudSyncStatusUseCase.ExecuteAsync(cancellationToken));
        return new LicenseCloudSyncToggleOutcome(state.SyncEnabled, state.StatusLine, state.SyncEnabled, true);
    }

    private LicenseCloudSyncState ToState(CloudSyncStatusSnapshot snapshot) =>
        new(snapshot.SyncEnabled, BuildStatusLine(snapshot));

    private string BuildStatusLine(CloudSyncStatusSnapshot snapshot)
    {
        if (snapshot.SyncEnabled && !_licenseService.CanUseCloudSync)
            return _loc.GetString(ResourceKeys.Sync_PausedRenew);

        if (!string.IsNullOrWhiteSpace(snapshot.LastError))
            return _loc.GetString(ResourceKeys.Sync_Error, snapshot.LastError);

        if (snapshot.LastSyncUtc.HasValue)
        {
            var formatted = snapshot.LastSyncUtc.Value.ToLocalTime().ToString("g");
            return _loc.GetString(ResourceKeys.Sync_LastSync, formatted);
        }

        return _loc.GetString(ResourceKeys.Sync_NeverSynced);
    }

    private static LicenseCloudSyncToggleOutcome RevertedSwitch() =>
        new(false, null, null, false);

    private static string GetBlockedMessageKey(EnableCloudSyncStatus status) => status switch
    {
        EnableCloudSyncStatus.BlockedBothHaveData => ResourceKeys.Sync_BlockedBothHaveData,
        EnableCloudSyncStatus.BlockedNoEntitlement => ResourceKeys.Sync_BlockedNoEntitlement,
        EnableCloudSyncStatus.BlockedUnsupported => ResourceKeys.Sync_BlockedUnsupported,
        EnableCloudSyncStatus.BlockedNoICloud => ResourceKeys.Sync_BlockedNoICloud,
        _ => ResourceKeys.Sync_BlockedUnsupported
    };
}
