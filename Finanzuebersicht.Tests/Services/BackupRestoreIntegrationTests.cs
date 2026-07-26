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
        private readonly InMemoryFinanceStore _mockDataService;
        private readonly MockSettingsService _mockSettingsService;

        public BackupRestoreIntegrationTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"integration_tests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
            _mockDataService = new InMemoryFinanceStore();
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
            var failingDataService = new FailingInMemoryFinanceStore();
            var failingService = new BackupService(failingDataService, failingDataService, failingDataService, failingDataService, failingDataService, failingDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));

            // Act
            var result = await failingService.RestoreBackupAsync(backupPath, metadata.Id);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.DataMayBeInconsistent);
        }

        private class MockSettingsService : SettingsService
        {
            public MockSettingsService(string testDataDir) : base(Path.Combine(testDataDir, "settings.json"))
            {
            }
        }

    }
}
