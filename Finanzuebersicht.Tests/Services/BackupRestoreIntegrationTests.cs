using Xunit;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Models;
using System.IO.Compression;
using System.Text.Json;

namespace Finanzuebersicht.Tests.Services
{
    /// <summary>
    /// Integration tests for complete backup/restore cycles.
    /// Tests end-to-end scenarios with multiple backup operations, restores, and data validation.
    /// </summary>
    public class BackupRestoreIntegrationTests : IDisposable
    {
        private readonly string _testDir;
        private readonly MockDataService _mockDataService;
        private readonly MockSettingsService _mockSettingsService;

        public BackupRestoreIntegrationTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"integration_tests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
            _mockDataService = new MockDataService();
            _mockSettingsService = new MockSettingsService(_testDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                    Directory.Delete(_testDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public async Task FullBackupRestoreCycle_PreserveAllData()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]), _mockDataService);
            var backupPath = Path.Combine(_testDir, "backups");

            // Setup data
            var originalCategories = new[]
            {
                new Category { Id = "c1", Name = "Groceries", Icon = "🛒", Color = "#FF5733" },
                new Category { Id = "c2", Name = "Transport", Icon = "🚗", Color = "#33FF57" }
            };
            var originalAccounts = new[]
            {
                new Account { Id = "a1", Name = "Girokonto", Type = AccountType.Girokonto }
            };

            var originalTransactions = new[]
            {
                new Transaction { Id = "t1", Titel = "Supermarket", Betrag = 50.5m, Datum = new DateTime(2026, 1, 1), KategorieId = "c1", AccountId = "a1" },
                new Transaction { Id = "t2", Titel = "Gas", Betrag = 60.0m, Datum = new DateTime(2026, 1, 5), KategorieId = "c2", AccountId = "a1" }
            };

            var originalRecurring = new[]
            {
                new RecurringTransaction
                {
                    Id = "r1",
                    Titel = "Rent",
                    Betrag = 1000m,
                    Typ = TransactionType.Ausgabe,
                    KategorieId = "c1",
                    Startdatum = new DateTime(2026, 1, 1),
                    Enddatum = null,
                    Aktiv = true
                }
            };

            _mockDataService.SetCategories(originalCategories);
            _mockDataService.SetAccounts(originalAccounts);
            _mockDataService.SetTransactions(originalTransactions);
            _mockDataService.SetRecurring(originalRecurring);
            _mockDataService.SetBudgets([new CategoryBudget { Id = "b1", KategorieId = "c1", Betrag = 300m }]);
            _mockDataService.SetSparZiele([new SparZiel { Id = "s1", Titel = "Urlaub", ZielBetrag = 2000m }]);
            _mockDataService.SetTransactionTemplates([new TransactionTemplate
            {
                Id = "tpl1",
                Name = "Wocheneinkauf",
                Titel = "Supermarket",
                Betrag = 50.5m,
                KategorieId = "c1",
                Typ = TransactionType.Ausgabe,
                UseCount = 3
            }]);

            // Act 1: Create backup
            var backup = await service.CreateBackupAsync(backupPath);

            // Assert 1: Backup created with correct data
            Assert.NotNull(backup);
            Assert.Equal(2, backup.EntityCounts[BackupEntityKeys.Categories]);
            Assert.Equal(1, backup.EntityCounts[BackupEntityKeys.Accounts]);
            Assert.Equal(2, backup.EntityCounts[BackupEntityKeys.Transactions]);
            Assert.Equal(1, backup.EntityCounts[BackupEntityKeys.Recurring]);
            Assert.Equal(1, backup.EntityCounts[BackupEntityKeys.Budgets]);
            Assert.Equal(1, backup.EntityCounts[BackupEntityKeys.Sparziele]);
            Assert.Equal(1, backup.EntityCounts[BackupEntityKeys.TransactionTemplates]);
            Assert.True(File.Exists(Path.Combine(backupPath, backup.FileName)));

            // Act 2: Restore backup — clear state first to verify data is re-saved
            _mockDataService.SetCategories([]);
            _mockDataService.SetTransactions([]);
            _mockDataService.SetRecurring([]);
            _mockDataService.SetBudgets([]);
            _mockDataService.SetSparZiele([]);
            _mockDataService.SetTransactionTemplates([]);

