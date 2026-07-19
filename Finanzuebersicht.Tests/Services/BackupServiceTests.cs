using Xunit;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Models;
using System.IO.Compression;
using System.Text.Json;

namespace Finanzuebersicht.Tests.Services
{
    public class BackupServiceTests : IDisposable
    {
        private readonly string _testBackupDir;
        private readonly string _testDataDir;
        private readonly MockDataService _mockDataService;
        private readonly MockSettingsService _mockSettingsService;

        public BackupServiceTests()
        {
            _testBackupDir = Path.Combine(Path.GetTempPath(), $"backup_tests_{Guid.NewGuid()}");
            _testDataDir = Path.Combine(Path.GetTempPath(), $"data_tests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testBackupDir);
            Directory.CreateDirectory(_testDataDir);

            _mockDataService = new MockDataService();
            _mockSettingsService = new MockSettingsService(_testDataDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testBackupDir))
                    Directory.Delete(_testBackupDir, true);
                if (Directory.Exists(_testDataDir))
                    Directory.Delete(_testDataDir, true);
            }
            catch { }
        }

        [Fact]
        public async Task CreateBackupAsync_CreatesValidZipFile()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            _mockDataService.SetCategories(new[]
            {
                new Category { Id = "cat1", Name = "Test", Icon = "🏠", Color = "#000" }
            });
            _mockDataService.SetTransactions(new[]
            {
                new Transaction { Id = "txn1", Titel = "Test", Betrag = 100m, Datum = DateTime.Now, KategorieId = "cat1" }
            });

            // Act
            var metadata = await service.CreateBackupAsync(_testBackupDir);

