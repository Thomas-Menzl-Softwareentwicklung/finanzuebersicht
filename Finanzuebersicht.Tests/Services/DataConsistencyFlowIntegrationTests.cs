using Xunit;
using Finanzuebersicht.Models;
using Finanzuebersicht.Application.UseCases.Categories;

namespace Finanzuebersicht.Tests.Services
{
    /// <summary>
    /// Integration tests for critical data consistency flows.
    /// Tests exercise production code paths through actual UseCases and Services.
    /// </summary>
    public class DataConsistencyFlowIntegrationTests
    {
        [Fact]
        public async Task DeleteCategoryUseCase_WithTransactions_ReassignsToFallback()
        {
            // Arrange
            var dataService = new InMemoryFinanceStore();
            
            var groceriesCategory = new Category
            {
                Id = "cat-groceries",
                Name = "Groceries",
                Icon = "🛒",
                Color = "#34C759",
                Typ = TransactionType.Ausgabe
            };
            var sonstiges = new Category
            {
                Id = "cat-sonstiges",
                Name = "Sonstiges",
                Icon = "📦",
                Color = "#A2845E",
                Typ = TransactionType.Ausgabe,
                SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges
            };

            await dataService.SaveCategoryAsync(groceriesCategory);
            await dataService.SaveCategoryAsync(sonstiges);

            var txn1 = new Transaction
            {
                Id = "txn1",
                Titel = "Supermarket",
                Betrag = 50m,
                Datum = DateTime.Today,
                KategorieId = "cat-groceries",
                Typ = TransactionType.Ausgabe
            };
            var txn2 = new Transaction
            {
                Id = "txn2",
                Titel = "Farm Market",
                Betrag = 30m,
                Datum = DateTime.Today,
                KategorieId = "cat-groceries",
                Typ = TransactionType.Ausgabe
            };

            await dataService.SaveTransactionAsync(txn1);
            await dataService.SaveTransactionAsync(txn2);

            var useCase = new DeleteCategoryUseCase(dataService, dataService, dataService);

            // Act
            await useCase.ExecuteAsync("cat-groceries");

            // Assert - verify transactions were reassigned to fallback
            var allTransactions = await dataService.GetTransactionsAsync(
                DateTime.Today.AddMonths(-12),
                DateTime.Today.AddDays(1));

            var txn1After = allTransactions.FirstOrDefault(t => t.Id == "txn1");
            var txn2After = allTransactions.FirstOrDefault(t => t.Id == "txn2");

            Assert.NotNull(txn1After);
            Assert.NotNull(txn2After);
            Assert.Equal("cat-sonstiges", txn1After.KategorieId);
            Assert.Equal("cat-sonstiges", txn2After.KategorieId);

            // Verify category was deleted
            var categories = await dataService.GetCategoriesAsync();
            var deletedCat = categories.FirstOrDefault(c => c.Id == "cat-groceries");
            Assert.Null(deletedCat);
        }

        [Fact]
        public async Task DeleteCategoryUseCase_WithRecurringTransactions_ReassignsToFallback()
        {
            // Arrange
            var dataService = new InMemoryFinanceStore();
            
            var utilitiesCategory = new Category
            {
                Id = "cat-utilities",
                Name = "Utilities",
                Icon = "⚡",
                Color = "#FF9500",
                Typ = TransactionType.Ausgabe
            };
            var sonstiges = new Category
            {
                Id = "cat-sonstiges",
                Name = "Sonstiges",
                Icon = "📦",
                Color = "#A2845E",
                Typ = TransactionType.Ausgabe,
                SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges
            };

            await dataService.SaveCategoryAsync(utilitiesCategory);
            await dataService.SaveCategoryAsync(sonstiges);

            var recurring = new RecurringTransaction
            {
                Id = "recurring1",
                Titel = "Monthly Bill",
                Betrag = 100m,
                KategorieId = "cat-utilities",
                Typ = TransactionType.Ausgabe,
                Startdatum = DateTime.Today.AddMonths(-3),
                Aktiv = true
            };

            await dataService.SaveRecurringTransactionAsync(recurring);

            var useCase = new DeleteCategoryUseCase(dataService, dataService, dataService);

            // Act
            await useCase.ExecuteAsync("cat-utilities");

            // Assert - verify recurring was reassigned to fallback
            var recurringAfter = await dataService.GetRecurringTransactionsAsync();
            var recurringAfterDelete = recurringAfter.FirstOrDefault(r => r.Id == "recurring1");

            Assert.NotNull(recurringAfterDelete);
            Assert.Equal("cat-sonstiges", recurringAfterDelete.KategorieId);

            var categories = await dataService.GetCategoriesAsync();
            var deletedCat = categories.FirstOrDefault(c => c.Id == "cat-utilities");
            Assert.Null(deletedCat);
        }

