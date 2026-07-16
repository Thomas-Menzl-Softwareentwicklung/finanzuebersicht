using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases.Transactions;

public class SearchTransactionsUseCaseTests
{
    private static SearchTransactionsUseCase CreateSut(
        IEnumerable<Transaction>? transactions = null,
        IEnumerable<Category>? categories = null,
        IEnumerable<Account>? accounts = null)
    {
        var allTransactions = (transactions ?? []).ToList();
        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(callInfo =>
            {
                var von = callInfo.ArgAt<DateTime>(0);
                var bis = callInfo.ArgAt<DateTime>(1);
                return allTransactions.Where(t => t.Datum >= von && t.Datum <= bis).ToList();
            });

        var categoryRepo = Substitute.For<ICategoryRepository>();
        categoryRepo.GetCategoriesAsync()
            .Returns((categories ?? []).ToList());

        var accountRepo = Substitute.For<IAccountRepository>();
        accountRepo.GetAccountsAsync()
            .Returns((accounts ?? []).ToList());

        return new SearchTransactionsUseCase(transactionRepo, categoryRepo, accountRepo);
    }

    private static Transaction MakeTransaction(
        string id, string titel, string verwendungszweck = "",
        string? kategorieId = null, TransactionType typ = TransactionType.Ausgabe,
        DateTime? datum = null)
    {
        return new Transaction
        {
            Id = id,
            Titel = titel,
            Verwendungszweck = verwendungszweck,
            KategorieId = kategorieId ?? string.Empty,
            Typ = typ,
            Betrag = 10m,
            Datum = datum ?? new DateTime(2026, 1, 15)
        };
    }

