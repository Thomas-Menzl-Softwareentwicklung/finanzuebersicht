using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Dashboard;

public class LoadDashboardMonthUseCase(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IRecurringTransactionRepository recurringTransactionRepository,
    IBudgetRepository budgetRepository)
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = recurringTransactionRepository;
    private readonly IBudgetRepository _budgetRepository = budgetRepository;

    public async Task<DashboardMonthData> ExecuteAsync(DateTime aktuellerMonat, DateTime today, string? accountId = null, CancellationToken cancellationToken = default)
    {
        var kategorien = await _categoryRepository.GetCategoriesAsync();

        var von = aktuellerMonat;
        var bis = aktuellerMonat.AddMonths(1).AddDays(-1);
        var transaktionen = await _transactionRepository.GetTransactionsAsync(von, bis);
        transaktionen = transaktionen.Where(t => !t.IsTransfer).ToList();
        if (!string.IsNullOrWhiteSpace(accountId))
            transaktionen = transaktionen.Where(t => t.AccountId == accountId).ToList();

        var istPrognose = aktuellerMonat > new DateTime(today.Year, today.Month, 1);
        if (istPrognose)
        {
            var dauerauftraege = await _recurringTransactionRepository.GetRecurringTransactionsAsync();
            foreach (var da in dauerauftraege.Where(d => d.Aktiv))
            {
                if (!string.IsNullOrWhiteSpace(accountId) && da.AccountId != accountId)
                    continue;

                if (da.Startdatum <= bis && (!da.Enddatum.HasValue || da.Enddatum.Value >= von))
                {
                    // Only add a forecast transaction for this month if the recurrence actually occurs in the month
                    if (!transaktionen.Any(t => t.DauerauftragId == da.Id) && RecurringScheduleCalculator.OccursInRange(da, von, bis))
                    {
                        transaktionen.Add(new Transaction
                        {
                            Betrag = da.Betrag,
                            Titel = da.Titel,
                            KategorieId = da.KategorieId,
                            AccountId = da.AccountId,
                            Typ = da.Typ,
                            Datum = von,
                            DauerauftragId = da.Id
                        });
                    }
                }
            }
        }

        var gesamtEinnahmen = transaktionen
            .Where(t => t.Typ == TransactionType.Einnahme)
            .Sum(t => Math.Abs(t.Betrag));

        var gesamtAusgaben = transaktionen
            .Where(t => t.Typ == TransactionType.Ausgabe)
            .Sum(t => Math.Abs(t.Betrag));

        // Fallback-Kategorie für nicht zugeordnete Transaktionen
        var unkategorisiert = new Category { Id = string.Empty, Name = "Unkategorisiert", Color = "#8E8E93", Icon = "📁" };

        var kategorieAusgaben = transaktionen
            .Where(t => t.Typ == TransactionType.Ausgabe)
            .GroupBy(t => t.KategorieId)
            .Select(g => new { Key = g.Key, Cat = kategorien.FirstOrDefault(k => k.Id == g.Key) ?? unkategorisiert, Total = g.Sum(t => Math.Abs(t.Betrag)) })
            .Select(x => new CategorySummary
            {
                CategoryId = x.Key,
                CategoryName = x.Cat!.Name,
                Total = (decimal)x.Total,
                Color = x.Cat.Color,
                Icon = x.Cat.Icon
            })
            .OrderByDescending(k => k.Total)
            .ToList();

        var budgets = await _budgetRepository.GetBudgetsAsync() ?? [];
        var isCurrentMonth = aktuellerMonat.Year == today.Year && aktuellerMonat.Month == today.Month;
        var verbleibendeTage = isCurrentMonth
            ? Math.Max(1, (bis.Date - today.Date).Days + 1)
            : 0;

        // Enrich with budget data
        foreach (var cs in kategorieAusgaben)
        {
            var budget = FindBudgetForMonth(budgets, cs.CategoryId, aktuellerMonat.Year, aktuellerMonat.Month);
            if (budget != null)
                cs.BudgetBetrag = budget.Betrag;
        }

        var ausgabenNachKategorie = transaktionen
            .Where(t => t.Typ == TransactionType.Ausgabe)
            .GroupBy(t => t.KategorieId)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Betrag)));

        var budgetHinweise = budgets
            .Select(b => new
            {
                Budget = b,
                Effective = FindBudgetForMonth(budgets, b.KategorieId, aktuellerMonat.Year, aktuellerMonat.Month)
            })
            .Where(x => x.Effective?.Id == x.Budget.Id && x.Budget.Betrag > 0)
            .Select(x =>
            {
                var category = kategorien.FirstOrDefault(k => k.Id == x.Budget.KategorieId) ?? unkategorisiert;
                ausgabenNachKategorie.TryGetValue(x.Budget.KategorieId, out var verbrauch);
                return new BudgetHintSummary
                {
                    CategoryId = x.Budget.KategorieId,
                    CategoryName = category.Name,
                    Color = category.Color,
                    Icon = category.Icon,
                    BudgetBetrag = x.Budget.Betrag,
                    Verbrauch = verbrauch,
                    IstAktuellerMonat = isCurrentMonth,
                    VerbleibendeTage = verbleibendeTage
                };
            })
            .OrderByDescending(b => b.IstKritisch)
            .ThenByDescending(b => b.VerbrauchProzentRaw)
            .ThenBy(b => b.CategoryName)
            .ToList();

        var kategorieEinnahmen = transaktionen
            .Where(t => t.Typ == TransactionType.Einnahme)
            .GroupBy(t => t.KategorieId)
            .Select(g => new { Key = g.Key, Cat = kategorien.FirstOrDefault(k => k.Id == g.Key) ?? unkategorisiert, Total = g.Sum(t => Math.Abs(t.Betrag)) })
            .Select(x => new CategorySummary
            {
                CategoryId = x.Key,
                CategoryName = x.Cat!.Name,
                Total = (decimal)x.Total,
                Color = x.Cat.Color,
                Icon = x.Cat.Icon
            })
            .OrderByDescending(k => k.Total)
            .ToList();

        ApplyCategoryPercentages(kategorieAusgaben, gesamtAusgaben);
        ApplyCategoryPercentages(kategorieEinnahmen, gesamtEinnahmen);

        return new DashboardMonthData
        {
            IstPrognose = istPrognose,
            GesamtEinnahmen = gesamtEinnahmen,
            GesamtAusgaben = gesamtAusgaben,
            Bilanz = gesamtEinnahmen - gesamtAusgaben,
            KategorieAusgaben = kategorieAusgaben,
            KategorieEinnahmen = kategorieEinnahmen,
            BudgetHinweise = budgetHinweise
        };
    }

    private static CategoryBudget? FindBudgetForMonth(IEnumerable<CategoryBudget> budgets, string categoryId, int year, int month)
        => budgets.FirstOrDefault(b => b.KategorieId == categoryId && b.Jahr == year && b.Monat == month)
            ?? budgets.FirstOrDefault(b => b.KategorieId == categoryId && b.Jahr == null && b.Monat == month)
            ?? budgets.FirstOrDefault(b => b.KategorieId == categoryId && b.Jahr == null && b.Monat == null);

    private static void ApplyCategoryPercentages(List<CategorySummary> items, decimal total)
    {
        if (total <= 0)
            return;

        foreach (var item in items)
            item.PercentageAmount = item.Total / total * 100;
    }

}

public class DashboardMonthData
{
    public bool IstPrognose { get; set; }
    public decimal GesamtEinnahmen { get; set; }
    public decimal GesamtAusgaben { get; set; }
    public decimal Bilanz { get; set; }
    public List<CategorySummary> KategorieAusgaben { get; set; } = new List<CategorySummary>();
    public List<CategorySummary> KategorieEinnahmen { get; set; } = new List<CategorySummary>();
    public List<BudgetHintSummary> BudgetHinweise { get; set; } = new List<BudgetHintSummary>();
}