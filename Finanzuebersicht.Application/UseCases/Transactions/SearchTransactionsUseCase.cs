using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public enum TransactionTypeFilter { Alle, Einnahme, Ausgabe }

public record SearchTransactionsQuery(
    string SearchText = "",
    string? KategorieId = null,
    string? AccountId = null,
    TransactionTypeFilter Typ = TransactionTypeFilter.Alle,
    DateTime? VonDatum = null,
    DateTime? BisDatum = null
);

public class SearchTransactionsResult
{
    public List<TransactionGroup> Gruppen { get; set; } = [];
    public Dictionary<string, string> IconMap { get; set; } = [];
    public Dictionary<string, string> CategoryNameMap { get; set; } = [];
    public Dictionary<string, string> ColorMap { get; set; } = [];
    public Dictionary<string, string> AccountMap { get; set; } = [];
    public int TotalCount { get; set; }
}

public class SearchTransactionsUseCase(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IAccountRepository accountRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IAccountRepository _accountRepository = accountRepository;

    public async Task<SearchTransactionsResult> ExecuteAsync(SearchTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        var von = query.VonDatum ?? DateTime.MinValue;
        var bis = query.BisDatum ?? DateTime.MaxValue;

        if (von > bis)
            (von, bis) = (bis, von);

        var alle = await _transactionRepository.GetTransactionsAsync(von, bis);

        var gefiltert = alle.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var text = query.SearchText.Trim();
            gefiltert = gefiltert.Where(t =>
                t.Titel.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                t.Verwendungszweck.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        if (query.KategorieId != null)
            gefiltert = gefiltert.Where(t => t.KategorieId == query.KategorieId);

        if (query.AccountId != null)
            gefiltert = gefiltert.Where(t => t.AccountId == query.AccountId);

        if (query.Typ == TransactionTypeFilter.Einnahme)
            gefiltert = gefiltert.Where(t => t.Typ == TransactionType.Einnahme);
        else if (query.Typ == TransactionTypeFilter.Ausgabe)
            gefiltert = gefiltert.Where(t => t.Typ == TransactionType.Ausgabe);

        var liste = gefiltert.OrderByDescending(t => t.Datum).ToList();

        // Group by day (same as month list) so cards stay short and CollectionView can
        // virtualize/scroll; month-sized BindableLayout cards rubber-band on iOS.
        var gruppen = liste
            .GroupBy(t => t.Datum.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new TransactionGroup(g.Key, g.OrderByDescending(t => t.Datum)))
            .ToList();

        var categories = await _categoryRepository.GetCategoriesAsync();
        var iconMap = categories.ToDictionary(c => c.Id, c => c.Icon ?? "📁");
        var categoryNameMap = categories.ToDictionary(c => c.Id, c => c.Name);
        var colorMap = categories.ToDictionary(c => c.Id, c => string.IsNullOrWhiteSpace(c.Color) ? "#8E8E93" : c.Color);
        var accounts = await _accountRepository.GetAccountsAsync();
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return new SearchTransactionsResult
        {
            Gruppen = gruppen,
            IconMap = iconMap,
            CategoryNameMap = categoryNameMap,
            ColorMap = colorMap,
            AccountMap = accountMap,
            TotalCount = liste.Count
        };
    }
}
