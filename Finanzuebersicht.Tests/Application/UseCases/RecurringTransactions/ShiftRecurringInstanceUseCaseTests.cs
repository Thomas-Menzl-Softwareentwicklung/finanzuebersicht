using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Models;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Finanzuebersicht.Tests.Application.UseCases.RecurringTransactions;

public class ShiftRecurringInstanceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AddsShiftException_WhenNoneExists()
    {
        var recurring = new RecurringTransaction { Id = "r-1", Exceptions = new List<RecurringException>() };
        var repo = Substitute.For<IRecurringTransactionRepository>();
        repo.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var sut = new ShiftRecurringInstanceUseCase(repo);

        await sut.ExecuteAsync("r-1", new System.DateTime(2026,3,1), new System.DateTime(2026,3,5), "note");

        await repo.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.Exceptions.Count == 1 && r.Exceptions[0].Type == RecurringExceptionType.Shift && r.Exceptions[0].ShiftToDate == new System.DateTime(2026,3,5)));
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesExistingShiftException_WhenSameDateExists()
    {
        var existingEx = new RecurringException { Id = "e-old", InstanceDate = new System.DateTime(2026,3,1), Type = RecurringExceptionType.Shift, ShiftToDate = new System.DateTime(2026,3,4) };
        var recurring = new RecurringTransaction { Id = "r-1", Exceptions = new List<RecurringException> { existingEx } };
        var repo = Substitute.For<IRecurringTransactionRepository>();
        repo.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var sut = new ShiftRecurringInstanceUseCase(repo);

        await sut.ExecuteAsync("r-1", new System.DateTime(2026,3,1), new System.DateTime(2026,3,6), "updated");

        await repo.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.Exceptions.Count == 1 && r.Exceptions[0].ShiftToDate == new System.DateTime(2026,3,6) && r.Exceptions[0].Note == "updated"));
    }
}
