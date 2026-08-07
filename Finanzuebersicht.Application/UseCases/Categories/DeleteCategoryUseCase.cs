using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Categories;

public class DeleteCategoryUseCase(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringTransactionRepository)
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;

    public async Task ExecuteAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetCategoriesAsync();
        if (!categories.Any(c => c.Id == categoryId))
            return;

        var fallbackCategory = categories
            .FirstOrDefault(c => c.SystemKey == Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges && c.Id != categoryId)
            ?? categories.FirstOrDefault(c => c.Id != categoryId);

        if (fallbackCategory == null)
        {
            fallbackCategory = new Category
            {
                Name = "Sonstiges",
                Icon = "📦",
                Color = "#A2845E",
                Typ = TransactionType.Ausgabe,
                SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges
            };

            await _categoryRepository.SaveCategoryAsync(fallbackCategory);
        }

        await _transactionRepository.RemapCategoryIdAsync(categoryId, fallbackCategory.Id, cancellationToken);

        var recurringTransactions = await _recurringTransactionRepository.GetRecurringTransactionsAsync();
        foreach (var recurring in recurringTransactions.Where(r => r.KategorieId == categoryId))
        {
            recurring.KategorieId = fallbackCategory.Id;
            await _recurringTransactionRepository.SaveRecurringTransactionAsync(recurring);
        }

        await _categoryRepository.DeleteCategoryAsync(categoryId);
    }
}