            var restoreResult = await service.RestoreBackupAsync(backupPath, backup.Id);

            // Assert 2: Restore successful and data actually written back
            Assert.True(restoreResult.Success, restoreResult.ErrorMessage);
            Assert.NotNull(restoreResult.RestoredMetadata);
            Assert.Equal(backup.Id, restoreResult.RestoredMetadata.Id);

            var restoredCategories = await _mockDataService.GetCategoriesAsync();
            var restoredAccounts = await _mockDataService.GetAccountsAsync();
            var restoredTransactions = await _mockDataService.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue);
            var restoredRecurring = await _mockDataService.GetRecurringTransactionsAsync();
            var restoredBudgets = await _mockDataService.GetBudgetsAsync();
            var restoredSparziele = await _mockDataService.GetSparZieleAsync();
            var restoredTemplates = await _mockDataService.GetTransactionTemplatesAsync();

            Assert.Equal(2, restoredCategories.Count);
            Assert.Single(restoredAccounts);
            Assert.Equal(2, restoredTransactions.Count);
            Assert.Single(restoredRecurring);
            Assert.Single(restoredBudgets);
            Assert.Single(restoredSparziele);
            Assert.Single(restoredTemplates);
            Assert.Contains(restoredCategories, c => c.Id == "c1" && c.Name == "Groceries");
            Assert.Contains(restoredAccounts, a => a.Id == "a1" && a.Name == "Girokonto");
            Assert.Contains(restoredTransactions, t => t.Id == "t1" && t.Betrag == 50.5m);
            Assert.Contains(restoredRecurring, r => r.Id == "r1" && r.Titel == "Rent");
            Assert.Contains(restoredBudgets, b => b.Id == "b1" && b.Betrag == 300m);
            Assert.Contains(restoredSparziele, s => s.Id == "s1" && s.Titel == "Urlaub");
            Assert.Contains(restoredTemplates, t => t.Id == "tpl1" && t.Name == "Wocheneinkauf" && t.UseCount == 3);
        }

        [Fact]
        public async Task MultipleBackups_ListsInCorrectOrder()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var backupPath = Path.Combine(_testDir, "backups");
            var categories = new[] { new Category { Id = "1", Name = "Test", Icon = "🏠", Color = "#000" } };
            _mockDataService.SetCategories(categories);

            // Act: Create multiple backups with delays
            var backup1 = await service.CreateBackupAsync(backupPath);
            await Task.Delay(50);
            var backup2 = await service.CreateBackupAsync(backupPath);
            await Task.Delay(50);
            var backup3 = await service.CreateBackupAsync(backupPath);

            var backups = (await service.ListBackupsAsync(backupPath)).ToList();

            // Assert: Newest first
            Assert.Equal(3, backups.Count);
            Assert.True(backups[0].CreatedAt >= backups[1].CreatedAt);
            Assert.True(backups[1].CreatedAt >= backups[2].CreatedAt);
        }

        [Fact]
        public async Task BackupWithEmptyDatabase_CreatesValidBackup()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var backupPath = Path.Combine(_testDir, "backups");
            // No data set

            // Act
            var backup = await service.CreateBackupAsync(backupPath);

            // Assert
            Assert.NotNull(backup);
            Assert.Equal(0, backup.EntityCounts[BackupEntityKeys.Categories]);
            Assert.Equal(0, backup.EntityCounts[BackupEntityKeys.Transactions]);
            Assert.Equal(0, backup.EntityCounts[BackupEntityKeys.Recurring]);
        }

        [Fact]
        public async Task DeleteBackup_RemovesBackupFile()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var backupPath = Path.Combine(_testDir, "backups");
            var category = new Category { Id = "1", Name = "Test", Icon = "🏠", Color = "#000" };
            _mockDataService.SetCategories(new[] { category });

            var backup = await service.CreateBackupAsync(backupPath);
            var backupFile = Path.Combine(backupPath, backup.FileName);
            Assert.True(File.Exists(backupFile));

            // Act
            await service.DeleteBackupAsync(backupPath, backup.Id);

            // Assert
            Assert.False(File.Exists(backupFile));
        }

        [Fact]
        public async Task ExportAsCSV_ContainsAllTransactionData()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var categories = new[]
            {
                new Category { Id = "c1", Name = "Food", Icon = "🍕", Color = "#FF0000" }
            };
            var transactions = new[]
            {
                new Transaction
                {
                    Id = "t1",
                    Titel = "Pizza",
                    Betrag = 25.50m,
                    Datum = new DateTime(2026, 3, 10),
                    KategorieId = "c1",
                    Typ = TransactionType.Ausgabe,
                    Verwendungszweck = "Dinner with \"quotes\""
                }
            };

            _mockDataService.SetCategories(categories);
            _mockDataService.SetTransactions(transactions);

            // Act
            var csvStream = await service.ExportAsCSVAsync();
            using var reader = new StreamReader(csvStream);
            var csv = await reader.ReadToEndAsync();

            // Assert: CSV contains header and transaction
            Assert.Contains("Datum,Titel,Betrag,Typ,Kategorie,Verwendungszweck", csv);
            Assert.Contains("2026-03-10", csv);
            Assert.Contains("Pizza", csv);
            Assert.Contains("Food", csv);
            // Betrag kann "25.5" oder "25,5" sein je nach Kultur
            Assert.True(csv.Contains("25.5") || csv.Contains("25,5"), "CSV should contain the amount in either US or DE format");
        }

        [Fact]
        public async Task BackupSettings_PersistsBackupMetadata()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var backupPath = Path.Combine(_testDir, "backups");
            _mockDataService.SetCategories(new[] { new Category { Id = "1", Name = "Test", Icon = "🏠", Color = "#000" } });

            // Act
            var backup1 = await service.CreateBackupAsync(backupPath);
            var lastBackupTime1 = _mockSettingsService.Get(SettingsKeys.LastBackupTime);

            await Task.Delay(100);

            var backup2 = await service.CreateBackupAsync(backupPath);
            var lastBackupTime2 = _mockSettingsService.Get(SettingsKeys.LastBackupTime);

            // Assert
            Assert.NotNull(lastBackupTime1);
            Assert.NotNull(lastBackupTime2);
            Assert.NotEqual(lastBackupTime1, lastBackupTime2);
        }

        [Fact]
        public async Task RestoreNonexistentBackup_ReturnsFailed()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var backupPath = Path.Combine(_testDir, "backups");

            // Act
            var result = await service.RestoreBackupAsync(backupPath, "nonexistent-backup-id");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("nicht gefunden", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RestoreBackup_BackupFileCorrupted_OriginalDataPreserved()
        {
            // Arrange: create backup, then corrupt the ZIP before restoring.
            // The restore fails at extraction (before any ReplaceAll* write occurs),
            // so the existing data must remain untouched.
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var backupPath = Path.Combine(_testDir, "backups");

            var originalCategories = new[] { new Category { Id = "c1", Name = "Groceries" } };
            var originalTransactions = new[] { new Transaction { Id = "t1", Titel = "Shop", Betrag = 10m, Datum = DateTime.Today } };
            _mockDataService.SetCategories(originalCategories);
            _mockDataService.SetTransactions(originalTransactions);

            var metadata = await service.CreateBackupAsync(backupPath);

            // Change the data to something different
            _mockDataService.SetCategories([new Category { Id = "c2", Name = "New" }]);
            _mockDataService.SetTransactions([new Transaction { Id = "t2", Titel = "New", Betrag = 99m, Datum = DateTime.Today }]);

            // Break the ZIP so restore fails mid-write by corrupting the zip file
            var backupFile = Directory.GetFiles(backupPath, "*.zip").First();
            await File.WriteAllTextAsync(backupFile, "NOT A ZIP FILE");

            // Act
            var result = await service.RestoreBackupAsync(backupPath, metadata.Id);

            // Assert: restore failed, rollback succeeded, data is still "c2/t2" (the state before restore attempt)
            Assert.False(result.Success);
            Assert.False(result.DataMayBeInconsistent);

            var cats = await _mockDataService.GetCategoriesAsync();
            Assert.Single(cats);
            Assert.Equal("c2", cats[0].Id);
        }

        [Fact]
        public async Task RestoreBackup_WriteFailsAndRollbackFails_DataMayBeInconsistentIsTrue()
        {
            // Arrange: create a valid backup
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var backupPath = Path.Combine(_testDir, "backups");

            _mockDataService.SetCategories([new Category { Id = "c1", Name = "Original" }]);
            var metadata = await service.CreateBackupAsync(backupPath);

            // Use a data service that fails on both write AND rollback
            var failingDataService = new FailingMockDataService();
            var failingService = new BackupService(failingDataService, failingDataService, failingDataService, failingDataService, failingDataService, failingDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));

            // Act
            var result = await failingService.RestoreBackupAsync(backupPath, metadata.Id);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.DataMayBeInconsistent);
        }

        // Mock implementations