    [Fact]
    public async Task ExecuteAsync_SearchText_MatchesTitel()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt", "Lebensmittel"),
            MakeTransaction("2", "Netflix", "Streaming")
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(SearchText: "super"));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Supermarkt", result.Gruppen[0].First().Titel);
    }

    [Fact]
    public async Task ExecuteAsync_SearchText_MatchesVerwendungszweck()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt", "Lebensmittel einkaufen"),
            MakeTransaction("2", "Netflix", "Streaming")
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(SearchText: "lebensmittel"));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Supermarkt", result.Gruppen[0].First().Titel);
    }

    [Fact]
    public async Task ExecuteAsync_SearchText_IsCaseInsensitive()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "NETFLIX", "STREAMING SERVICE")
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(SearchText: "netflix"));

        Assert.Equal(1, result.TotalCount);

        var result2 = await sut.ExecuteAsync(new SearchTransactionsQuery(SearchText: "NETFLIX"));

        Assert.Equal(1, result2.TotalCount);
    }

    [Fact]
    public async Task ExecuteAsync_FilterNachKategorie_OnlyReturnsMatchingCategory()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt", kategorieId: "kat-1"),
            MakeTransaction("2", "Netflix", kategorieId: "kat-2"),
            MakeTransaction("3", "Gehalt", kategorieId: "kat-1", typ: TransactionType.Einnahme)
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(KategorieId: "kat-1"));

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Gruppen.SelectMany(g => g), t => Assert.Equal("kat-1", t.KategorieId));
    }

    [Fact]
    public async Task ExecuteAsync_FilterNachTyp_Einnahme_OnlyReturnsEinnahmen()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt", typ: TransactionType.Ausgabe),
            MakeTransaction("2", "Gehalt", typ: TransactionType.Einnahme),
            MakeTransaction("3", "Freiberuflich", typ: TransactionType.Einnahme)
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(Typ: TransactionTypeFilter.Einnahme));

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Gruppen.SelectMany(g => g), t => Assert.Equal(TransactionType.Einnahme, t.Typ));
    }

    [Fact]
    public async Task ExecuteAsync_FilterNachTyp_Ausgabe_OnlyReturnsAusgaben()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt", typ: TransactionType.Ausgabe),
            MakeTransaction("2", "Gehalt", typ: TransactionType.Einnahme)
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(Typ: TransactionTypeFilter.Ausgabe));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(TransactionType.Ausgabe, result.Gruppen[0].First().Typ);
    }

    [Fact]
    public async Task ExecuteAsync_FilterNachDatumsbereich_ReturnsOnlyTransactionsInRange()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Januar", datum: new DateTime(2026, 1, 15)),
            MakeTransaction("2", "März", datum: new DateTime(2026, 3, 10)),
            MakeTransaction("3", "Juni", datum: new DateTime(2026, 6, 1))
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(
            VonDatum: new DateTime(2026, 2, 1),
            BisDatum: new DateTime(2026, 4, 30)));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("März", result.Gruppen[0].First().Titel);
    }

    [Fact]
    public async Task ExecuteAsync_FilterKombiniert_AppliesAllFilters()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt", kategorieId: "kat-1", typ: TransactionType.Ausgabe,
                datum: new DateTime(2026, 3, 5)),
            MakeTransaction("2", "Gehalt", kategorieId: "kat-2", typ: TransactionType.Einnahme,
                datum: new DateTime(2026, 3, 1)),
            MakeTransaction("3", "Online Shop", verwendungszweck: "Supermarkt Lieferung",
                kategorieId: "kat-1", typ: TransactionType.Ausgabe, datum: new DateTime(2026, 3, 20))
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(
            SearchText: "supermarkt",
            KategorieId: "kat-1",
            Typ: TransactionTypeFilter.Ausgabe,
            VonDatum: new DateTime(2026, 3, 1),
            BisDatum: new DateTime(2026, 3, 31)));

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task ExecuteAsync_NoResults_ReturnsEmptyGroups()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt")
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(SearchText: "gibtsNicht123"));

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Gruppen);
    }

    [Fact]
    public async Task ExecuteAsync_GroupsByMonth_SortedDescending()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Januar", datum: new DateTime(2026, 1, 10)),
            MakeTransaction("2", "März", datum: new DateTime(2026, 3, 5)),
            MakeTransaction("3", "Februar", datum: new DateTime(2026, 2, 20))
        };
        var sut = CreateSut(transactions);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery());

        Assert.Equal(3, result.Gruppen.Count);
        Assert.Equal(new DateTime(2026, 3, 1), result.Gruppen[0].Datum);
        Assert.Equal(new DateTime(2026, 2, 1), result.Gruppen[1].Datum);
        Assert.Equal(new DateTime(2026, 1, 1), result.Gruppen[2].Datum);
    }

    [Fact]
    public async Task ExecuteAsync_IconMap_ContainsCategoryIcons()
    {
        var transactions = new[] { MakeTransaction("1", "Test", kategorieId: "kat-1") };
        var categories = new[] { new Category { Id = "kat-1", Name = "Lebensmittel", Icon = "🛒" } };
        var sut = CreateSut(transactions, categories);

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery());

        Assert.True(result.IconMap.ContainsKey("kat-1"));
        Assert.Equal("🛒", result.IconMap["kat-1"]);
    }

    [Fact]
    public async Task ExecuteAsync_FilterNachKonto_OnlyReturnsMatchingAccount()
    {
        var transactions = new[]
        {
            MakeTransaction("1", "Supermarkt", datum: new DateTime(2026, 1, 1)),
            MakeTransaction("2", "Gehalt", datum: new DateTime(2026, 1, 2))
        };
        transactions[0].AccountId = "acc-1";
        transactions[1].AccountId = "acc-2";

        var sut = CreateSut(transactions, accounts: new[]
        {
            new Account { Id = "acc-1", Name = "Girokonto" },
            new Account { Id = "acc-2", Name = "Tagesgeld" }
        });

        var result = await sut.ExecuteAsync(new SearchTransactionsQuery(AccountId: "acc-1"));

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Gruppen.SelectMany(g => g), t => Assert.Equal("acc-1", t.AccountId));
        Assert.Equal("Girokonto", result.AccountMap["acc-1"]);
    }
}
