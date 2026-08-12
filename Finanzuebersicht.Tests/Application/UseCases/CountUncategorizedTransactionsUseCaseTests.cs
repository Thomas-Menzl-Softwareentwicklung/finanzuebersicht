using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class CountUncategorizedTransactionsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsZero_WhenCategoryMissing()
    {
        var transactions = Substitute.For<ITransactionRepository>();
        var uncategorized = Substitute.For<IUncategorizedCategoryService>();
        uncategorized.FindIdAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        var sut = new CountUncategorizedTransactionsUseCase(transactions, uncategorized);
        Assert.Equal(0, await sut.ExecuteAsync());
        await transactions.DidNotReceive().GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ExecuteAsync_CountsNonTransferMatches()
    {
        var transactions = Substitute.For<ITransactionRepository>();
        transactions.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(
        [
            new Transaction { KategorieId = "uncat", IsTransfer = false },
            new Transaction { KategorieId = "uncat", IsTransfer = true },
            new Transaction { KategorieId = "other", IsTransfer = false },
            new Transaction { KategorieId = "uncat", IsTransfer = false }
        ]);
        var uncategorized = Substitute.For<IUncategorizedCategoryService>();
        uncategorized.FindIdAsync(Arg.Any<CancellationToken>()).Returns("uncat");

        var sut = new CountUncategorizedTransactionsUseCase(transactions, uncategorized);
        Assert.Equal(2, await sut.ExecuteAsync());
    }
}
