using System.Globalization;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Core.Services.ScreenshotDemo;

public static class ScreenshotDemoFixture
{
    public sealed class Snapshot
    {
        public required IReadOnlyList<Account> Accounts { get; init; }
        public required IReadOnlyList<Category> Categories { get; init; }
        public required IReadOnlyList<Transaction> Transactions { get; init; }
        public required IReadOnlyList<RecurringTransaction> RecurringTransactions { get; init; }
        public required IReadOnlyList<CategoryBudget> Budgets { get; init; }
        public required IReadOnlyList<SparZiel> SparZiele { get; init; }
    }

    private sealed class Labels
    {
        public required string GiroAccount { get; init; }
        public required string SavingsAccount { get; init; }
        public required string Lebensmittel { get; init; }
        public required string Transport { get; init; }
        public required string Wohnen { get; init; }
        public required string Unterhaltung { get; init; }
        public required string Gesundheit { get; init; }
        public required string Gehalt { get; init; }
        public required string Sonstiges { get; init; }
        public required string Miete { get; init; }
        public required string Apotheke { get; init; }
        public required string CafeAmMarkt { get; init; }
        public required string DeutscheBahn { get; init; }
        public required string Urlaub { get; init; }
    }

    private static class Ids
    {
        public const string GiroAccount = "screenshot-acc-giro";
        public const string SavingsAccount = "screenshot-acc-sparkonto";

        public const string CatLebensmittel = "screenshot-cat-lebensmittel";
        public const string CatTransport = "screenshot-cat-transport";
        public const string CatWohnen = "screenshot-cat-wohnen";
        public const string CatUnterhaltung = "screenshot-cat-unterhaltung";
        public const string CatGesundheit = "screenshot-cat-gesundheit";
        public const string CatGehalt = "screenshot-cat-gehalt";
        public const string CatSonstiges = "screenshot-cat-sonstiges";

        public const string RecurringMiete = "screenshot-rec-miete";
        public const string SparZielUrlaub = "screenshot-spar-urlaub";
        public const string BudgetLebensmittel = "screenshot-budget-lebensmittel";
    }

    public static Snapshot Create(IClock clock, CultureInfo? culture = null)
    {
        var labels = ResolveLabels(culture);
        var today = clock.Today;
        var categories = CreateCategories(labels);
        var accounts = CreateAccounts(today, labels);
        var categoryIds = categories.ToDictionary(c => c.SystemKey!, c => c.Id);
        var transactions = CreateTransactions(today, categoryIds, Ids.GiroAccount, labels);
        var recurring = CreateRecurring(categoryIds, Ids.GiroAccount, today, labels);
        var budgets = CreateBudgets(categoryIds, today);
        var sparZiele = CreateSparZiele(today, labels);

        return new Snapshot
        {
            Accounts = accounts,
            Categories = categories,
            Transactions = transactions,
            RecurringTransactions = recurring,
            Budgets = budgets,
            SparZiele = sparZiele
        };
    }

