using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.RecurringTransactions;

public class BookDueRecurringInstanceUseCase(
    IRecurringTransactionRepository recurringTransactionRepository,
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task<UseCaseResult> ExecuteAsync(
        string recurringTransactionId,
        DateTime instanceDate,
        decimal? amountOverride = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var list = await recurringTransactionRepository.GetRecurringTransactionsAsync();
        var recurring = list.FirstOrDefault(r => r.Id == recurringTransactionId);
        if (recurring is null)
            return UseCaseResult.Fail(UseCaseErrorCode.RecurringNotFound);

        var effectiveDate = RecurringScheduleCalculator.ApplyExceptions(recurring, instanceDate.Date);
        var amount = amountOverride ?? recurring.Betrag;

        var existing = await transactionRepository.GetTransactionsAsync(
            effectiveDate.Date,
            effectiveDate.Date.AddDays(1).AddTicks(-1));
        if (existing.Any(t => t.DauerauftragId == recurring.Id))
            return UseCaseResult.Fail(UseCaseErrorCode.RecurringAlreadyBooked);

        var accountId = recurring.AccountId;
        if (string.IsNullOrWhiteSpace(accountId))
        {
            var accounts = await accountRepository.GetAccountsAsync();
            accountId = accounts.FirstOrDefault(a => a.SystemKey == SystemAccountKeys.Default && !a.IsArchived)?.Id
                ?? accounts.FirstOrDefault(a => !a.IsArchived)?.Id;
        }

        if (string.IsNullOrWhiteSpace(accountId))
            return UseCaseResult.Fail(UseCaseErrorCode.NoActiveAccount);

        var transaction = new Transaction
        {
            Id = RecurringInstanceIds.For(recurring.Id, instanceDate.Date),
            Betrag = amount,
            Titel = recurring.Titel,
            KategorieId = recurring.KategorieId,
            AccountId = accountId,
            Typ = recurring.Typ,
            Datum = effectiveDate,
            DauerauftragId = recurring.Id
        };
        CloudSyncNotify.StampUpdatedAt(transaction);

        await transactionRepository.SaveTransactionAsync(transaction);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.Transaction,
            transaction.Id,
            cancellationToken);

        recurring.LetzteAusfuehrung = instanceDate.Date;
        CloudSyncNotify.StampUpdatedAt(recurring);
        await recurringTransactionRepository.SaveRecurringTransactionAsync(recurring);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.RecurringTransaction,
            recurring.Id,
            cancellationToken);

        return UseCaseResult.Ok();
    }
}
