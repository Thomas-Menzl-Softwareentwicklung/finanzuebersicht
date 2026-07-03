using Finanzuebersicht.Application.UseCases.Dashboard;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Tests.TestHelpers;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class RecurringScheduleConsistencyTests
{
    [Fact]
    public void MonthForecastAndCashflowDayLoop_AgreeOnSkippedMonthlyInstance()
    {
        var recurring = new RecurringTransaction
        {
            Id = "rec-skip",
            Titel = "Abo",
            Betrag = 15m,
            Typ = TransactionType.Ausgabe,
            Aktiv = true,
            Startdatum = new DateTime(2026, 1, 1),
            Interval = RecurrenceInterval.Monthly,
            Exceptions =
            [
                new RecurringException
                {
                    InstanceDate = new DateTime(2026, 4, 1),
                    Type = RecurringExceptionType.Skip
                }
            ]
        };

        var monthStart = new DateTime(2026, 4, 1);
        var monthEnd = new DateTime(2026, 4, 30);

        var occursInMonth = RecurringScheduleCalculator.OccursInRange(recurring, monthStart, monthEnd);
        var occursOnAnyDayInMonth = false;
        for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
        {
            if (RecurringScheduleCalculator.OccursOnDate(recurring, day))
            {
                occursOnAnyDayInMonth = true;
                break;
            }
        }

        Assert.False(occursInMonth);
        Assert.False(occursOnAnyDayInMonth);
    }

    [Fact]
    public async Task DashboardMonthForecast_ExcludesSkippedRecurringInFutureMonth()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var budgetRepository = Substitute.For<IBudgetRepository>();

        categoryRepository.GetCategoriesAsync().Returns(
        [
            new Category { Id = "cat-a", Name = "Abo", Typ = TransactionType.Ausgabe }
        ]);
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);
        recurringRepository.GetRecurringTransactionsAsync().Returns(
        [
            new RecurringTransaction
            {
                Id = "rec-1",
                Titel = "Streaming",
                Betrag = 15m,
                KategorieId = "cat-a",
                Typ = TransactionType.Ausgabe,
                Aktiv = true,
                Startdatum = new DateTime(2026, 1, 1),
                Interval = RecurrenceInterval.Monthly,
                Exceptions =
                [
                    new RecurringException
                    {
                        InstanceDate = new DateTime(2026, 4, 1),
                        Type = RecurringExceptionType.Skip
                    }
                ]
            }
        ]);

        var useCase = new LoadDashboardMonthUseCase(
            categoryRepository,
            transactionRepository,
            recurringRepository,
            budgetRepository);

        var result = await useCase.ExecuteAsync(new DateTime(2026, 4, 1), new DateTime(2026, 3, 15));

        Assert.True(result.IstPrognose);
        Assert.Equal(0m, result.GesamtAusgaben);
    }

    [Fact]
    public async Task CashflowOutlook_DoesNotProjectSkippedDayInstance()
    {
        var today = new DateTime(2026, 4, 1);
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(
        [
            new RecurringTransaction
            {
                Id = "rec-1",
                Titel = "Streaming",
                Betrag = 15m,
                Typ = TransactionType.Ausgabe,
                Aktiv = true,
                Startdatum = new DateTime(2026, 1, 1),
                Interval = RecurrenceInterval.Monthly,
                AccountId = "acc-1",
                Exceptions =
                [
                    new RecurringException
                    {
                        InstanceDate = new DateTime(2026, 4, 1),
                        Type = RecurringExceptionType.Skip
                    }
                ]
            }
        ]);

        var sut = new LoadCashflowOutlookUseCase(
            transactionRepository,
            recurringRepository,
            new FixedClock(today));

        var result = await sut.ExecuteAsync(accountId: "acc-1");

        Assert.DoesNotContain(
            result.Days.SelectMany(d => d.Entries),
            e => e.Title == "Streaming" && e.IsProjected && e.Date == new DateTime(2026, 4, 1) && !e.IsOverdue);
    }
}
