using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Backup;

public class ExportCsvUseCase(IBackupService backupService)
{
    private readonly IBackupService _backupService = backupService;

    public async Task<UseCaseResult<Stream>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var stream = await _backupService.ExportAsCSVAsync().ConfigureAwait(false);
            return UseCaseResult.Ok(stream);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return UseCaseResult.Fail<Stream>(UseCaseErrorCode.BackupExportFailed);
        }
    }
}
