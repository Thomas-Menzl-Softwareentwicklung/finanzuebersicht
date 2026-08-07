using Finanzuebersicht.Application.UseCases.Transactions;

namespace Finanzuebersicht.Tests.Application.UseCases.Transactions;

public class GetEarliestTransactionYearUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsMinimumYear_WhenTransactionsExist()
    {
        var repository = Substitute.For<ITransactionRepository>();
        repository.GetEarliestTransactionYearAsync(Arg.Any<CancellationToken>()).Returns(2022);

        var sut = new GetEarliestTransactionYearUseCase(repository);

        var result = await sut.ExecuteAsync();

        Assert.Equal(2022, result);
        await repository.Received(1).GetEarliestTransactionYearAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNull_WhenNoTransactionsExist()
    {
        var repository = Substitute.For<ITransactionRepository>();
        repository.GetEarliestTransactionYearAsync(Arg.Any<CancellationToken>()).Returns((int?)null);

        var sut = new GetEarliestTransactionYearUseCase(repository);

        var result = await sut.ExecuteAsync();

        Assert.Null(result);
    }
}
