using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Application.UseCases.Import;

/// <summary>
/// CSV import orchestration (analyze DTOs, commit). Prefer <see cref="PrepareCsvImportUseCase"/> /
/// <see cref="AnalyzeCsvImportUseCase"/> / <see cref="CommitCsvImportUseCase"/> from Presentation.
/// </summary>
public class CsvImportOrchestrator(
    ITransactionRepository transactionRepository,
    ILogger<CsvImportOrchestrator> logger,
    ICategoryRepository? categoryRepository = null,
    CategorizationService? categorizationService = null,
    IAccountRepository? accountRepository = null,
    IUncategorizedCategoryService? uncategorizedCategoryService = null,
    ICloudSyncOrchestrator? cloudSyncOrchestrator = null)
{
    private const double HistoricalConfidenceThreshold = 0.5;

    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ILogger<CsvImportOrchestrator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICategoryRepository? _categoryRepository = categoryRepository;
    private readonly CategorizationService? _categorizationService = categorizationService;
    private readonly IAccountRepository? _accountRepository = accountRepository;
    private readonly IUncategorizedCategoryService? _uncategorizedCategoryService = uncategorizedCategoryService;
    private readonly ICloudSyncOrchestrator? _cloudSyncOrchestrator = cloudSyncOrchestrator;

    public async Task<ImportPreviewResult> AnalyzeDtosAsync(
        IReadOnlyList<TransactionDto> dtos,
        string? accountId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("CsvImport: starting analysis (accountId={AccountId})", accountId ?? "(none)");

        var categories = await LoadCategoriesAsync().ConfigureAwait(false);
        var defaultAccountId = await ResolveDefaultAccountIdAsync(cancellationToken).ConfigureAwait(false);
        var existingInRange = await LoadExistingTransactionsForDtosAsync(dtos).ConfigureAwait(false);
        var historicalCategories = await BuildHistoricalCategoryMapAsync(dtos, categories, cancellationToken).ConfigureAwait(false);

        var rows = new List<ImportPreviewRow>();
        var batchTransactions = new List<Transaction>();

        for (var index = 0; index < dtos.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dto = dtos[index];

            if (dto is null || dto.Buchungsdatum == default || dto.HasUnparsableAmount)
            {
                var placeholderTransaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    Betrag = 0m,
                    Datum = dto?.Buchungsdatum ?? default,
                    Titel = dto is not null ? BuildTitle(dto) : string.Empty,
                    Verwendungszweck = dto?.Verwendungszweck ?? string.Empty,
                    KategorieId = string.Empty,
                    Typ = TransactionType.Ausgabe,
                    AccountId = accountId ?? dto?.SourceAccountId ?? string.Empty
                };

                rows.Add(new ImportPreviewRow
                {
                    SourceIndex = index,
                    IsIncluded = false,
                    Status = ImportPreviewRowStatus.Invalid,
                    StatusMessage = dto is not null && dto.HasUnparsableAmount && dto.Buchungsdatum != default
                        ? ImportMessageKeys.UnparsableAmount
                        : ImportMessageKeys.MissingBookingDate,
                    Transaction = placeholderTransaction
                });
                continue;
            }

            var transaction = CreateTransaction(dto, accountId ?? defaultAccountId);
            if (ContainsDuplicate(existingInRange, transaction) || ContainsDuplicate(batchTransactions, transaction))
            {
                rows.Add(new ImportPreviewRow
                {
                    SourceIndex = index,
                    IsIncluded = false,
                    Status = ImportPreviewRowStatus.Duplicate,
                    StatusMessage = ImportMessageKeys.PossibleDuplicate,
                    Transaction = transaction
                });
                continue;
            }

            transaction.KategorieId = await ResolveCategoryIdForAnalyzeAsync(
                dto,
                categories,
                historicalCategories,
                cancellationToken).ConfigureAwait(false);

            var status = string.IsNullOrWhiteSpace(transaction.KategorieId)
                ? ImportPreviewRowStatus.Uncategorized
                : ImportPreviewRowStatus.Ready;

            rows.Add(new ImportPreviewRow
            {
                SourceIndex = index,
                IsIncluded = true,
                Status = status,
                StatusMessage = status == ImportPreviewRowStatus.Uncategorized
                    ? ImportMessageKeys.CategoryUnresolved
                    : null,
                Transaction = transaction
            });

            batchTransactions.Add(transaction);
        }

        _logger.LogInformation(
            "CsvImport: analysis finished — ready={Ready}, duplicates={Duplicates}, invalid={Invalid}, uncategorized={Uncategorized}",
            rows.Count(r => r.Status == ImportPreviewRowStatus.Ready),
            rows.Count(r => r.Status == ImportPreviewRowStatus.Duplicate),
            rows.Count(r => r.Status == ImportPreviewRowStatus.Invalid),
            rows.Count(r => r.Status == ImportPreviewRowStatus.Uncategorized));

        return new ImportPreviewResult { Rows = rows };
    }

    public async Task<ImportResult> CommitImportAsync(
        ImportPreviewResult preview,
        IEnumerable<string>? selectedRowIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        cancellationToken.ThrowIfCancellationRequested();

        if (!preview.Success)
        {
            return new ImportResult { ErrorMessage = preview.ErrorMessage };
        }

        var selectedIds = selectedRowIds?.ToHashSet(StringComparer.Ordinal)
            ?? preview.Rows.Where(r => r.IsIncluded).Select(r => r.Id).ToHashSet(StringComparer.Ordinal);

        var rowsToCommit = preview.Rows
            .Where(r => selectedIds.Contains(r.Id) && r.IsIncluded && IsCommittableStatus(r.Status))
            .ToList();

        var existingInRange = await LoadExistingTransactionsForRowsAsync(rowsToCommit).ConfigureAwait(false);
        var defaultAccountId = await ResolveDefaultAccountIdAsync(cancellationToken).ConfigureAwait(false);
        var imported = new List<Transaction>();
        var duplicates = new List<Transaction>();
        var saveErrors = new List<string>();
        var committedBatch = new List<Transaction>();
        string? uncategorizedCategoryId = null;

        foreach (var row in rowsToCommit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var transaction = CloneTransaction(row.Transaction);
            if (string.IsNullOrWhiteSpace(transaction.AccountId))
                transaction.AccountId = defaultAccountId;
            if (string.IsNullOrWhiteSpace(transaction.KategorieId))
            {
                uncategorizedCategoryId ??= await EnsureUncategorizedCategoryAsync(cancellationToken).ConfigureAwait(false);
                transaction.KategorieId = uncategorizedCategoryId;
            }

            if (ContainsDuplicate(existingInRange, transaction) || ContainsDuplicate(committedBatch, transaction))
            {
                duplicates.Add(transaction);
                continue;
            }

            try
            {
                CloudSyncNotify.StampUpdatedAt(transaction);
                await _transactionRepository.SaveTransactionAsync(transaction).ConfigureAwait(false);
                await CloudSyncNotify.NotifyUpsertAsync(
                    _cloudSyncOrchestrator,
                    SyncEntityType.Transaction,
                    transaction.Id,
                    cancellationToken).ConfigureAwait(false);
                imported.Add(transaction);
                existingInRange.Add(transaction);
                committedBatch.Add(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CsvImport: failed to save transaction '{Title}' {Amount} on {Date}",
                    transaction.Titel, transaction.Betrag, transaction.Datum);
                saveErrors.Add($"{transaction.Titel} ({transaction.Datum:dd.MM.yyyy}): {ex.Message}");
            }
        }

        return new ImportResult
        {
            Imported = imported,
            Duplicates = duplicates,
            SaveErrors = saveErrors
        };
    }

    private async Task<List<Category>> LoadCategoriesAsync()
    {
        if (_categoryRepository is null)
            return [];

        try
        {
            return await _categoryRepository.GetCategoriesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CsvImport: failed to load categories during analysis");
            return [];
        }
    }

    private async Task<Dictionary<string, Category>> BuildHistoricalCategoryMapAsync(
        IReadOnlyList<TransactionDto> dtos,
        IReadOnlyList<Category> categories,
        CancellationToken cancellationToken)
    {
        if (_categoryRepository is null || categories.Count == 0)
            return new Dictionary<string, Category>(StringComparer.Ordinal);

        var categoriesById = categories.ToDictionary(c => c.Id, c => c);
        var uncategorizedIds = categories
            .Where(IsUncategorizedCategory)
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);

        var payees = dtos
            .Select(GetPayee)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Normalize(p!))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (payees.Count == 0)
            return new Dictionary<string, Category>(StringComparer.Ordinal);

        var since = DateTime.Today.AddMonths(-24);
        List<Transaction> transactions;
        try
        {
            transactions = await _transactionRepository
                .GetTransactionsAsync(since, DateTime.MaxValue)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CsvImport: failed to load historical transactions for categorization");
            return new Dictionary<string, Category>(StringComparer.Ordinal);
        }

        var transactionsByKey = transactions
            .Select(t => new { Key = Normalize(t.Titel), Tx = t })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Tx).ToList(), StringComparer.Ordinal);

        var result = new Dictionary<string, Category>(StringComparer.Ordinal);
        foreach (var normalizedPayee in payees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Transaction> matchingTransactions;
            if (transactionsByKey.TryGetValue(normalizedPayee, out var exactList))
            {
                matchingTransactions = exactList;
            }
            else
            {
                matchingTransactions = transactionsByKey
                    .Where(kv => kv.Key.Contains(normalizedPayee, StringComparison.Ordinal))
                    .SelectMany(kv => kv.Value)
                    .ToList();
            }

            if (matchingTransactions.Count == 0)
                continue;

            var categoryCounts = matchingTransactions
                .Where(t => !string.IsNullOrWhiteSpace(t.KategorieId) && !uncategorizedIds.Contains(t.KategorieId))
                .GroupBy(t => t.KategorieId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (categoryCounts.Count == 0)
                continue;

            var top = categoryCounts[0];
            var confidence = (double)top.Count / matchingTransactions.Count;
            if (confidence < HistoricalConfidenceThreshold)
                continue;

            if (!categoriesById.TryGetValue(top.CategoryId, out var category))
                continue;

            result[normalizedPayee] = category;
        }

        return result;
    }

    private async Task<string> ResolveCategoryIdForAnalyzeAsync(
        TransactionDto dto,
        IReadOnlyList<Category> categories,
        IReadOnlyDictionary<string, Category> historicalCategories,
        CancellationToken cancellationToken)
    {
        if (categories.Count == 0)
            return string.Empty;

        if (_categorizationService is not null)
        {
            var category = await _categorizationService.TryCategorizAsync(
                dto,
                categories,
                allowFallback: false,
                strategyFilter: strategy => strategy is not HistoricalCategorizationStrategy,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (category is not null)
                return category.Id;
        }

        var payee = GetPayee(dto);
        if (string.IsNullOrWhiteSpace(payee))
            return string.Empty;

        return historicalCategories.TryGetValue(Normalize(payee), out var historicalCategory)
            ? historicalCategory.Id
            : string.Empty;
    }

    private async Task<List<Transaction>> LoadExistingTransactionsForDtosAsync(IReadOnlyList<TransactionDto> dtos)
    {
        var validDates = dtos
            .Where(d => d?.Buchungsdatum != null && d.Buchungsdatum != default)
            .Select(d => d!.Buchungsdatum.Date)
            .ToList();

        if (validDates.Count == 0)
            return [];

        try
        {
            return await _transactionRepository
                .GetTransactionsAsync(validDates.Min().AddDays(-1), validDates.Max().AddDays(1))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CsvImport: failed to load existing transactions for duplicate check");
            return [];
        }
    }

    private async Task<List<Transaction>> LoadExistingTransactionsForRowsAsync(IReadOnlyList<ImportPreviewRow> rows)
    {
        var validDates = rows
            .Select(r => r.Transaction.Datum.Date)
            .ToList();

        if (validDates.Count == 0)
            return [];

        try
        {
            return await _transactionRepository
                .GetTransactionsAsync(validDates.Min().AddDays(-1), validDates.Max().AddDays(1))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CsvImport: failed to reload transactions for commit duplicate check");
            return [];
        }
    }

    private async Task<string?> ResolveDefaultAccountIdAsync(CancellationToken cancellationToken)
    {
        if (_accountRepository is null)
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var accounts = await _accountRepository.GetAccountsAsync().ConfigureAwait(false);
            return accounts.FirstOrDefault(a => a.SystemKey == Finanzuebersicht.Constants.SystemAccountKeys.Default)?.Id
                ?? accounts.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CsvImport: failed to load accounts for default selection");
            return null;
        }
    }

    private async Task<string> EnsureUncategorizedCategoryAsync(CancellationToken cancellationToken)
    {
        if (_uncategorizedCategoryService is not null)
            return await _uncategorizedCategoryService.EnsureAsync(cancellationToken).ConfigureAwait(false);

        return string.Empty;
    }

    private static Transaction CreateTransaction(TransactionDto dto, string? accountId)
    {
        return new Transaction
        {
            Betrag = Math.Abs(dto.Betrag),
            Datum = dto.Buchungsdatum,
            Titel = BuildTitle(dto),
            Verwendungszweck = dto.Verwendungszweck ?? string.Empty,
            KategorieId = string.Empty,
            Typ = dto.Betrag >= 0 ? TransactionType.Einnahme : TransactionType.Ausgabe,
            AccountId = accountId ?? dto.SourceAccountId
        };
    }

    private static bool IsCommittableStatus(ImportPreviewRowStatus status)
        => status is ImportPreviewRowStatus.Ready or ImportPreviewRowStatus.Uncategorized or ImportPreviewRowStatus.SaveError;

    private static bool ContainsDuplicate(IEnumerable<Transaction> candidates, Transaction transaction)
        => candidates.Any(candidate => IsDuplicate(candidate, transaction));

    private static bool IsDuplicate(Transaction existing, Transaction candidate)
    {
        var from = candidate.Datum.Date.AddDays(-1);
        var to = candidate.Datum.Date.AddDays(1);
        return existing.Datum.Date >= from
            && existing.Datum.Date <= to
            && Math.Abs(existing.Betrag) == Math.Abs(candidate.Betrag)
            && Normalize(existing.Titel) == Normalize(candidate.Titel);
    }

    private bool IsUncategorizedCategory(Category category)
        => _uncategorizedCategoryService?.IsUncategorized(category)
           ?? (category.SystemKey == Finanzuebersicht.Constants.SystemCategoryKeys.Unkategorisiert
               || string.Equals(category.Name, "Unkategorisiert", StringComparison.OrdinalIgnoreCase));

    private static string? GetPayee(TransactionDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.Zahlungsempfaenger)
            ? dto.Zahlungsempfaenger.Trim()
            : !string.IsNullOrWhiteSpace(dto.Zahlungspflichtige)
                ? dto.Zahlungspflichtige.Trim()
                : null;
    }

    private static Transaction CloneTransaction(Transaction transaction)
    {
        return new Transaction
        {
            Id = transaction.Id,
            Betrag = transaction.Betrag,
            Titel = transaction.Titel,
            Datum = transaction.Datum,
            KategorieId = transaction.KategorieId,
            Typ = transaction.Typ,
            DauerauftragId = transaction.DauerauftragId,
            AccountId = transaction.AccountId,
            Verwendungszweck = transaction.Verwendungszweck
        };
    }

    private static string BuildTitle(TransactionDto dto)
    {
        var title = !string.IsNullOrWhiteSpace(dto.Zahlungsempfaenger)
            ? dto.Zahlungsempfaenger
            : !string.IsNullOrWhiteSpace(dto.Zahlungspflichtige)
                ? dto.Zahlungspflichtige
                : dto.Verwendungszweck;

        return string.IsNullOrWhiteSpace(title)
            ? (string.IsNullOrWhiteSpace(dto.Verwendungszweck)
                ? $"Buchung {dto.Buchungsdatum:dd.MM.yyyy} {dto.Betrag:0.00}€"
                : dto.Verwendungszweck)
            : title;
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var lowered = input.Trim().ToLowerInvariant();
        var normalized = lowered.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var cleaned = new string([.. sb.ToString().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))]);
        return Regex.Replace(cleaned, "\\s+", " ").Trim();
    }
}
