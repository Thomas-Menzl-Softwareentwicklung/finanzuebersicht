using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class DeleteCategoryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_RemapsTransactionsAndRecurringToFallbackBeforeDelete()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        categoryRepository.GetCategoriesAsync().Returns(new List<Category>
        {
            new() { Id = "cat-delete", Name = "Zu löschen" },
            new() { Id = "cat-default", Name = "Sonstiges", SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges }
        });
        transactionRepository.RemapCategoryIdAsync("cat-delete", "cat-default", Arg.Any<CancellationToken>())
            .Returns(1);
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>
        {
            new() { Id = "r-1", KategorieId = "cat-delete" },
            new() { Id = "r-2", KategorieId = "cat-other" }
        });

        var sut = new DeleteCategoryUseCase(categoryRepository, transactionRepository, recurringRepository);

        await sut.ExecuteAsync("cat-delete");

        await transactionRepository.Received(1).RemapCategoryIdAsync("cat-delete", "cat-default", Arg.Any<CancellationToken>());
        await recurringRepository.Received(1).SaveRecurringTransactionAsync(
            NonNullArg.Is<RecurringTransaction>(r => r.Id == "r-1" && r.KategorieId == "cat-default"));
        await categoryRepository.Received(1).DeleteCategoryAsync("cat-delete");
    }

    [Fact]
    public async Task ExecuteAsync_CreatesFallback_WhenNoOtherCategoryExists()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        categoryRepository.GetCategoriesAsync().Returns(new List<Category>
        {
            new() { Id = "cat-delete", Name = "Zu löschen" }
        });
        transactionRepository.RemapCategoryIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1);
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>());

        var sut = new DeleteCategoryUseCase(categoryRepository, transactionRepository, recurringRepository);

        await sut.ExecuteAsync("cat-delete");

        await categoryRepository.Received(1).SaveCategoryAsync(
            NonNullArg.Is<Category>(c => c.SystemKey == Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges));
        await transactionRepository.Received(1).RemapCategoryIdAsync(
            "cat-delete",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await categoryRepository.Received(1).DeleteCategoryAsync("cat-delete");
    }
}
