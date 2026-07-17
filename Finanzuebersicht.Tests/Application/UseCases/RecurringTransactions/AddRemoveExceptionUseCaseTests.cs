using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Models;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Finanzuebersicht.Tests.Application.UseCases.RecurringTransactions;

public class AddRemoveExceptionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AddsException_WhenNoneExists()
    {
        var recurring = new RecurringTransaction { Id = "r-1", Exceptions = new List<RecurringException>() };
        var repo = Substitute.For<IRecurringTransactionRepository>();
        repo.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var sut = new AddRecurringExceptionUseCase(repo);
        var ex = new RecurringException { Id = "e-1", InstanceDate = new System.DateTime(2026,3,1), Type = RecurringExceptionType.Skip };

        await sut.ExecuteAsync("r-1", ex);

        await repo.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.Exceptions.Count == 1 && r.Exceptions[0].Id == "e-1"));
    }

    [Fact]
    public async Task ExecuteAsync_ReplacesException_WhenSameDateExists()
    {
        var existingEx = new RecurringException { Id = "e-old", InstanceDate = new System.DateTime(2026,3,1), Type = RecurringExceptionType.Skip };
        var recurring = new RecurringTransaction { Id = "r-1", Exceptions = new List<RecurringException> { existingEx } };
        var repo = Substitute.For<IRecurringTransactionRepository>();
        repo.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var sut = new AddRecurringExceptionUseCase(repo);
        var newEx = new RecurringException { Id = "e-new", InstanceDate = new System.DateTime(2026,3,1), Type = RecurringExceptionType.Shift, ShiftToDate = new System.DateTime(2026,3,2) };

        await sut.ExecuteAsync("r-1", newEx);

        await repo.Received(1).SaveRecurringTransactionAsync(NonNullArg.Is<RecurringTransaction>(r => r.Exceptions.Count == 1 && r.Exceptions[0].Id == "e-new" && r.Exceptions[0].Type == RecurringExceptionType.Shift && r.Exceptions[0].ShiftToDate == new System.DateTime(2026,3,2)));
    }
}
