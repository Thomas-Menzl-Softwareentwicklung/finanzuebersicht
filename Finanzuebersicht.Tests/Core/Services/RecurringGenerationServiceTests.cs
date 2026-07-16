using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finanzuebersicht.Models;
using Finanzuebersicht.Tests.TestHelpers;
using Xunit;
using NSubstitute;

namespace Finanzuebersicht.Tests.Services;

public class RecurringGenerationServiceTests
{
    [Fact]
    public async Task Weekly_GeneratesExpectedTransactions()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var start = DateTime.Today.AddDays(-15); // ~2 weeks ago
        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Weekly",
            Betrag = 10m,
            Typ = TransactionType.Ausgabe,
            Startdatum = start,
            Aktiv = true,
            KategorieId = "cat-1",
            Interval = RecurrenceInterval.Weekly,
            IntervalFactor = 1
        };

        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var saved = new List<Transaction>();
        transactionRepository.When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(call => saved.Add(call.ArgNotNull<Transaction>()));

        var service = new RecurringGenerationService(recurringRepository, transactionRepository);
        await service.GeneratePendingRecurringTransactionsAsync();

        Assert.True(saved.Count >= 2);
        Assert.All(saved, t =>
        {
            Assert.Equal(recurring.Id, t.DauerauftragId);
            Assert.Equal("Weekly", t.Titel);
            Assert.Equal(10m, t.Betrag);
            Assert.Null(t.AccountId);
        });

        // expected last candidate: advance by 7 days until <= today
        var expected = start;
        while (expected.AddDays(7) <= DateTime.Today)
            expected = expected.AddDays(7);

        await recurringRepository.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.LetzteAusfuehrung.HasValue && r.LetzteAusfuehrung.Value.Date == expected.Date));
    }

    [Fact]
    public async Task Quarterly_GeneratesExpectedTransactions()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var start = new DateTime(DateTime.Today.Year - 1, 12, 31); // end of last year
        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Quarterly",
            Betrag = 100m,
            Typ = TransactionType.Ausgabe,
            Startdatum = start,
            Aktiv = true,
            KategorieId = "cat-q",
            Interval = RecurrenceInterval.Quarterly,
            IntervalFactor = 1
        };

        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var saved = new List<Transaction>();
        transactionRepository.When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(call => saved.Add(call.ArgNotNull<Transaction>()));

        var service = new RecurringGenerationService(recurringRepository, transactionRepository);
        await service.GeneratePendingRecurringTransactionsAsync();

        // At least one quarterly instance should be generated (depending on current date)
        Assert.True(saved.Count >= 1);
        Assert.All(saved, t => Assert.Equal(recurring.Id, t.DauerauftragId));
    }

    [Fact]
    public async Task SkipException_SkipsInstanceButUpdatesLastExecution()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var start = DateTime.Today.AddMonths(-2);
        var instanceToSkip = start.AddMonths(1);

        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Monthly",
            Betrag = 50m,
            Typ = TransactionType.Ausgabe,
            Startdatum = start,
            Aktiv = true,
            KategorieId = "cat-m",
            Interval = RecurrenceInterval.Monthly,
            IntervalFactor = 1,
            Exceptions = new List<RecurringException>
            {
                new RecurringException { InstanceDate = instanceToSkip, Type = RecurringExceptionType.Skip }
            }
        };

        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var saved = new List<Transaction>();
        transactionRepository.When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(call => saved.Add(call.ArgNotNull<Transaction>()));

        var service = new RecurringGenerationService(recurringRepository, transactionRepository);
        await service.GeneratePendingRecurringTransactionsAsync();

        // Ensure no saved transaction has Datum == instanceToSkip
        Assert.DoesNotContain(saved, t => t.Datum.Date == instanceToSkip.Date);

        // But repository should be saved with LetzteAusfuehrung at least up to that skipped instance
        await recurringRepository.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.LetzteAusfuehrung.HasValue && r.LetzteAusfuehrung.Value.Date >= instanceToSkip.Date));
    }

    [Fact]
    public async Task ShiftException_CreatesTransactionAtShiftedDate()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var start = DateTime.Today.AddMonths(-1);
        var instance = start.AddMonths(1);
        var shifted = instance.AddDays(3);

        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "MonthlyShift",
            Betrag = 75m,
            Typ = TransactionType.Ausgabe,
            Startdatum = start,
            Aktiv = true,
            KategorieId = "cat-s",
            Interval = RecurrenceInterval.Monthly,
            IntervalFactor = 1,
            Exceptions = new List<RecurringException>
            {
                new RecurringException { InstanceDate = instance, Type = RecurringExceptionType.Shift, ShiftToDate = shifted }
            }
        };

        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var saved = new List<Transaction>();
        transactionRepository.When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(call => saved.Add(call.ArgNotNull<Transaction>()));

        var service = new RecurringGenerationService(recurringRepository, transactionRepository);
        await service.GeneratePendingRecurringTransactionsAsync();

        Assert.Contains(saved, t => t.Datum.Date == shifted.Date && t.DauerauftragId == recurring.Id);
        await recurringRepository.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.LetzteAusfuehrung.HasValue));
    }

    [Fact]
    public async Task GeneratesTransaction_WithFallbackDefaultAccount_WhenRecurringHasNoAccount()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();

        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Monthly",
            Betrag = 20m,
            Typ = TransactionType.Ausgabe,
            Startdatum = DateTime.Today.AddMonths(-1),
            Aktiv = true,
            KategorieId = "cat-a",
            Interval = RecurrenceInterval.Monthly,
            IntervalFactor = 1
        };

        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-default", Name = "Girokonto", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default }
        });

        var saved = new List<Transaction>();
        transactionRepository.When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(call => saved.Add(call.ArgNotNull<Transaction>()));

        var service = new RecurringGenerationService(recurringRepository, transactionRepository, accountRepository: accountRepository);
        await service.GeneratePendingRecurringTransactionsAsync();

        Assert.NotEmpty(saved);
        Assert.All(saved, t => Assert.Equal("acc-default", t.AccountId));
        await recurringRepository.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.AccountId == "acc-default"));
    }

    [Fact]
    public async Task FutureLetzteAusfuehrung_SkipsRecurringAndDoesNotSave()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var clock = new FixedClock(new DateTime(2024, 6, 1));

        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Future Guard",
            Betrag = 50m,
            Typ = TransactionType.Ausgabe,
            Startdatum = new DateTime(2024, 1, 1),
            LetzteAusfuehrung = new DateTime(2024, 12, 31), // in the future relative to clock
            Aktiv = true,
            KategorieId = "cat-1",
            Interval = RecurrenceInterval.Monthly,
            IntervalFactor = 1
        };

        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var service = new RecurringGenerationService(recurringRepository, transactionRepository, clock);
        await service.GeneratePendingRecurringTransactionsAsync();

        await transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
        await recurringRepository.DidNotReceive().SaveRecurringTransactionAsync(Arg.Any<RecurringTransaction>());
    }

    [Fact]
    public async Task MaxInstancesPerRun_StopsAtLimitAndSavesState()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        // Start far enough back that more than 500 daily instances would be pending
        var clock = new FixedClock(new DateTime(2024, 6, 1));
        var start = new DateTime(2022, 1, 1); // ~880 days ago → 880 daily instances pending

        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Daily Limit Test",
            Betrag = 1m,
            Typ = TransactionType.Ausgabe,
            Startdatum = start,
            Aktiv = true,
            KategorieId = "cat-1",
            Interval = RecurrenceInterval.Daily,
            IntervalFactor = 1
        };

        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var saved = new List<Transaction>();
        transactionRepository.When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(call => saved.Add(call.ArgNotNull<Transaction>()));

        var service = new RecurringGenerationService(recurringRepository, transactionRepository, clock);
        await service.GeneratePendingRecurringTransactionsAsync();

        Assert.Equal(500, saved.Count);
        // LetzteAusfuehrung must be updated to the last generated instance
        await recurringRepository.Received(1).SaveRecurringTransactionAsync(
            NonNullArg.Is<RecurringTransaction>(r => r.LetzteAusfuehrung.HasValue));
    }
}
