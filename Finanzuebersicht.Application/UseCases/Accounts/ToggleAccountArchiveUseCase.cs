using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Accounts;

public class ToggleAccountArchiveUseCase(
    IAccountRepository accountRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task<UseCaseResult<Account>> ExecuteAsync(
        Account account,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (account.IsSystemAccount)
            return UseCaseResult.Fail<Account>(UseCaseErrorCode.SystemAccountCannotArchive);

        account.IsArchived = isArchived;
        CloudSyncNotify.StampUpdatedAt(account);
        await _accountRepository.SaveAccountAsync(account);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Account,
            account.Id,
            cancellationToken);
        return UseCaseResult.Ok(account);
    }
}
