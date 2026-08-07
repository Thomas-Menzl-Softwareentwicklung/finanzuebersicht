using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Finanzuebersicht.Core.Constants;
using Microsoft.Extensions.Logging;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Infrastructure.Services
{
    /// <summary>
    /// Implementierung des Backup- und Restore-Services mit Versionierung und Validierung.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IRecurringTransactionRepository _recurringRepository;
        private readonly IBudgetRepository _budgetRepository;
        private readonly ISparZielRepository _sparZielRepository;
        private readonly ITransactionTemplateRepository? _transactionTemplateRepository;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<BackupService>? _logger;
        private readonly Finanzuebersicht.Core.Services.IClock _clock;
        private readonly DataMigrationService _migrationService;

        private static readonly JsonSerializerOptions BackupJsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private const int CurrentSchemaVersion = 3;

        public BackupService(
            ICategoryRepository categoryRepository,
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IRecurringTransactionRepository recurringRepository,
            IBudgetRepository budgetRepository,
            ISparZielRepository sparZielRepository,
            ISettingsService settingsService,
            DataMigrationService migrationService,
            ITransactionTemplateRepository? transactionTemplateRepository = null,
            ILogger<BackupService>? logger = null,
            Finanzuebersicht.Core.Services.IClock? clock = null)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _recurringRepository = recurringRepository ?? throw new ArgumentNullException(nameof(recurringRepository));
            _budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
            _sparZielRepository = sparZielRepository ?? throw new ArgumentNullException(nameof(sparZielRepository));
            _transactionTemplateRepository = transactionTemplateRepository;
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
            _logger = logger;
            _clock = clock ?? Finanzuebersicht.Core.Services.SystemClock.Instance;
        }

        /// <summary>
        /// Erstellt ein ZIP-basiertes Backup mit allen Daten und Metadaten.
        /// </summary>
        public async Task<BackupMetadata> CreateBackupAsync(string? customPath = null)
        {
            try
            {
                var backupPath = customPath ?? _settingsService.GetBackupPath();
                Directory.CreateDirectory(backupPath);

                // Backup ID = ISO-Timestamp format (z.B. 2026-03-11T21-46-19-123)
                var backupId = _clock.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fff");
                var fileName = $"backup_{backupId}.zip";
                var filePath = Path.Combine(backupPath, fileName);

                // Lade alle Daten
                var categories = await _categoryRepository.GetCategoriesAsync();
                var accounts = await _accountRepository.GetAccountsAsync();
                var transactions = await _transactionRepository.GetAllTransactionsAsync();
                var recurring = await _recurringRepository.GetRecurringTransactionsAsync();
                var budgets = await _budgetRepository.GetBudgetsAsync();
                var sparziele = await _sparZielRepository.GetSparZieleAsync();
                List<TransactionTemplate> transactionTemplates = _transactionTemplateRepository is null
                    ? []
                    : await _transactionTemplateRepository.GetTransactionTemplatesAsync();

                // Erstelle Metadaten
                var metadata = new BackupMetadata
                {
                    Id = backupId,
                    CreatedAt = _clock.UtcNow,
                    SchemaVersion = CurrentSchemaVersion,
                    FileName = fileName,
                    EntityCounts = new Dictionary<string, int>
                    {
                        { BackupEntityKeys.Categories, categories.Count() },
                        { BackupEntityKeys.Accounts, accounts.Count() },
                        { BackupEntityKeys.Transactions, transactions.Count() },
                        { BackupEntityKeys.Recurring, recurring.Count() },
                        { BackupEntityKeys.Budgets, budgets.Count },
                        { BackupEntityKeys.Sparziele, sparziele.Count },
                        { BackupEntityKeys.TransactionTemplates, transactionTemplates.Count }
                    }
                };

                // Erstelle ZIP
                using (var zipArchive = ZipFile.Open(filePath, ZipArchiveMode.Create))
                {
                    // Schreibe Daten-Dateien
                    WriteJsonToZip(zipArchive, DataFileNames.Categories, categories);
                    WriteJsonToZip(zipArchive, DataFileNames.Accounts, accounts);
                    WriteJsonToZip(zipArchive, DataFileNames.Transactions, transactions);
                    WriteJsonToZip(zipArchive, DataFileNames.Recurring, recurring);
                    WriteJsonToZip(zipArchive, DataFileNames.Budgets, budgets);
                    WriteJsonToZip(zipArchive, DataFileNames.Sparziele, sparziele);
                    WriteJsonToZip(zipArchive, DataFileNames.TransactionTemplates, transactionTemplates);
                    WriteJsonToZip(zipArchive, DataFileNames.BackupMetadata, metadata);
                }

                _logger?.LogInformation("Backup erstellt: {FileName} mit {CatCount} Kategorien, {TxnCount} Transaktionen, {RecCount} Daueraufträgen, {BudCount} Budgets, {SparCount} Sparzielen, {TemplateCount} Vorlagen",
                    fileName,
                    metadata.EntityCounts[BackupEntityKeys.Categories],
                    metadata.EntityCounts[BackupEntityKeys.Transactions],
                    metadata.EntityCounts[BackupEntityKeys.Recurring],
                    metadata.EntityCounts[BackupEntityKeys.Budgets],
                    metadata.EntityCounts[BackupEntityKeys.Sparziele],
                    metadata.EntityCounts[BackupEntityKeys.TransactionTemplates]);

                // Speichere Zeitstempel in Settings
                _settingsService.SetLastBackupTime(_clock.UtcNow);

                return metadata;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Erstellen des Backups");
                throw;
            }
        }

        /// <summary>
        /// Listet alle Backups im Verzeichnis.
        /// </summary>
        public async Task<IEnumerable<BackupMetadata>> ListBackupsAsync(string backupPath)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                    return [];

                var backups = new List<BackupMetadata>();

                foreach (var zipFile in Directory.EnumerateFiles(backupPath, "backup_*.zip"))
                {
                    try
                    {
                        var metadata = ExtractMetadataFromZip(zipFile);
                        if (metadata != null)
                            backups.Add(metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Fehler beim Lesen von Backup-Metadaten aus {FileName}", Path.GetFileName(zipFile));
                    }
                }

                // Sortiere nach Erstellungsdatum (neueste zuerst)
                return backups.OrderByDescending(b => b.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Auflisten von Backups");
                throw;
            }
        }

        /// <summary>
        /// Stellt ein Backup wieder her mit Validierung und atomarer Operation.
        /// </summary>
        public async Task<RestoreResult> RestoreBackupAsync(string backupPath, string backupId)
        {
            try
            {
                var fileName = $"backup_{backupId}.zip";
                var filePath = Path.Combine(backupPath, fileName);

                if (!File.Exists(filePath))
                {
                    return new RestoreResult
                    {
                        Success = false,
                        ErrorMessage = $"Backup-Datei nicht gefunden: {fileName}"
                    };
                }

                // Validiere ZIP-Struktur und Metadaten
                var validationResult = ValidateBackup(filePath);
                if (!validationResult.Success)
                    return validationResult;

                // Extrahiere alle Dateien aus ZIP als rohe JSON-Strings
                BackupArchiveData archiveData;
                using (var zipArchive = ZipFile.OpenRead(filePath))
                {
                    var metadata = ReadJsonFromZip<BackupMetadata>(zipArchive, DataFileNames.BackupMetadata);
                    if (metadata == null)
                        return new RestoreResult { Success = false, ErrorMessage = "ZIP-Datei ist beschädigt oder unvollständig" };

                    archiveData = new BackupArchiveData { Metadata = metadata };
                    foreach (var entry in zipArchive.Entries)
                    {
                        using var stream = entry.Open();
                        using var reader = new System.IO.StreamReader(stream);
                        archiveData.Files[entry.Name] = await reader.ReadToEndAsync();
                    }
                }

                // Migration anwenden falls nötig
                if (_migrationService.NeedsMigration(archiveData.Metadata, CurrentSchemaVersion))
                {
                    _logger?.LogInformation("Backup Schema v{From} → v{To}: Migration wird angewendet",
                        archiveData.Metadata.SchemaVersion, CurrentSchemaVersion);
                    archiveData = await _migrationService.MigrateAsync(archiveData, CurrentSchemaVersion);
                }

                // Deserialisiere die (ggf. migrierten) Daten in konkrete Typen
                var categories = DeserializeFile<List<Category>>(archiveData, DataFileNames.Categories);
                var accounts = DeserializeFile<List<Account>>(archiveData, DataFileNames.Accounts);
                var transactions = DeserializeFile<List<Transaction>>(archiveData, DataFileNames.Transactions);
                var recurring = DeserializeFile<List<RecurringTransaction>>(archiveData, DataFileNames.Recurring);
                var budgets = DeserializeFile<List<CategoryBudget>>(archiveData, DataFileNames.Budgets);
                var sparziele = DeserializeFile<List<SparZiel>>(archiveData, DataFileNames.Sparziele);
                var transactionTemplates = DeserializeFile<List<TransactionTemplate>>(archiveData, DataFileNames.TransactionTemplates) ?? [];

                if (categories == null || accounts == null || transactions == null || recurring == null || budgets == null || sparziele == null)
                    return new RestoreResult { Success = false, ErrorMessage = "ZIP-Datei ist beschädigt oder unvollständig" };

                // Atomare Restore mit Rollback-Capability
                var restoreSuccess = await AtomicRestoreAsync(categories, accounts, transactions, recurring, budgets, sparziele, transactionTemplates);

                if (!restoreSuccess)
                    return new RestoreResult { Success = false, ErrorMessage = "Fehler beim Speichern der wiederhergestellten Daten" };

                _logger?.LogInformation("Restore aus Backup {BackupId} erfolgreich abgeschlossen", backupId);

                var counts = archiveData.Metadata.EntityCounts;
                return new RestoreResult
                {
                    Success = true,
                    Details = $"Wiederhergestellt: {counts.GetValueOrDefault(BackupEntityKeys.Categories)} Kategorien, " +
                              $"{counts.GetValueOrDefault(BackupEntityKeys.Transactions)} Transaktionen, " +
                              $"{counts.GetValueOrDefault(BackupEntityKeys.Recurring)} Daueraufträge, " +
                              $"{counts.GetValueOrDefault(BackupEntityKeys.TransactionTemplates)} Vorlagen",
                    RestoredMetadata = archiveData.Metadata
                };
            }
            catch (RollbackFailedException ex)
            {
                _logger?.LogCritical(ex, "Restore und Rollback fehlgeschlagen für Backup {BackupId} – Daten möglicherweise inkonsistent", backupId);
                return new RestoreResult
                {
                    Success = false,
                    DataMayBeInconsistent = true,
                    ErrorMessage = "Wiederherstellung und Rollback sind fehlgeschlagen. Die Daten könnten inkonsistent sein. Bitte starte die App neu und versuche es erneut."
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Restore aus Backup {BackupId}", backupId);
                return new RestoreResult
                {
                    Success = false,
                    ErrorMessage = $"Fehler beim Restore: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Stellt alle Daten aus dem Backup wieder her in je einem einzigen Schreibvorgang pro Entity-Typ.
        /// Bei einem Fehler wird ein Rollback auf den vorherigen Zustand durchgeführt.
        /// Wirft <see cref="RollbackFailedException"/> wenn der Rollback selbst fehlschlägt.
        /// </summary>
        private async Task<bool> AtomicRestoreAsync(
            List<Category> categories,
            List<Account> accounts,
            List<Transaction> transactions,
            List<RecurringTransaction> recurring,
            List<CategoryBudget> budgets,
            List<SparZiel> sparziele,
            List<TransactionTemplate> transactionTemplates)
        {
            // Snapshot des aktuellen Zustands laden (für Rollback).
            // Wenn dieser Schritt fehlschlägt (z.B. DataCorruptionException), bricht der Restore
            // ab bevor irgendetwas überschrieben wurde — das ist das gewünschte Verhalten.
            var snapshotCategories = await _categoryRepository.GetCategoriesAsync();
            var snapshotAccounts = await _accountRepository.GetAccountsAsync();
            var snapshotTransactions = await _transactionRepository.GetAllTransactionsAsync();
            var snapshotRecurring = await _recurringRepository.GetRecurringTransactionsAsync();
            var snapshotBudgets = await _budgetRepository.GetBudgetsAsync();
            var snapshotSparziele = await _sparZielRepository.GetSparZieleAsync();
            List<TransactionTemplate> snapshotTransactionTemplates = _transactionTemplateRepository is null
                ? []
                : await _transactionTemplateRepository.GetTransactionTemplatesAsync();

            try
            {
                _logger?.LogInformation("Starte Wiederherstellung: {CatCount} Kategorien, {TxnCount} Transaktionen, {RecCount} Daueraufträge, {BudCount} Budgets, {SparCount} Sparziele, {TemplateCount} Vorlagen",
                    categories.Count, transactions.Count, recurring.Count, budgets.Count, sparziele.Count, transactionTemplates.Count);

                await _categoryRepository.ReplaceAllCategoriesAsync(categories);
                await _accountRepository.ReplaceAllAccountsAsync(accounts);
                await _transactionRepository.ReplaceAllTransactionsAsync(transactions);
                await _recurringRepository.ReplaceAllRecurringTransactionsAsync(recurring);
                await _budgetRepository.ReplaceAllBudgetsAsync(budgets);
                await _sparZielRepository.ReplaceAllSparZieleAsync(sparziele);
                if (_transactionTemplateRepository is not null)
                    await _transactionTemplateRepository.ReplaceAllTransactionTemplatesAsync(transactionTemplates);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler bei der Wiederherstellung – starte Rollback");
                await RollbackAsync(snapshotCategories, snapshotAccounts, snapshotTransactions, snapshotRecurring, snapshotBudgets, snapshotSparziele, snapshotTransactionTemplates);
                return false;
            }
        }

        private async Task RollbackAsync(
            List<Category> categories,
            List<Account> accounts,
            List<Transaction> transactions,
            List<RecurringTransaction> recurring,
            List<CategoryBudget> budgets,
            List<SparZiel> sparziele,
            List<TransactionTemplate> transactionTemplates)
        {
            try
            {
                await _categoryRepository.ReplaceAllCategoriesAsync(categories);
                await _accountRepository.ReplaceAllAccountsAsync(accounts);
                await _transactionRepository.ReplaceAllTransactionsAsync(transactions);
                await _recurringRepository.ReplaceAllRecurringTransactionsAsync(recurring);
                await _budgetRepository.ReplaceAllBudgetsAsync(budgets);
                await _sparZielRepository.ReplaceAllSparZieleAsync(sparziele);
                if (_transactionTemplateRepository is not null)
                    await _transactionTemplateRepository.ReplaceAllTransactionTemplatesAsync(transactionTemplates);

                _logger?.LogInformation("Rollback erfolgreich abgeschlossen");
            }
            catch (Exception rollbackEx)
            {
                _logger?.LogCritical(rollbackEx, "Rollback fehlgeschlagen – Datenzustand ist möglicherweise inkonsistent");
                throw new RollbackFailedException("Rollback nach fehlgeschlagenem Restore ist fehlgeschlagen. Der Datenzustand kann inkonsistent sein.", rollbackEx);
            }
        }

        /// <summary>
        /// Löscht ein Backup.
        /// </summary>
        public async Task DeleteBackupAsync(string backupPath, string backupId)
        {
            try
            {
                var fileName = $"backup_{backupId}.zip";
                var filePath = Path.Combine(backupPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger?.LogInformation("Backup gelöscht: {FileName}", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Löschen von Backup {BackupId}", backupId);
                throw;
            }
        }

        /// <summary>
        /// Exportiert aktuelle Daten als CSV.
        /// </summary>
        public async Task<Stream> ExportAsCSVAsync()
        {
            try
            {
                var transactions = await _transactionRepository.GetAllTransactionsAsync();
                var categories = await _categoryRepository.GetCategoriesAsync();
                var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

                var memoryStream = new MemoryStream();
                using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
                {
                    // CSV Header für Transaktionen
                    await writer.WriteLineAsync("Datum,Titel,Betrag,Typ,Kategorie,Verwendungszweck");

                    foreach (var txn in transactions.OrderBy(t => t.Datum))
                    {
                        var category = categoryMap.TryGetValue(txn.KategorieId, out var cat) ? cat : "Unbekannt";
                        var betrag = txn.Betrag.ToString("F2").Replace(",", ".");
                        var titel = EscapeCSV(txn.Titel);
                        var zweck = EscapeCSV(txn.Verwendungszweck);

                        await writer.WriteLineAsync(
                            $"{txn.Datum:yyyy-MM-dd},\"{titel}\",{betrag},{txn.Typ},\"{category}\",\"{zweck}\"");
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim CSV-Export");
                throw;
            }
        }

        // ========== Hilfsmethoden ==========

        private static void WriteJsonToZip<T>(ZipArchive archive, string entryName, T data)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);
            var json = JsonSerializer.Serialize(data, BackupJsonOptions);
            writer.Write(json);
        }

        private static T? DeserializeFile<T>(BackupArchiveData archive, string fileName) where T : class
        {
            if (!archive.Files.TryGetValue(fileName, out var json))
                return null;
            return JsonSerializer.Deserialize<T>(json, BackupJsonOptions);
        }

        private static T? ReadJsonFromZip<T>(ZipArchive archive, string entryName) where T : class
        {
            var entry = archive.GetEntry(entryName);
            if (entry == null)
                return null;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<T>(json, BackupJsonOptions);
        }

        private BackupMetadata? ExtractMetadataFromZip(string zipPath)
        {
            using var zipArchive = ZipFile.OpenRead(zipPath);
            return ReadJsonFromZip<BackupMetadata>(zipArchive, DataFileNames.BackupMetadata);
        }

        private static RestoreResult ValidateBackup(string zipPath)
        {
            try
            {
                using (var zipArchive = ZipFile.OpenRead(zipPath))
                {
                    // Validiere Metadaten-Schema: ältere Versionen werden migriert, neuere abgelehnt
                    var metadata = ReadJsonFromZip<BackupMetadata>(zipArchive, DataFileNames.BackupMetadata);
                    if (metadata == null || metadata.SchemaVersion > CurrentSchemaVersion)
                    {
                        return new RestoreResult
                        {
                            Success = false,
                            ErrorMessage = $"Schema-Version nicht kompatibel. Backup: v{metadata?.SchemaVersion}, App: v{CurrentSchemaVersion}"
                        };
                    }

                    var requiredFiles = new List<string>
                    {
                        DataFileNames.Categories,
                        DataFileNames.Transactions,
                        DataFileNames.Recurring,
                        DataFileNames.BackupMetadata
                    };
                    if (metadata.SchemaVersion >= 3)
                        requiredFiles.Add(DataFileNames.Accounts);

                    var missingFiles = requiredFiles.Where(f => zipArchive.GetEntry(f) == null).ToList();
                    if (missingFiles.Count != 0)
                    {
                        return new RestoreResult
                        {
                            Success = false,
                            ErrorMessage = $"Backup unvollständig. Fehlende Dateien: {string.Join(", ", missingFiles)}"
                        };
                    }

                    return new RestoreResult { Success = true };
                }
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    ErrorMessage = $"ZIP-Datei beschädigt: {ex.Message}"
                };
            }
        }

        private static string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\"", "\"\"");
        }
    }
}
