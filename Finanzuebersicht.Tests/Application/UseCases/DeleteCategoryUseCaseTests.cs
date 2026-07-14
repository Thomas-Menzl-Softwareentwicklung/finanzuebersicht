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
        transactionRepository.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue).Returns(new List<Transaction>
        {
            new() { Id = "tx-1", KategorieId = "cat-delete" },
            new() { Id = "tx-2", KategorieId = "cat-other" }
        });
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>
        {
            new() { Id = "r-1", KategorieId = "cat-delete" },
            new() { Id = "r-2", KategorieId = "cat-other" }
        });

        var sut = new DeleteCategoryUseCase(categoryRepository, transactionRepository, recurringRepository);

        await sut.ExecuteAsync("cat-delete");

        await transactionRepository.Received(1).SaveTransactionAsync(
            Arg.Is<Transaction>(t => t != null && t.Id == "tx-1" && t.KategorieId == "cat-default"));
        await recurringRepository.Received(1).SaveRecurringTransactionAsync(
            Arg.Is<RecurringTransaction>(r => r != null && r.Id == "r-1" && r.KategorieId == "cat-default"));
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
        transactionRepository.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue).Returns(new List<Transaction>
        {
            new() { Id = "tx-1", KategorieId = "cat-delete" }
        });
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction>());

        var sut = new DeleteCategoryUseCase(categoryRepository, transactionRepository, recurringRepository);

        await sut.ExecuteAsync("cat-delete");

        await categoryRepository.Received(1).SaveCategoryAsync(
            Arg.Is<Category>(c => c != null && c.SystemKey == Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges));
        await transactionRepository.Received(1).SaveTransactionAsync(Arg.Any<Transaction>());
        await categoryRepository.Received(1).DeleteCategoryAsync("cat-delete");
    }
}