            // Assert
            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata.Id);
            Assert.Equal("2026-03-11T21-46-19-123".Length, metadata.Id.Length); // Format: yyyy-MM-ddTHH-mm-ss-fff
            Assert.Contains("backup_", metadata.FileName);
            Assert.True(File.Exists(Path.Combine(_testBackupDir, metadata.FileName)));
        }

        [Fact]
        public async Task CreateBackupAsync_ZipContainsAllRequiredFiles()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            _mockDataService.SetCategories(new[] { new Category { Id = "1", Name = "Test", Icon = "🏠", Color = "#000" } });

            // Act
            var metadata = await service.CreateBackupAsync(_testBackupDir);
            var zipPath = Path.Combine(_testBackupDir, metadata.FileName);

            // Assert
            using (var zipArchive = ZipFile.OpenRead(zipPath))
            {
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Categories));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Accounts));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Transactions));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Recurring));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Budgets));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Sparziele));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.TransactionTemplates));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.BackupMetadata));
            }
        }

        [Fact]
        public async Task CreateBackupAsync_MetadataHasCorrectEntityCounts()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var categories = new[] { 
                new Category { Id = "1", Name = "Cat1", Icon = "🏠", Color = "#000" },
                new Category { Id = "2", Name = "Cat2", Icon = "🛒", Color = "#111" }
            };
            var accounts = new[]
            {
                new Account { Id = "a1", Name = "Girokonto", Type = AccountType.Girokonto }
            };
            var transactions = new[] {
                new Transaction { Id = "t1", Titel = "T1", Betrag = 100m, Datum = DateTime.Now, KategorieId = "1" },
                new Transaction { Id = "t2", Titel = "T2", Betrag = 200m, Datum = DateTime.Now, KategorieId = "1" },
                new Transaction { Id = "t3", Titel = "T3", Betrag = 300m, Datum = DateTime.Now, KategorieId = "2" }
            };
            var recurring = new[] {
                new RecurringTransaction { Id = "r1", Titel = "R1", Betrag = 50m, KategorieId = "1" }
            };

            _mockDataService.SetCategories(categories);
            _mockDataService.SetAccounts(accounts);
            _mockDataService.SetTransactions(transactions);
            _mockDataService.SetRecurring(recurring);

            // Act
            var metadata = await service.CreateBackupAsync(_testBackupDir);

            // Assert
            Assert.Equal(2, metadata.EntityCounts[BackupEntityKeys.Categories]);
            Assert.Equal(1, metadata.EntityCounts[BackupEntityKeys.Accounts]);
            Assert.Equal(3, metadata.EntityCounts[BackupEntityKeys.Transactions]);
            Assert.Equal(1, metadata.EntityCounts[BackupEntityKeys.Recurring]);
        }

        [Fact]
        public async Task CreateBackupAsync_ZipContainsBudgetsAndSparZiele()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            _mockDataService.SetBudgets(new[]
            {
                new CategoryBudget { Id = "b1", KategorieId = "cat1", Betrag = 200m, Jahr = 2026, Monat = 1 },
                new CategoryBudget { Id = "b2", KategorieId = "cat2", Betrag = 150m, Jahr = 2026, Monat = 1 }
            });
            _mockDataService.SetSparZiele(new[]
            {
                new SparZiel { Id = "s1", Titel = "Urlaub", ZielBetrag = 2000m, AktuellerBetrag = 500m }
            });

            // Act
            var metadata = await service.CreateBackupAsync(_testBackupDir);
            var zipPath = Path.Combine(_testBackupDir, metadata.FileName);

            // Assert: ZIP enthält budgets.json und sparziele.json
            using (var zipArchive = ZipFile.OpenRead(zipPath))
            {
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Budgets));
                Assert.NotNull(zipArchive.GetEntry(DataFileNames.Sparziele));
            }

            // Assert: EntityCounts enthält budgets und sparziele
            Assert.Equal(2, metadata.EntityCounts[BackupEntityKeys.Budgets]);
            Assert.Equal(1, metadata.EntityCounts[BackupEntityKeys.Sparziele]);
        }

        [Fact]
        public async Task ListBackupsAsync_ReturnsBackupsInReverseChronologicalOrder()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            _mockDataService.SetCategories(new[] { new Category { Id = "1", Name = "Test", Icon = "🏠", Color = "#000" } });

            // Create backups with small delays
            var backup1 = await service.CreateBackupAsync(_testBackupDir);
            await Task.Delay(100);
            var backup2 = await service.CreateBackupAsync(_testBackupDir);

            // Act
            var backups = (await service.ListBackupsAsync(_testBackupDir)).ToList();

            // Assert
            Assert.Equal(2, backups.Count);
            Assert.True(backups[0].CreatedAt >= backups[1].CreatedAt, "Backups should be newest first");
        }

        [Fact]
        public async Task ListBackupsAsync_ReturnsEmptyWhenDirectoryDoesNotExist()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var nonExistentPath = Path.Combine(_testBackupDir, "nonexistent");

            // Act
            var backups = await service.ListBackupsAsync(nonExistentPath);

            // Assert
            Assert.Empty(backups);
        }

        [Fact]
        public async Task RestoreBackupAsync_FailsWhenFileDoesNotExist()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));

            // Act
            var result = await service.RestoreBackupAsync(_testBackupDir, "nonexistent_id");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("nicht gefunden", result.ErrorMessage);
        }

        [Fact]
        public async Task RestoreBackupAsync_ValidatesZipIntegrity()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var corruptedZipPath = Path.Combine(_testBackupDir, "backup_corrupted.zip");
            File.WriteAllText(corruptedZipPath, "This is not a valid ZIP file");

            // Act
            var result = await service.RestoreBackupAsync(_testBackupDir, "corrupted");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("beschädigt", result.ErrorMessage);
        }

        [Fact]
        public async Task RestoreBackupAsync_DetectsMissingRequiredFiles()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var incompleteZipPath = Path.Combine(_testBackupDir, "backup_incomplete.zip");

            // Create incomplete ZIP (missing files)
            using (var zipArchive = ZipFile.Open(incompleteZipPath, ZipArchiveMode.Create))
            {
                var entry = zipArchive.CreateEntry(DataFileNames.Categories);
                using (var stream = entry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write("[]");
                }
                WriteJsonToZip(zipArchive, DataFileNames.BackupMetadata, new BackupMetadata
                {
                    Id = "incomplete",
                    CreatedAt = DateTime.UtcNow,
                    SchemaVersion = 3,
                    FileName = "backup_incomplete.zip",
                    EntityCounts = new Dictionary<string, int>()
                });
            }

            // Act
            var result = await service.RestoreBackupAsync(_testBackupDir, "incomplete");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Fehlende Dateien", result.ErrorMessage);
        }

        [Fact]
        public async Task RestoreBackupAsync_ValidatesSchemaVersion()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var wrongVersionZipPath = Path.Combine(_testBackupDir, "backup_wrongversion.zip");

            // Create backup with wrong schema version
            var wrongMetadata = new BackupMetadata
            {
                Id = "test",
                CreatedAt = DateTime.UtcNow,
                SchemaVersion = 999, // Wrong version!
                EntityCounts = new Dictionary<string, int>
                {
                    { BackupEntityKeys.Categories, 0 },
                    { BackupEntityKeys.Transactions, 0 },
                    { BackupEntityKeys.Recurring, 0 }
                },
                FileName = "backup_wrongversion.zip"
            };

            using (var zipArchive = ZipFile.Open(wrongVersionZipPath, ZipArchiveMode.Create))
            {
                WriteJsonToZip(zipArchive, DataFileNames.Categories, new List<object>());
                WriteJsonToZip(zipArchive, DataFileNames.Transactions, new List<object>());
                WriteJsonToZip(zipArchive, DataFileNames.Recurring, new List<object>());
                WriteJsonToZip(zipArchive, DataFileNames.BackupMetadata, wrongMetadata);
            }

            // Act
            var result = await service.RestoreBackupAsync(_testBackupDir, "wrongversion");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Schema-Version", result.ErrorMessage);
        }

        [Fact]
        public async Task ExportAsCSVAsync_CreatesValidCsvStream()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var categories = new[] { new Category { Id = "1", Name = "Lebensmittel", Icon = "🛒", Color = "#000" } };
            var transactions = new[] {
                new Transaction { Id = "t1", Titel = "Supermarkt", Betrag = 50.50m, Datum = new DateTime(2026, 3, 11), 
                    KategorieId = "1", Verwendungszweck = "Wöchentliches Einkaufen" }
            };

            _mockDataService.SetCategories(categories);
            _mockDataService.SetTransactions(transactions);

            // Act
            var csvStream = await service.ExportAsCSVAsync();
            using (var reader = new StreamReader(csvStream))
            {
                var csv = reader.ReadToEnd();

                // Assert
                Assert.Contains("Datum,Titel,Betrag,Typ,Kategorie,Verwendungszweck", csv);
                Assert.Contains("2026-03-11", csv);
                Assert.Contains("Supermarkt", csv);
                Assert.Contains("50.50", csv);
                Assert.Contains("Lebensmittel", csv);
            }
        }

        [Fact]
        public async Task ExportAsCSVAsync_EscapesCsvSpecialCharacters()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            var categories = new[] { new Category { Id = "1", Name = "Test", Icon = "🏠", Color = "#000" } };
            var transactions = new[] {
                new Transaction { Id = "t1", Titel = "Titel \"mit Anführungszeichen\"", Betrag = 100m, 
                    Datum = DateTime.Now, KategorieId = "1", Verwendungszweck = "" }
            };

            _mockDataService.SetCategories(categories);
            _mockDataService.SetTransactions(transactions);

            // Act
            var csvStream = await service.ExportAsCSVAsync();
            using (var reader = new StreamReader(csvStream))
            {
                var csv = reader.ReadToEnd();

                // Assert
                Assert.Contains("\"mit Anführungszeichen\"", csv);
            }
        }

        [Fact]
        public async Task DeleteBackupAsync_RemovesBackupFile()
        {
            // Arrange
            var service = new BackupService(_mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockDataService, _mockSettingsService, new DataMigrationService([new V1ToV2Migrator(), new V2ToV3Migrator()]));
            _mockDataService.SetCategories(new[] { new Category { Id = "1", Name = "Test", Icon = "🏠", Color = "#000" } });
            var metadata = await service.CreateBackupAsync(_testBackupDir);
            var zipPath = Path.Combine(_testBackupDir, metadata.FileName);

            Assert.True(File.Exists(zipPath));

            // Act
            await service.DeleteBackupAsync(_testBackupDir, metadata.Id);

            // Assert
            Assert.False(File.Exists(zipPath));
        }

        // ========== Hilfsmethoden ==========

        private static void WriteJsonToZip<T>(ZipArchive archive, string entryName, T data)
        {
            var entry = archive.CreateEntry(entryName);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                writer.Write(json);
            }
        }
    }

    // ========== Mock-Implementierungen ==========

