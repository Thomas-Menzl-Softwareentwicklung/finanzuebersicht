using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class SaveTransferUseCase(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task<UseCaseResult> ExecuteAsync(
        string fromAccountId,
        string toAccountId,
        decimal amount,
        DateTime date,
        string? title = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fromAccountId) || string.IsNullOrWhiteSpace(toAccountId))
            return UseCaseResult.Fail(UseCaseErrorCode.TransferAccountsRequired);
        if (fromAccountId == toAccountId)
            return UseCaseResult.Fail(UseCaseErrorCode.TransferAccountsMustDiffer);
        if (amount <= 0)
            return UseCaseResult.Fail(UseCaseErrorCode.TransferAmountMustBePositive);

        var accounts = await _accountRepository.GetAccountsAsync();
        var source = accounts.FirstOrDefault(a => a.Id == fromAccountId);
        var target = accounts.FirstOrDefault(a => a.Id == toAccountId);
        if (source == null || target == null)
            return UseCaseResult.Fail(UseCaseErrorCode.AccountNotFound);
        if (source.IsArchived || target.IsArchived)
            return UseCaseResult.Fail(UseCaseErrorCode.AccountArchived);

        var transferGroupId = Guid.NewGuid().ToString();
        var transferTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        var transferNote = note ?? string.Empty;

        var outgoing = new Transaction
        {
            Betrag = amount,
            Titel = transferTitle,
            Verwendungszweck = transferNote,
            Datum = date,
            Typ = TransactionType.Ausgabe,
            KategorieId = string.Empty,
            AccountId = fromAccountId,
            IsTransfer = true,
            TransferGroupId = transferGroupId
        };
        CloudSyncNotify.StampUpdatedAt(outgoing);

        var incoming = new Transaction
        {
            Betrag = amount,
            Titel = transferTitle,
            Verwendungszweck = transferNote,
            Datum = date,
            Typ = TransactionType.Einnahme,
            KategorieId = string.Empty,
            AccountId = toAccountId,
            IsTransfer = true,
            TransferGroupId = transferGroupId
        };
        CloudSyncNotify.StampUpdatedAt(incoming);

        await _transactionRepository.SaveTransactionsAsync([outgoing, incoming]);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Transaction,
            outgoing.Id,
            cancellationToken);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Transaction,
            incoming.Id,
            cancellationToken);
        return UseCaseResult.Ok();
    }
}
