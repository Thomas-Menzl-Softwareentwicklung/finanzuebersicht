using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class LoadTransactionsMonthUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_GroupsAndSortsTransactionsByDayDescending()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();

        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { Id = "t1", Datum = new DateTime(2026, 3, 5, 10, 0, 0), KategorieId = "c1" },
                new() { Id = "t2", Datum = new DateTime(2026, 3, 6, 8, 0, 0), KategorieId = "c2" },
                new() { Id = "t3", Datum = new DateTime(2026, 3, 6, 12, 0, 0), KategorieId = "c1" }
            });

        categoryRepository.GetCategoriesAsync().Returns(new List<Category>
        {
            new() { Id = "c1", Icon = "🍔" },
            new() { Id = "c2", Icon = "🚗" }
        });
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "a1", Name = "Giro" },
            new() { Id = "a2", Name = "Tagesgeld" }
        });

        var useCase = new LoadTransactionsMonthUseCase(transactionRepository, categoryRepository, accountRepository);

        var result = await useCase.ExecuteAsync(new DateTime(2026, 3, 1));

        Assert.Equal(2, result.Gruppen.Count);
        Assert.Equal(new DateTime(2026, 3, 6), result.Gruppen[0].Datum);
        Assert.Equal(2, result.Gruppen[0].Count);
        Assert.Equal("t3", result.Gruppen[0][0].Id);
        Assert.Equal("t2", result.Gruppen[0][1].Id);
        Assert.Equal(new DateTime(2026, 3, 5), result.Gruppen[1].Datum);
    }

    [Fact]
    public async Task ExecuteAsync_BuildsIconMapWithFolderFallback()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();

        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>());

        categoryRepository.GetCategoriesAsync().Returns(new List<Category>
        {
            new() { Id = "c1", Icon = "💼" },
            new() { Id = "c2", Icon = null! }
        });
        accountRepository.GetAccountsAsync().Returns(new List<Account>());

        var useCase = new LoadTransactionsMonthUseCase(transactionRepository, categoryRepository, accountRepository);

        var result = await useCase.ExecuteAsync(new DateTime(2026, 3, 1));

        Assert.Equal("💼", result.IconMap["c1"]);
        Assert.Equal("📁", result.IconMap["c2"]);
        Assert.Empty(result.AccountMap);
    }

    [Fact]
    public async Task ExecuteAsync_BuildsColorMapFromCategories()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();

        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>());

        categoryRepository.GetCategoriesAsync().Returns(new List<Category>
        {
            new() { Id = "c1", Icon = "🍔", Color = "#34C759" },
            new() { Id = "c2", Icon = "🚗", Color = "#FF9500" }
        });
        accountRepository.GetAccountsAsync().Returns(new List<Account>());

        var useCase = new LoadTransactionsMonthUseCase(transactionRepository, categoryRepository, accountRepository);
        var result = await useCase.ExecuteAsync(new DateTime(2026, 3, 1));

        Assert.Equal("#34C759", result.ColorMap["c1"]);
        Assert.Equal("#FF9500", result.ColorMap["c2"]);
    }
}