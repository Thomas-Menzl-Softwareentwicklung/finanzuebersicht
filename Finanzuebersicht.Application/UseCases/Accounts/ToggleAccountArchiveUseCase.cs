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

    public async Task<Account> ExecuteAsync(Account account, bool isArchived, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (account.IsSystemAccount)
            throw new InvalidOperationException("System account cannot be archived.");

        account.IsArchived = isArchived;
        CloudSyncNotify.StampUpdatedAt(account);
        await _accountRepository.SaveAccountAsync(account);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Account,
            account.Id,
            cancellationToken);
        return account;
    }
}
