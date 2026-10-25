using Finanzuebersicht.Application.UseCases.Dashboard;
using Finanzuebersicht.Models;
using Finanzuebersicht.Tests.TestHelpers;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class LoadCashflowOutlookUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_IncludesTransactionsAndProjectedRecurring()
    {
        var today = new DateTime(2026, 3, 15);
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Titel = "Gehalt",
                    Betrag = 3000m,
                    Typ = TransactionType.Einnahme,
                    Datum = new DateTime(2026, 3, 20),
                    AccountId = "acc-1"
                }
            });

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>
        {
            new()
            {
                Id = "rec-1",
                Titel = "Miete",
                Betrag = 900m,
                Typ = TransactionType.Ausgabe,
                Aktiv = true,
                Startdatum = new DateTime(2026, 1, 1),
                Interval = RecurrenceInterval.Monthly,
                AccountId = "acc-1"
            }
        });

        var sut = new LoadCashflowOutlookUseCase(transactionRepository, recurringRepository, new FixedClock(today));
        var result = await sut.ExecuteAsync(accountId: "acc-1");

        Assert.Contains(result.Days, d => d.Entries.Any(e => e.Title == "Gehalt" && !e.IsProjected));
        Assert.Contains(result.Days, d => d.Entries.Any(e => e.Title == "Miete" && e.IsProjected));
        Assert.Contains(result.Days.SelectMany(d => d.Entries), e => e.Title == "Miete" && !e.IsEstimatedAmount);
        Assert.Equal(3000m, result.ProjectedIncome);
    }

    [Fact]
    public async Task ExecuteAsync_UsesAndMarksEstimateForVariableRecurringAmounts()
    {
        var today = new DateTime(2026, 3, 1);
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { Id = "tx-actual", Titel = "Paid", Betrag = 20m, Typ = TransactionType.Ausgabe, Datum = today, AccountId = "acc-1" }
            });
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>
        {
            new()
            {
                Id = "rec-variable-first", Titel = "Water", Betrag = 35m,
                IsVariableAmount = true, Typ = TransactionType.Ausgabe, Aktiv = true,
                Startdatum = today, Interval = RecurrenceInterval.Monthly, AccountId = "acc-1"
            },
            new()
            {
                Id = "rec-variable-last", Titel = "Power", Betrag = 60m,
                IsVariableAmount = true, LastBookedAmount = 72m, Typ = TransactionType.Ausgabe, Aktiv = true,
                Startdatum = today, Interval = RecurrenceInterval.Monthly, AccountId = "acc-1"
            }
        });

        var sut = new LoadCashflowOutlookUseCase(transactionRepository, recurringRepository, new FixedClock(today));
        var result = await sut.ExecuteAsync(accountId: "acc-1");
        var entries = result.Days.SelectMany(day => day.Entries).ToList();

        var initialEstimate = Assert.Single(entries, entry => entry.Title == "Water");
        Assert.Equal(35m, initialEstimate.Amount);
        Assert.True(initialEstimate.IsProjected);
        Assert.True(initialEstimate.IsEstimatedAmount);
        var lastEstimate = Assert.Single(entries, entry => entry.Title == "Power");
        Assert.Equal(72m, lastEstimate.Amount);
        Assert.True(lastEstimate.IsEstimatedAmount);
        var actual = Assert.Single(entries, entry => entry.Title == "Paid");
        Assert.False(actual.IsEstimatedAmount);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesOverdueUnbookedRecurringOnToday()
    {
        var today = new DateTime(2026, 3, 15);
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>());

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>
        {
            new()
            {
                Id = "rec-overdue",
                Titel = "Versicherung",
                Betrag = 50m,
                Typ = TransactionType.Ausgabe,
                Aktiv = true,
                Startdatum = new DateTime(2026, 1, 1),
                Interval = RecurrenceInterval.Monthly,
                AccountId = "acc-1"
            }
        });

        var sut = new LoadCashflowOutlookUseCase(transactionRepository, recurringRepository, new FixedClock(today));
        var result = await sut.ExecuteAsync(accountId: "acc-1");

        var todayGroup = result.Days.Single(d => d.Date == today);
        var overdueEntry = Assert.Single(todayGroup.Entries, e => e.Title == "Versicherung");
        Assert.True(overdueEntry.IsOverdue);
        Assert.True(overdueEntry.IsProjected);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsProjectedRecurring_WhenAlreadyBooked()
    {
        var today = new DateTime(2026, 3, 1);
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Titel = "Miete",
                    Betrag = 900m,
                    Typ = TransactionType.Ausgabe,
                    Datum = new DateTime(2026, 3, 1),
                    AccountId = "acc-1",
                    DauerauftragId = "rec-1"
                }
            });

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>
        {
            new()
            {
                Id = "rec-1",
                Titel = "Miete",
                Betrag = 900m,
                Typ = TransactionType.Ausgabe,
                Aktiv = true,
                Startdatum = new DateTime(2026, 1, 1),
                Interval = RecurrenceInterval.Monthly,
                AccountId = "acc-1"
            }
        });

        var sut = new LoadCashflowOutlookUseCase(transactionRepository, recurringRepository, new FixedClock(today));
        var result = await sut.ExecuteAsync(accountId: "acc-1");

        Assert.DoesNotContain(result.Days.SelectMany(d => d.Entries), e => e.Title == "Miete" && e.IsProjected);
    }

    [Fact]
    public async Task ExecuteAsync_MarksDaysWithLargeNetAsNotable()
    {
        var today = new DateTime(2026, 3, 1);
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new()
                {
                    Titel = "Gehalt",
                    Betrag = 2500m,
                    Typ = TransactionType.Einnahme,
                    Datum = today,
                    AccountId = "acc-1"
                }
            });

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>());

        var sut = new LoadCashflowOutlookUseCase(transactionRepository, recurringRepository, new FixedClock(today));
        var result = await sut.ExecuteAsync(accountId: "acc-1");

        var day = Assert.Single(result.Days);
        Assert.True(day.IsNotable);
    }
}
