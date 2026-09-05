using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Backup;

public class ListBackupsUseCase(IBackupService backupService)
{
    private readonly IBackupService _backupService = backupService;

    public async Task<UseCaseResult<IReadOnlyList<BackupMetadata>>> ExecuteAsync(
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var list = (await _backupService.ListBackupsAsync(backupPath).ConfigureAwait(false)).ToList();
            return UseCaseResult.Ok<IReadOnlyList<BackupMetadata>>(list);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return UseCaseResult.Fail<IReadOnlyList<BackupMetadata>>(UseCaseErrorCode.BackupFailed);
        }
    }
}