#pragma warning disable CS0618
    internal class MockDataService : IDataService, IAccountRepository
    {
        private List<Category> _categories = [];
        private List<Account> _accounts = [];
        private List<Transaction> _transactions = [];
        private List<RecurringTransaction> _recurring = [];
        private List<CategoryBudget> _budgets = [];
        private List<SparZiel> _sparziele = [];

        public void SetCategories(IEnumerable<Category> categories) => _categories = categories.ToList();
        public void SetAccounts(IEnumerable<Account> accounts) => _accounts = accounts.ToList();
        public void SetTransactions(IEnumerable<Transaction> transactions) => _transactions = transactions.ToList();
        public void SetRecurring(IEnumerable<RecurringTransaction> recurring) => _recurring = recurring.ToList();
        public void SetBudgets(IEnumerable<CategoryBudget> budgets) => _budgets = budgets.ToList();
        public void SetSparZiele(IEnumerable<SparZiel> sparziele) => _sparziele = sparziele.ToList();

        // ICategoryRepository
        public Task<List<Category>> GetCategoriesAsync() => Task.FromResult(_categories);
        public Task<List<Account>> GetAccountsAsync() => Task.FromResult(_accounts);
        public Task SaveCategoryAsync(Category category) => Task.CompletedTask;
        public Task DeleteCategoryAsync(string id) => Task.CompletedTask;
        public Task ReplaceAllCategoriesAsync(IEnumerable<Category> categories) { _categories = categories.ToList(); return Task.CompletedTask; }
        public Task SaveAccountAsync(Account account) => Task.CompletedTask;
        public Task DeleteAccountAsync(string id) => Task.CompletedTask;
        public Task ReplaceAllAccountsAsync(IEnumerable<Account> accounts) { _accounts = accounts.ToList(); return Task.CompletedTask; }

        // ITransactionRepository
        public Task<List<Transaction>> GetTransactionsAsync(DateTime vonDatum, DateTime bisDatum) 
            => Task.FromResult(_transactions.Where(t => t.Datum >= vonDatum && t.Datum <= bisDatum).ToList());
        public Task SaveTransactionAsync(Transaction transaction) => Task.CompletedTask;
        public Task SaveTransactionsAsync(IEnumerable<Transaction> transactions) => Task.CompletedTask;
        public Task DeleteTransactionAsync(string id) => Task.CompletedTask;
        public Task DeleteTransferGroupAsync(string transferGroupId) => Task.CompletedTask;
        public Task ReplaceAllTransactionsAsync(IEnumerable<Transaction> transactions) { _transactions = transactions.ToList(); return Task.CompletedTask; }
        public Task<Category?> GetMostCommonCategoryForPayeeAsync(
            string payee,
            double confidenceThreshold = 0.5,
            CancellationToken cancellationToken = default) 
            => Task.FromResult<Category?>(null);

        // IRecurringTransactionRepository
        public Task<List<RecurringTransaction>> GetRecurringTransactionsAsync() => Task.FromResult(_recurring);
        public Task SaveRecurringTransactionAsync(RecurringTransaction recurring) => Task.CompletedTask;
        public Task DeleteRecurringTransactionAsync(string id) => Task.CompletedTask;
        public Task ReplaceAllRecurringTransactionsAsync(IEnumerable<RecurringTransaction> recurring) { _recurring = recurring.ToList(); return Task.CompletedTask; }

        // IRecurringGenerationService
        public Task GeneratePendingRecurringTransactionsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        // IReportingService
        public Task<YearSummary> GetYearSummaryAsync(int year) => Task.FromResult(new YearSummary());
        public Task<MonthSummary> GetMonthSummaryAsync(int year, int month) => Task.FromResult(new MonthSummary());

        // IBudgetRepository
        public Task<List<CategoryBudget>> GetBudgetsAsync() => Task.FromResult(_budgets);
        public Task SaveBudgetAsync(CategoryBudget budget) => Task.CompletedTask;
        public Task DeleteBudgetAsync(string id) => Task.CompletedTask;
        public Task<CategoryBudget?> GetBudgetForCategoryAsync(string kategorieId, int year, int month) => Task.FromResult<CategoryBudget?>(null);
        public Task ReplaceAllBudgetsAsync(IEnumerable<CategoryBudget> budgets) { _budgets = budgets.ToList(); return Task.CompletedTask; }

        // ISparZielRepository
        public Task<List<SparZiel>> GetSparZieleAsync() => Task.FromResult(_sparziele);
        public Task SaveSparZielAsync(SparZiel sparZiel) => Task.CompletedTask;
        public Task DeleteSparZielAsync(string id) => Task.CompletedTask;
        public Task ReplaceAllSparZieleAsync(IEnumerable<SparZiel> sparziele) { _sparziele = sparziele.ToList(); return Task.CompletedTask; }
    }
#pragma warning restore CS0618

    internal class MockSettingsService : SettingsService
    {
        public MockSettingsService(string testDataDir) : base(Path.Combine(testDataDir, "settings.json"))
        {
        }
    }
}