    private static Labels ResolveLabels(CultureInfo? culture)
    {
        var isEnglish = string.Equals(
            (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName,
            "en",
            StringComparison.OrdinalIgnoreCase);

        return isEnglish
            ? new Labels
            {
                GiroAccount = "Checking Account",
                SavingsAccount = "Savings Account",
                Lebensmittel = "Groceries",
                Transport = "Transport",
                Wohnen = "Housing",
                Unterhaltung = "Entertainment",
                Gesundheit = "Health",
                Gehalt = "Salary",
                Sonstiges = "Other",
                Miete = "Rent",
                Apotheke = "Pharmacy",
                CafeAmMarkt = "Café at Market",
                DeutscheBahn = "Deutsche Bahn",
                Urlaub = "Vacation 2027"
            }
            : new Labels
            {
                GiroAccount = "Girokonto",
                SavingsAccount = "Sparkonto",
                Lebensmittel = "Lebensmittel",
                Transport = "Transport",
                Wohnen = "Wohnen",
                Unterhaltung = "Unterhaltung",
                Gesundheit = "Gesundheit",
                Gehalt = "Gehalt",
                Sonstiges = "Sonstiges",
                Miete = "Miete",
                Apotheke = "Apotheke",
                CafeAmMarkt = "Café am Markt",
                DeutscheBahn = "Deutsche Bahn",
                Urlaub = "Urlaub 2027"
            };
    }

    private static List<Category> CreateCategories(Labels labels) =>
    [
        new()
        {
            Id = Ids.CatLebensmittel,
            Name = labels.Lebensmittel,
            Icon = "🛒",
            Color = "#34C759",
            Typ = TransactionType.Ausgabe,
            SystemKey = SystemCategoryKeys.Lebensmittel
        },
        new()
        {
            Id = Ids.CatTransport,
            Name = labels.Transport,
            Icon = "🚗",
            Color = "#007AFF",
            Typ = TransactionType.Ausgabe,
            SystemKey = SystemCategoryKeys.Transport
        },
        new()
        {
            Id = Ids.CatWohnen,
            Name = labels.Wohnen,
            Icon = "🏠",
            Color = "#FF9500",
            Typ = TransactionType.Ausgabe,
            SystemKey = SystemCategoryKeys.Wohnen
        },
        new()
        {
            Id = Ids.CatUnterhaltung,
            Name = labels.Unterhaltung,
            Icon = "🎬",
            Color = "#AF52DE",
            Typ = TransactionType.Ausgabe,
            SystemKey = SystemCategoryKeys.Unterhaltung
        },
        new()
        {
            Id = Ids.CatGesundheit,
            Name = labels.Gesundheit,
            Icon = "💊",
            Color = "#FF2D55",
            Typ = TransactionType.Ausgabe,
            SystemKey = SystemCategoryKeys.Gesundheit
        },
        new()
        {
            Id = Ids.CatGehalt,
            Name = labels.Gehalt,
            Icon = "💼",
            Color = "#34C759",
            Typ = TransactionType.Einnahme,
            SystemKey = SystemCategoryKeys.Gehalt
        },
        new()
        {
            Id = Ids.CatSonstiges,
            Name = labels.Sonstiges,
            Icon = "📦",
            Color = "#A2845E",
            Typ = TransactionType.Ausgabe,
            SystemKey = SystemCategoryKeys.Sonstiges
        }
    ];

    private static List<Account> CreateAccounts(DateTime today, Labels labels)
    {
        var openingDate = new DateTime(today.Year, today.Month, 1);

        return
        [
            new Account
            {
                Id = Ids.GiroAccount,
                Name = labels.GiroAccount,
                Type = AccountType.Girokonto,
                SystemKey = SystemAccountKeys.Default,
                OpeningBalance = 2847.50m,
                OpeningBalanceDate = openingDate
            },
            new Account
            {
                Id = Ids.SavingsAccount,
                Name = labels.SavingsAccount,
                Type = AccountType.Tagesgeld,
                OpeningBalance = 12_500m,
                OpeningBalanceDate = openingDate
            }
        ];
    }

    private static List<Transaction> CreateTransactions(
        DateTime today,
        IReadOnlyDictionary<string, string> categoryIds,
        string giroAccountId,
        Labels labels)
    {
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var previousMonth = currentMonth.AddMonths(-1);

        return
        [
            Tx("screenshot-tx-gehalt-cur", 4200m, labels.Gehalt, currentMonth, categoryIds[SystemCategoryKeys.Gehalt], TransactionType.Einnahme, giroAccountId),
            Tx("screenshot-tx-miete-cur", -950m, labels.Miete, currentMonth.AddDays(2), categoryIds[SystemCategoryKeys.Wohnen], TransactionType.Ausgabe, giroAccountId),
            Tx("screenshot-tx-rewe", -67.40m, "REWE", currentMonth.AddDays(4), categoryIds[SystemCategoryKeys.Lebensmittel], TransactionType.Ausgabe, giroAccountId),
            Tx("screenshot-tx-shell", -45.20m, "Shell", currentMonth.AddDays(7), categoryIds[SystemCategoryKeys.Transport], TransactionType.Ausgabe, giroAccountId),
            Tx("screenshot-tx-netflix", -12.99m, "Netflix", currentMonth.AddDays(9), categoryIds[SystemCategoryKeys.Unterhaltung], TransactionType.Ausgabe, giroAccountId),
            Tx("screenshot-tx-apotheke", -23.50m, labels.Apotheke, currentMonth.AddDays(10), categoryIds[SystemCategoryKeys.Gesundheit], TransactionType.Ausgabe, giroAccountId),
            Tx("screenshot-tx-cafe", -8.90m, labels.CafeAmMarkt, today, categoryIds[SystemCategoryKeys.Sonstiges], TransactionType.Ausgabe, giroAccountId),

            Tx("screenshot-tx-gehalt-prev", 4200m, labels.Gehalt, previousMonth, categoryIds[SystemCategoryKeys.Gehalt], TransactionType.Einnahme, giroAccountId),
            Tx("screenshot-tx-miete-prev", -950m, labels.Miete, previousMonth.AddDays(2), categoryIds[SystemCategoryKeys.Wohnen], TransactionType.Ausgabe, giroAccountId),
            Tx("screenshot-tx-edeka", -82.30m, "EDEKA", previousMonth.AddDays(5), categoryIds[SystemCategoryKeys.Lebensmittel], TransactionType.Ausgabe, giroAccountId),
            Tx("screenshot-tx-db", -89.00m, labels.DeutscheBahn, previousMonth.AddDays(12), categoryIds[SystemCategoryKeys.Transport], TransactionType.Ausgabe, giroAccountId)
        ];
    }

    private static Transaction Tx(
        string id,
        decimal amount,
        string title,
        DateTime date,
        string categoryId,
        TransactionType type,
        string accountId) =>
        new()
        {
            Id = id,
            Betrag = amount,
            Titel = title,
            Datum = date,
            KategorieId = categoryId,
            Typ = type,
            AccountId = accountId
        };

    private static List<RecurringTransaction> CreateRecurring(
        IReadOnlyDictionary<string, string> categoryIds,
        string giroAccountId,
        DateTime today,
        Labels labels)
    {
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var currentMonthMiete = currentMonth.AddDays(2);
        var lastRun = currentMonthMiete <= today
            ? currentMonthMiete
            : currentMonthMiete.AddMonths(-1);

        return
        [
            new RecurringTransaction
            {
                Id = Ids.RecurringMiete,
                Betrag = -950m,
                Titel = labels.Miete,
                KategorieId = categoryIds[SystemCategoryKeys.Wohnen],
                AccountId = giroAccountId,
                Typ = TransactionType.Ausgabe,
                Startdatum = today.AddYears(-1),
                LetzteAusfuehrung = lastRun,
                Aktiv = true,
                Interval = RecurrenceInterval.Monthly,
                IntervalFactor = 1,
                ReminderDaysBefore = 3
            }
        ];
    }

    private static List<CategoryBudget> CreateBudgets(
        IReadOnlyDictionary<string, string> categoryIds,
        DateTime today) =>
    [
        new CategoryBudget
        {
            Id = Ids.BudgetLebensmittel,
            KategorieId = categoryIds[SystemCategoryKeys.Lebensmittel],
            Betrag = 400m,
            Monat = today.Month,
            Jahr = today.Year
        }
    ];

    private static List<SparZiel> CreateSparZiele(DateTime today, Labels labels) =>
    [
        new SparZiel
        {
            Id = Ids.SparZielUrlaub,
            Titel = labels.Urlaub,
            Icon = "🏖️",
            ZielBetrag = 5000m,
            AktuellerBetrag = 2800m,
            Faelligkeitsdatum = new DateTime(today.Year + 1, 6, 1),
            MonatlicheSparrate = 250m
        }
    ];
}