#pragma warning disable CS0618
        private class MockDataService : IDataService, IAccountRepository, ITransactionTemplateRepository
        {
            private List<Category> _categories = [];
            private List<Account> _accounts = [];
            private List<Transaction> _transactions = [];
            private List<RecurringTransaction> _recurring = [];
            private List<CategoryBudget> _budgets = [];
            private List<SparZiel> _sparziele = [];
            private List<TransactionTemplate> _transactionTemplates = [];

            public void SetCategories(IEnumerable<Category> categories) => _categories = categories.ToList();
            public void SetAccounts(IEnumerable<Account> accounts) => _accounts = accounts.ToList();
            public void SetTransactions(IEnumerable<Transaction> transactions) => _transactions = transactions.ToList();
            public void SetRecurring(IEnumerable<RecurringTransaction> recurring) => _recurring = recurring.ToList();
            public void SetBudgets(IEnumerable<CategoryBudget> budgets) => _budgets = budgets.ToList();
            public void SetSparZiele(IEnumerable<SparZiel> sparziele) => _sparziele = sparziele.ToList();
            public void SetTransactionTemplates(IEnumerable<TransactionTemplate> templates) => _transactionTemplates = templates.ToList();

            public Task<List<Category>> GetCategoriesAsync() => Task.FromResult(_categories.ToList());
            public Task<List<Account>> GetAccountsAsync() => Task.FromResult(_accounts.ToList());
            public Task SaveCategoryAsync(Category category)
            {
                var idx = _categories.FindIndex(c => c.Id == category.Id);
                if (idx >= 0) _categories[idx] = category; else _categories.Add(category);
                return Task.CompletedTask;
            }
            public Task DeleteCategoryAsync(string id) { _categories.RemoveAll(c => c.Id == id); return Task.CompletedTask; }
            public Task ReplaceAllCategoriesAsync(IEnumerable<Category> categories) { _categories = categories.ToList(); return Task.CompletedTask; }
            public Task SaveAccountAsync(Account account)
            {
                var idx = _accounts.FindIndex(a => a.Id == account.Id);
                if (idx >= 0) _accounts[idx] = account; else _accounts.Add(account);
                return Task.CompletedTask;
            }
            public Task DeleteAccountAsync(string id) { _accounts.RemoveAll(a => a.Id == id); return Task.CompletedTask; }
            public Task ReplaceAllAccountsAsync(IEnumerable<Account> accounts) { _accounts = accounts.ToList(); return Task.CompletedTask; }

            public Task<List<Transaction>> GetTransactionsAsync(DateTime vonDatum, DateTime bisDatum)
                => Task.FromResult(_transactions.Where(t => t.Datum >= vonDatum && t.Datum <= bisDatum).ToList());
            public Task SaveTransactionAsync(Transaction transaction)
            {
                var idx = _transactions.FindIndex(t => t.Id == transaction.Id);
                if (idx >= 0) _transactions[idx] = transaction; else _transactions.Add(transaction);
                return Task.CompletedTask;
            }
            public Task SaveTransactionsAsync(IEnumerable<Transaction> transactions)
            {
                foreach (var transaction in transactions)
                {
                    var idx = _transactions.FindIndex(t => t.Id == transaction.Id);
                    if (idx >= 0) _transactions[idx] = transaction; else _transactions.Add(transaction);
                }
                return Task.CompletedTask;
            }
            public Task DeleteTransactionAsync(string id) { _transactions.RemoveAll(t => t.Id == id); return Task.CompletedTask; }
            public Task DeleteTransferGroupAsync(string transferGroupId) { _transactions.RemoveAll(t => t.TransferGroupId == transferGroupId); return Task.CompletedTask; }
            public Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> transactions) { _transactions = transactions.ToList(); return Task.CompletedTask; }
            public Task<Category?> GetMostCommonCategoryForPayeeAsync(
                string payee,
                double confidenceThreshold = 0.5,
                CancellationToken cancellationToken = default)
                => Task.FromResult<Category?>(null);

            public Task<List<RecurringTransaction>> GetRecurringTransactionsAsync() => Task.FromResult(_recurring.ToList());
            public Task SaveRecurringTransactionAsync(RecurringTransaction recurring)
            {
                var idx = _recurring.FindIndex(r => r.Id == recurring.Id);
                if (idx >= 0) _recurring[idx] = recurring; else _recurring.Add(recurring);
                return Task.CompletedTask;
            }
            public Task DeleteRecurringTransactionAsync(string id) { _recurring.RemoveAll(r => r.Id == id); return Task.CompletedTask; }
            public Task ReplaceAllRecurringTransactionsAsync(IEnumerable<RecurringTransaction> recurring) { _recurring = recurring.ToList(); return Task.CompletedTask; }

            public Task GeneratePendingRecurringTransactionsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task<YearSummary> GetYearSummaryAsync(int year) => Task.FromResult(new YearSummary());
            public Task<MonthSummary> GetMonthSummaryAsync(int year, int month) => Task.FromResult(new MonthSummary());

            public Task<List<CategoryBudget>> GetBudgetsAsync() => Task.FromResult(_budgets.ToList());
            public Task SaveBudgetAsync(CategoryBudget budget)
            {
                var idx = _budgets.FindIndex(b => b.Id == budget.Id);
                if (idx >= 0) _budgets[idx] = budget; else _budgets.Add(budget);
                return Task.CompletedTask;
            }
            public Task DeleteBudgetAsync(string id) { _budgets.RemoveAll(b => b.Id == id); return Task.CompletedTask; }
            public Task<CategoryBudget?> GetBudgetForCategoryAsync(string kategorieId, int year, int month) => Task.FromResult<CategoryBudget?>(null);
            public Task ReplaceAllBudgetsAsync(IEnumerable<CategoryBudget> budgets) { _budgets = budgets.ToList(); return Task.CompletedTask; }

            public Task<List<SparZiel>> GetSparZieleAsync() => Task.FromResult(_sparziele.ToList());
            public Task SaveSparZielAsync(SparZiel sparZiel)
            {
                var idx = _sparziele.FindIndex(s => s.Id == sparZiel.Id);
                if (idx >= 0) _sparziele[idx] = sparZiel; else _sparziele.Add(sparZiel);
                return Task.CompletedTask;
            }
            public Task DeleteSparZielAsync(string id) { _sparziele.RemoveAll(s => s.Id == id); return Task.CompletedTask; }
            public Task ReplaceAllSparZieleAsync(IEnumerable<SparZiel> sparziele) { _sparziele = sparziele.ToList(); return Task.CompletedTask; }

            public Task<List<TransactionTemplate>> GetTransactionTemplatesAsync() => Task.FromResult(_transactionTemplates.ToList());
            public Task SaveTransactionTemplateAsync(TransactionTemplate template)
            {
                var idx = _transactionTemplates.FindIndex(t => t.Id == template.Id);
                if (idx >= 0) _transactionTemplates[idx] = template; else _transactionTemplates.Add(template);
                return Task.CompletedTask;
            }
            public Task DeleteTransactionTemplateAsync(string id) { _transactionTemplates.RemoveAll(t => t.Id == id); return Task.CompletedTask; }
            public Task ReplaceAllTransactionTemplatesAsync(IEnumerable<TransactionTemplate> templates) { _transactionTemplates = templates.ToList(); return Task.CompletedTask; }
        }
