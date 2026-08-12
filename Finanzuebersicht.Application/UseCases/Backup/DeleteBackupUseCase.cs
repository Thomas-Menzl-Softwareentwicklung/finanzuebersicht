using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Backup;

public class DeleteBackupUseCase(IBackupService backupService)
{
    private readonly IBackupService _backupService = backupService;

    public async Task<UseCaseResult> ExecuteAsync(
        string backupPath,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _backupService.DeleteBackupAsync(backupPath, backupId).ConfigureAwait(false);
            return UseCaseResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return UseCaseResult.Fail(UseCaseErrorCode.BackupFailed);
        }
    }
}