        [Fact]
        public async Task DeleteCategoryUseCase_WithoutFallback_CreatesSystemFallback()
        {
            // Arrange - only have the category to delete, no fallback exists
            var dataService = new InMemoryFinanceStore();
            
            var onlyCategory = new Category
            {
                Id = "cat-only",
                Name = "OnlyOne",
                Icon = "🏠",
                Color = "#000000",
                Typ = TransactionType.Ausgabe
            };

            await dataService.SaveCategoryAsync(onlyCategory);

            var txn = new Transaction
            {
                Id = "txn-orphan",
                Titel = "Test",
                Betrag = 100m,
                Datum = DateTime.Today,
                KategorieId = "cat-only",
                Typ = TransactionType.Ausgabe
            };

            await dataService.SaveTransactionAsync(txn);

            var useCase = new DeleteCategoryUseCase(dataService, dataService, dataService);

            // Act - UseCase should create fallback if needed
            await useCase.ExecuteAsync("cat-only");

            // Assert - fallback was created
            var categories = await dataService.GetCategoriesAsync();
            var fallback = categories.FirstOrDefault(c => c.SystemKey == Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges);
            
            Assert.NotNull(fallback);
            
            var txnAfter = (await dataService.GetTransactionsAsync(
                DateTime.Today.AddMonths(-12),
                DateTime.Today.AddDays(1)))
                .FirstOrDefault(t => t.Id == "txn-orphan");
            
            Assert.NotNull(txnAfter);
            Assert.Equal(fallback.Id, txnAfter.KategorieId);
        }

        [Fact]
        public async Task DeleteCategoryUseCase_MixedData_HandlesBothTransactionsAndRecurring()
        {
            // Arrange - category used by both transactions and recurring
            var dataService = new InMemoryFinanceStore();
            
            var entertainmentCategory = new Category
            {
                Id = "cat-entertainment",
                Name = "Entertainment",
                Icon = "🎬",
                Color = "#AF52DE",
                Typ = TransactionType.Ausgabe
            };
            var sonstiges = new Category
            {
                Id = "cat-sonstiges",
                Name = "Sonstiges",
                Icon = "📦",
                Color = "#A2845E",
                Typ = TransactionType.Ausgabe,
                SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges
            };

            await dataService.SaveCategoryAsync(entertainmentCategory);
            await dataService.SaveCategoryAsync(sonstiges);

            // Add transaction
            var txn = new Transaction
            {
                Id = "txn-movie",
                Titel = "Movie",
                Betrag = 25m,
                Datum = DateTime.Today,
                KategorieId = "cat-entertainment",
                Typ = TransactionType.Ausgabe
            };
            await dataService.SaveTransactionAsync(txn);

            // Add recurring
            var recurring = new RecurringTransaction
            {
                Id = "rec-cinema",
                Titel = "Monthly Cinema",
                Betrag = 40m,
                KategorieId = "cat-entertainment",
                Typ = TransactionType.Ausgabe,
                Startdatum = DateTime.Today.AddMonths(-1),
                Aktiv = true
            };
            await dataService.SaveRecurringTransactionAsync(recurring);

            var useCase = new DeleteCategoryUseCase(dataService, dataService, dataService);

            // Act
            await useCase.ExecuteAsync("cat-entertainment");

            // Assert - both should be reassigned
            var txnAfter = (await dataService.GetTransactionsAsync(
                DateTime.Today.AddMonths(-12),
                DateTime.Today.AddDays(1)))
                .FirstOrDefault(t => t.Id == "txn-movie");
            
            var recAfter = (await dataService.GetRecurringTransactionsAsync())
                .FirstOrDefault(r => r.Id == "rec-cinema");

            Assert.NotNull(txnAfter);
            Assert.NotNull(recAfter);
            Assert.Equal("cat-sonstiges", txnAfter.KategorieId);
            Assert.Equal("cat-sonstiges", recAfter.KategorieId);
        }

    }
}
