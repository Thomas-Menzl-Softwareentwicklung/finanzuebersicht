using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Dashboard;

public class LoadCashflowOutlookUseCase(
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringTransactionRepository,
    Finanzuebersicht.Core.Services.IClock? clock = null)
{
    private readonly Finanzuebersicht.Core.Services.IClock _clock = clock ?? SystemClock.Instance;
    private const decimal NotableDayThreshold = 100m;
    private const int RecurringDedupLookbackDays = 120;

    public async Task<CashflowOutlookData> ExecuteAsync(
        int horizonDays = 30,
        string? accountId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var from = _clock.Today.Date;
        var to = from.AddDays(Math.Max(1, horizonDays));
        var entries = new List<CashflowEntry>();

        var allTransactions = await transactionRepository.GetTransactionsAsync(
            from.AddDays(-RecurringDedupLookbackDays),
            to);
        if (!string.IsNullOrWhiteSpace(accountId))
            allTransactions = allTransactions.Where(t => t.AccountId == accountId).ToList();

        foreach (var transaction in allTransactions.Where(t => !t.IsTransfer && t.Datum.Date >= from))
        {
            entries.Add(new CashflowEntry
            {
                Date = transaction.Datum.Date,
                Title = transaction.Titel,
                Amount = Math.Abs(transaction.Betrag),
                Typ = transaction.Typ,
                IsProjected = false
            });
        }

        var recurringItems = await recurringTransactionRepository.GetRecurringTransactionsAsync();

        foreach (var recurring in recurringItems.Where(r => r.Aktiv))
        {
            if (!string.IsNullOrWhiteSpace(accountId) && recurring.AccountId != accountId)
                continue;

            TryAddOverdueRecurring(entries, recurring, allTransactions, from);

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                if (!RecurringScheduleCalculator.OccursOnDate(recurring, day))
                    continue;

                if (IsRecurringBooked(allTransactions, recurring.Id, day))
                    continue;

                entries.Add(new CashflowEntry
                {
                    Date = day,
                    Title = recurring.Titel,
                    Amount = Math.Abs(recurring.Betrag),
                    Typ = recurring.Typ,
                    IsProjected = true
                });
            }
        }

        var days = entries
            .GroupBy(e => e.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var income = g.Where(e => e.Typ == TransactionType.Einnahme).Sum(e => e.Amount);
                var expenses = g.Where(e => e.Typ == TransactionType.Ausgabe).Sum(e => e.Amount);
                var net = income - expenses;
                return new CashflowDayGroup
                {
                    Date = g.Key,
                    Entries = g.OrderBy(e => e.Typ).ThenBy(e => e.Title).ToList(),
                    NetAmount = net,
                    IsNotable = Math.Abs(net) >= NotableDayThreshold
                };
            })
            .ToList();

        return new CashflowOutlookData
        {
            From = from,
            To = to,
            Days = days,
            ProjectedIncome = entries.Where(e => e.Typ == TransactionType.Einnahme).Sum(e => e.Amount),
            ProjectedExpenses = entries.Where(e => e.Typ == TransactionType.Ausgabe).Sum(e => e.Amount)
        };
    }

    private static void TryAddOverdueRecurring(
        List<CashflowEntry> entries,
        RecurringTransaction recurring,
        List<Transaction> transactions,
        DateTime from)
    {
        if (!RecurringScheduleCalculator.IsWithinActiveRange(recurring, from))
            return;

        if (IsRecurringBooked(transactions, recurring.Id, from))
            return;

        var candidate = recurring.LetzteAusfuehrung.HasValue
            ? RecurringScheduleCalculator.GetNextInstance(recurring, recurring.LetzteAusfuehrung.Value)
            : recurring.Startdatum.Date;

        DateTime? latestUnbookedDue = null;
        while (candidate < from)
        {
            var effective = RecurringScheduleCalculator.ApplyExceptions(recurring, candidate);
            if (!IsRecurringBooked(transactions, recurring.Id, effective))
                latestUnbookedDue = effective;

            candidate = RecurringScheduleCalculator.GetNextInstance(recurring, candidate);
        }

        if (latestUnbookedDue is null)
            return;

        if (entries.Any(e => e.IsProjected && e.Title == recurring.Titel && e.Date == from))
            return;

        entries.Add(new CashflowEntry
        {
            Date = from,
            Title = recurring.Titel,
            Amount = Math.Abs(recurring.Betrag),
            Typ = recurring.Typ,
            IsProjected = true,
            IsOverdue = true
        });
    }

    private static bool IsRecurringBooked(IEnumerable<Transaction> transactions, string recurringId, DateTime day)
        => transactions.Any(t => t.DauerauftragId == recurringId && t.Datum.Date == day.Date);

}
