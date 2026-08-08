using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class GetEntityByIdUseCaseTests
{
    [Fact]
    public async Task GetTransactionById_ReturnsMatchingTransaction()
    {
        var repository = Substitute.For<ITransactionRepository>();
        repository.GetAllTransactionsAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new Transaction { Id = "tx-1", Titel = "A" },
                new Transaction { Id = "tx-2", Titel = "B" }
            ]);

        var result = await new GetTransactionByIdUseCase(repository).ExecuteAsync("tx-2");

        Assert.NotNull(result);
        Assert.Equal("B", result!.Titel);
    }

    [Fact]
    public async Task GetAccountById_ReturnsMatchingAccount()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc-1", Name = "Giro" },
            new Account { Id = "acc-2", Name = "Spar" }
        ]);

        var result = await new GetAccountByIdUseCase(repository).ExecuteAsync("acc-1");

        Assert.NotNull(result);
        Assert.Equal("Giro", result!.Name);
    }

    [Fact]
    public async Task GetCategoryById_ReturnsMatchingCategory()
    {
        var repository = Substitute.For<ICategoryRepository>();
        repository.GetCategoriesAsync().Returns(
        [
            new Category { Id = "cat-1", Name = "Essen" },
            new Category { Id = "cat-2", Name = "Miete" }
        ]);

        var result = await new GetCategoryByIdUseCase(repository).ExecuteAsync("cat-2");

        Assert.NotNull(result);
        Assert.Equal("Miete", result!.Name);
    }
}
