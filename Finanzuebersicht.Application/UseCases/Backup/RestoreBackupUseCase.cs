using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Backup;

public class RestoreBackupUseCase(IBackupService backupService)
{
    private readonly IBackupService _backupService = backupService;

    public async Task<UseCaseResult<BackupMetadata?>> ExecuteAsync(
        string backupPath,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await _backupService.RestoreBackupAsync(backupPath, backupId).ConfigureAwait(false);
            if (result.Success)
                return UseCaseResult.Ok(result.RestoredMetadata);

            if (result.DataMayBeInconsistent || result.ErrorKind == BackupErrorKind.RestoreAndRollbackFailed)
                return UseCaseResult.Fail<BackupMetadata?>(UseCaseErrorCode.BackupDataInconsistent);

            return UseCaseResult.Fail<BackupMetadata?>(MapError(result.ErrorKind));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return UseCaseResult.Fail<BackupMetadata?>(UseCaseErrorCode.BackupRestoreFailed);
        }
    }

    private static UseCaseErrorCode MapError(BackupErrorKind kind) => kind switch
    {
        BackupErrorKind.NotFound => UseCaseErrorCode.BackupNotFound,
        BackupErrorKind.CorruptOrIncomplete => UseCaseErrorCode.BackupCorrupt,
        BackupErrorKind.SchemaIncompatible => UseCaseErrorCode.BackupSchemaIncompatible,
        BackupErrorKind.SaveFailed => UseCaseErrorCode.BackupFailed,
        BackupErrorKind.RestoreAndRollbackFailed => UseCaseErrorCode.BackupDataInconsistent,
        _ => UseCaseErrorCode.BackupRestoreFailed
    };
}
