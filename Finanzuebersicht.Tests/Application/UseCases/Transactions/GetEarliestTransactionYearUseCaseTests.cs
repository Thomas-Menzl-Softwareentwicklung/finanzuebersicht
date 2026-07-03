using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.Transactions;

public class GetEarliestTransactionYearUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsMinimumYear_WhenTransactionsExist()
    {
        var repository = Substitute.For<ITransactionRepository>();
        repository.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue).Returns(
        [
            new Transaction { Datum = new DateTime(2024, 6, 1) },
            new Transaction { Datum = new DateTime(2022, 1, 15) },
            new Transaction { Datum = new DateTime(2026, 3, 1) }
        ]);

        var sut = new GetEarliestTransactionYearUseCase(repository);

        var result = await sut.ExecuteAsync();

        Assert.Equal(2022, result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNull_WhenNoTransactionsExist()
    {
        var repository = Substitute.For<ITransactionRepository>();
        repository.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue).Returns([]);

        var sut = new GetEarliestTransactionYearUseCase(repository);

        var result = await sut.ExecuteAsync();

        Assert.Null(result);
    }
}