#pragma warning restore CS0618

        private class MockSettingsService : SettingsService
        {
            public MockSettingsService(string testDataDir) : base(Path.Combine(testDataDir, "settings.json"))
            {
            }
        }

        /// <summary>
        /// A data service that always throws on ReplaceAll operations (both write and rollback).
        /// Used to test the scenario where restore fails AND rollback also fails.
        /// </summary>
#pragma warning disable CS0618
        private class FailingMockDataService : IDataService, IAccountRepository
        {
            public Task<List<Category>> GetCategoriesAsync() => Task.FromResult(new List<Category>());
            public Task<List<Account>> GetAccountsAsync() => Task.FromResult(new List<Account>());
            public Task<List<Transaction>> GetTransactionsAsync(DateTime v, DateTime b) => Task.FromResult(new List<Transaction>());
            public Task<List<RecurringTransaction>> GetRecurringTransactionsAsync() => Task.FromResult(new List<RecurringTransaction>());
            public Task<List<CategoryBudget>> GetBudgetsAsync() => Task.FromResult(new List<CategoryBudget>());
            public Task<List<SparZiel>> GetSparZieleAsync() => Task.FromResult(new List<SparZiel>());

            public Task ReplaceAllCategoriesAsync(IEnumerable<Category> c) => Task.FromException(new InvalidOperationException("Simulated write failure"));
            public Task ReplaceAllAccountsAsync(IEnumerable<Account> a) => Task.FromException(new InvalidOperationException("Simulated write failure"));
            public Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> t) => Task.FromException(new InvalidOperationException("Simulated write failure"));
            public Task ReplaceAllRecurringTransactionsAsync(IEnumerable<RecurringTransaction> r) => Task.FromException(new InvalidOperationException("Simulated write failure"));
            public Task ReplaceAllBudgetsAsync(IEnumerable<CategoryBudget> b) => Task.FromException(new InvalidOperationException("Simulated write failure"));
            public Task ReplaceAllSparZieleAsync(IEnumerable<SparZiel> s) => Task.FromException(new InvalidOperationException("Simulated write failure"));

            public Task SaveCategoryAsync(Category c) => Task.CompletedTask;
            public Task SaveAccountAsync(Account a) => Task.CompletedTask;
            public Task DeleteCategoryAsync(string id) => Task.CompletedTask;
            public Task DeleteAccountAsync(string id) => Task.CompletedTask;
            public Task SaveTransactionAsync(Transaction t) => Task.CompletedTask;
            public Task SaveTransactionsAsync(IEnumerable<Transaction> transactions) => Task.CompletedTask;
            public Task DeleteTransactionAsync(string id) => Task.CompletedTask;
            public Task DeleteTransferGroupAsync(string transferGroupId) => Task.CompletedTask;
            public Task<Category?> GetMostCommonCategoryForPayeeAsync(string p, double c = 0.5, CancellationToken ct = default) => Task.FromResult<Category?>(null);
            public Task SaveRecurringTransactionAsync(RecurringTransaction r) => Task.CompletedTask;
            public Task DeleteRecurringTransactionAsync(string id) => Task.CompletedTask;
            public Task GeneratePendingRecurringTransactionsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<YearSummary> GetYearSummaryAsync(int y) => Task.FromResult(new YearSummary());
            public Task<MonthSummary> GetMonthSummaryAsync(int y, int m) => Task.FromResult(new MonthSummary());
            public Task SaveBudgetAsync(CategoryBudget b) => Task.CompletedTask;
            public Task DeleteBudgetAsync(string id) => Task.CompletedTask;
            public Task<CategoryBudget?> GetBudgetForCategoryAsync(string k, int y, int m) => Task.FromResult<CategoryBudget?>(null);
            public Task SaveSparZielAsync(SparZiel s) => Task.CompletedTask;
            public Task DeleteSparZielAsync(string id) => Task.CompletedTask;
        }
#pragma warning restore CS0618
    }
}
