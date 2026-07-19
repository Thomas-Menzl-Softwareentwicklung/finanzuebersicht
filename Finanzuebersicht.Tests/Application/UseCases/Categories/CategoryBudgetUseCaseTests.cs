using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.Categories;

public class LoadCategoryBudgetUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsZero_WhenCategoryIdEmpty()
    {
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var sut = new LoadCategoryBudgetUseCase(budgetRepository);

        var result = await sut.ExecuteAsync(" ");

        Assert.Equal(0, result);
        await budgetRepository.DidNotReceive().GetBudgetsAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsDefaultBudgetAmount()
    {
        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns(
        [
            new CategoryBudget { Id = "b1", KategorieId = "cat-1", Betrag = 150m, Monat = null, Jahr = null },
            new CategoryBudget { Id = "b2", KategorieId = "cat-1", Betrag = 99m, Monat = 6, Jahr = 2025 }
        ]);
        var sut = new LoadCategoryBudgetUseCase(budgetRepository);

        var result = await sut.ExecuteAsync("cat-1");

        Assert.Equal(150m, result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsZero_WhenNoDefaultBudget()
    {
        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns(
        [
            new CategoryBudget { Id = "b2", KategorieId = "cat-1", Betrag = 99m, Monat = 6, Jahr = 2025 }
        ]);
        var sut = new LoadCategoryBudgetUseCase(budgetRepository);

        var result = await sut.ExecuteAsync("cat-1");

        Assert.Equal(0, result);
    }
}

public class SaveCategoryBudgetUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesBudget_WhenAmountPositiveAndMissing()
    {
        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns([]);
        var sut = new SaveCategoryBudgetUseCase(budgetRepository);

        await sut.ExecuteAsync("cat-1", 120m);

        await budgetRepository.Received(1).SaveBudgetAsync(Arg.Is<CategoryBudget>(b =>
            b != null && b.KategorieId == "cat-1" && b.Betrag == 120m && b.Monat == null && b.Jahr == null));
    }

    [Fact]
    public async Task ExecuteAsync_DeletesBudget_WhenAmountZeroAndExists()
    {
        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns(
        [
            new CategoryBudget { Id = "b1", KategorieId = "cat-1", Betrag = 50m }
        ]);
        var sut = new SaveCategoryBudgetUseCase(budgetRepository);

        await sut.ExecuteAsync("cat-1", 0m);

        await budgetRepository.Received(1).DeleteBudgetAsync("b1");
        await budgetRepository.DidNotReceive().SaveBudgetAsync(Arg.Any<CategoryBudget>());
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesExistingBudget()
    {
        var existing = new CategoryBudget { Id = "b1", KategorieId = "cat-1", Betrag = 50m };
        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns([existing]);
        var sut = new SaveCategoryBudgetUseCase(budgetRepository);

        await sut.ExecuteAsync("cat-1", 80m);

        Assert.Equal(80m, existing.Betrag);
        await budgetRepository.Received(1).SaveBudgetAsync(existing);
    }
}
