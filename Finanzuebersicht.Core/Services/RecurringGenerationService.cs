using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Core.Services;

public class RecurringGenerationService(
    IRecurringTransactionRepository recurringRepository,
    ITransactionRepository transactionRepository,
    Finanzuebersicht.Core.Services.IClock? clock = null,
    ILogger<RecurringGenerationService>? logger = null,
    IAccountRepository? accountRepository = null,
    ILocalChangeNotifier? localChangeNotifier = null) : IRecurringGenerationService
{
    private readonly IRecurringTransactionRepository _recurringRepository = recurringRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IAccountRepository? _accountRepository = accountRepository;
    private readonly Finanzuebersicht.Core.Services.IClock _clock = clock ?? Finanzuebersicht.Core.Services.SystemClock.Instance;
    private readonly ILogger<RecurringGenerationService>? _logger = logger;
    private readonly ILocalChangeNotifier? _localChangeNotifier = localChangeNotifier;
    private const int MaxInstancesPerRun = 500;

    public async Task GeneratePendingRecurringTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var recurringItems = await _recurringRepository.GetRecurringTransactionsAsync();
        var defaultAccountId = await ResolveDefaultAccountIdAsync(cancellationToken);
        var today = _clock.Today;
        var existingTransactionIds = (await _transactionRepository.GetAllTransactionsAsync(cancellationToken))
            .Select(t => t.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var recurring in recurringItems.Where(item => item.Aktiv))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (recurring.Enddatum.HasValue && recurring.Enddatum.Value < today)
                continue;

            // Guard against corrupted future LetzteAusfuehrung
            if (recurring.LetzteAusfuehrung.HasValue && recurring.LetzteAusfuehrung.Value.Date > today)
            {
                _logger?.LogWarning(
                    "RecurringTransaction {Id} ('{Titel}') has LetzteAusfuehrung {Date} in the future. Skipping.",
                    recurring.Id, recurring.Titel, recurring.LetzteAusfuehrung.Value.Date);
                continue;
            }

            var generatedCount = 0;

            // determine the first instance to consider
            var candidate = !recurring.LetzteAusfuehrung.HasValue
                ? recurring.Startdatum
                : RecurringScheduleCalculator.GetNextInstance(recurring, recurring.LetzteAusfuehrung.Value);

            while (candidate <= today && generatedCount < MaxInstancesPerRun)
            {
                // check for exceptions for this instance date
                var exception = recurring.Exceptions?.FirstOrDefault(e => e.InstanceDate.Date == candidate.Date);
                if (exception != null && exception.Type == RecurringExceptionType.Skip)
                {
                    // mark as executed (skip this instance)
                    recurring.LetzteAusfuehrung = candidate;
                    candidate = RecurringScheduleCalculator.GetNextInstance(recurring, candidate);
                    continue;
                }

                var transactionDate = candidate;
                if (exception != null && exception.Type == RecurringExceptionType.Shift && exception.ShiftToDate.HasValue)
                {
                    transactionDate = exception.ShiftToDate.Value.Date;
                }

                var instanceId = RecurringInstanceIds.For(recurring.Id, candidate);
                if (existingTransactionIds.Contains(instanceId))
                {
                    recurring.LetzteAusfuehrung = candidate;
                    candidate = RecurringScheduleCalculator.GetNextInstance(recurring, candidate);
                    continue;
                }

                var transaction = new Transaction
                {
                    Id = instanceId,
                    Betrag = recurring.Betrag,
                    Titel = recurring.Titel,
                    Datum = transactionDate,
                    KategorieId = recurring.KategorieId,
                    AccountId = recurring.AccountId ?? defaultAccountId,
                    Typ = recurring.Typ,
                    DauerauftragId = recurring.Id
                };

                recurring.AccountId ??= defaultAccountId;
                await _transactionRepository.SaveTransactionAsync(transaction);
                existingTransactionIds.Add(instanceId);
                generatedCount++;

                if (_localChangeNotifier is not null)
                    await _localChangeNotifier.NotifyTransactionUpsertAsync(transaction.Id, cancellationToken);

                // mark this instance as last executed (use the template instance date)
                recurring.LetzteAusfuehrung = candidate;
                candidate = RecurringScheduleCalculator.GetNextInstance(recurring, candidate);
            }

            // Warn if the limit was hit and instances are still pending
            if (generatedCount == MaxInstancesPerRun && candidate <= today)
            {
                _logger?.LogWarning(
                    "RecurringTransaction {Id} ('{Titel}') hit the {Limit}-instance limit. " +
                    "Oldest pending instance: {PendingDate}. Remaining instances will be generated on next run.",
                    recurring.Id, recurring.Titel, MaxInstancesPerRun, candidate.Date);
            }

            await _recurringRepository.SaveRecurringTransactionAsync(recurring);
            if (_localChangeNotifier is not null)
                await _localChangeNotifier.NotifyRecurringUpsertAsync(recurring.Id, cancellationToken);
        }
    }

    private async Task<string?> ResolveDefaultAccountIdAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_accountRepository == null) return null;

        var accounts = await _accountRepository.GetAccountsAsync();
        return accounts.FirstOrDefault(a => a.SystemKey == Finanzuebersicht.Constants.SystemAccountKeys.Default)?.Id
            ?? accounts.FirstOrDefault()?.Id;
    }

    private static DateTime GetNextInstance(RecurringTransaction recurring, DateTime fromDate)
        => RecurringScheduleCalculator.GetNextInstance(recurring, fromDate);

    private static DateTime AddMonthsPreserveDay(DateTime date, int months)
        => RecurringScheduleCalculator.AddMonthsPreserveDay(date, months);
}
