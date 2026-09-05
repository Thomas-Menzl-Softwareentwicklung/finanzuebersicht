using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Backup;

public class CreateBackupUseCase(IBackupService backupService)
{
    private readonly IBackupService _backupService = backupService;

    public async Task<UseCaseResult<BackupMetadata>> ExecuteAsync(
        string? backupPath = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var metadata = await _backupService.CreateBackupAsync(backupPath).ConfigureAwait(false);
            return UseCaseResult.Ok(metadata);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return UseCaseResult.Fail<BackupMetadata>(UseCaseErrorCode.BackupFailed);
        }
    }
}
