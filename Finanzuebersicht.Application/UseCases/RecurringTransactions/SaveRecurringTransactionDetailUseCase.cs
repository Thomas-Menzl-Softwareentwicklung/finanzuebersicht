using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.RecurringTransactions;

public class SaveRecurringTransactionDetailUseCase(
    IRecurringTransactionRepository recurringTransactionRepository,
    IRecurringGenerationService recurringGenerationService,
    IAccountRepository accountRepository,
    ILicenseService? licenseService = null,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;
    private readonly IRecurringGenerationService _recurringGenerationService = recurringGenerationService;
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly ILicenseService _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task<UseCaseResult> ExecuteAsync(
        RecurringTransaction? existing,
        decimal betrag,
        string titel,
        string kategorieId,
        string? accountId,
        TransactionType typ,
        DateTime startdatum,
        DateTime? enddatum,
        bool aktiv,
        RecurrenceInterval interval = RecurrenceInterval.Monthly,
        int intervalFactor = 1,
        int reminderDaysBefore = 0,
        List<RecurringException>? exceptions = null,
        CancellationToken cancellationToken = default)
    {
        if (existing == null)
        {
            var recurringItems = await _recurringTransactionRepository.GetRecurringTransactionsAsync() ?? [];
            _licenseService.EnsureCanCreate(LimitedResource.RecurringTransactions, recurringItems.Count);
        }

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            var accounts = await _accountRepository.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.Id == accountId);
            if (account is null)
                return UseCaseResult.Fail(UseCaseErrorCode.AccountNotFound);
            if (account.IsArchived && (existing == null || existing.AccountId != accountId))
                return UseCaseResult.Fail(UseCaseErrorCode.AccountArchived);
        }

        var recurring = existing ?? new RecurringTransaction();
        recurring.Betrag = betrag;
        recurring.Titel = titel;
        recurring.KategorieId = kategorieId;
        recurring.AccountId = accountId;
        recurring.Typ = typ;
        recurring.Startdatum = startdatum;
        recurring.Enddatum = enddatum;
        recurring.Aktiv = aktiv;
        recurring.Interval = interval;
        recurring.IntervalFactor = intervalFactor;
        recurring.ReminderDaysBefore = reminderDaysBefore;
        if (exceptions != null)
            recurring.Exceptions = exceptions;
        CloudSyncNotify.StampUpdatedAt(recurring);

        await _recurringTransactionRepository.SaveRecurringTransactionAsync(recurring);
        await CloudSyncNotify.NotifyUpsertAsync(
            _cloudSyncOrchestrator,
            SyncEntityType.RecurringTransaction,
            recurring.Id,
            cancellationToken);
        await _recurringGenerationService.GeneratePendingRecurringTransactionsAsync(cancellationToken);

        return UseCaseResult.Ok();
    }
}
