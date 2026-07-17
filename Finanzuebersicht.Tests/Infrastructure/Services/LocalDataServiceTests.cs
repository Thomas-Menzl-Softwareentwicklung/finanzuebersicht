using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Services;

public class LocalDataServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly LocalDataService _service;

    public LocalDataServiceTests()
    {
        // Eigenes Temp-Verzeichnis pro Test
        _testDir = Path.Combine(Path.GetTempPath(), $"finanz_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        var settings = new SettingsService(Path.Combine(_testDir, "settings.json"));
        settings.Set(SettingsKeys.DataPath, _testDir);
        _service = new LocalDataService(settings, new Finanzuebersicht.Core.Services.SystemClock());
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    #region Categories

    [Fact]
    public async Task SaveAndGetCategory()
    {
        var cat = new Category
        {
            Id = Guid.NewGuid().ToString(),
            Name = "TestKat",
            Icon = "🧪",
            Color = "#FF0000",
            Typ = TransactionType.Ausgabe
        };

        await _service.SaveCategoryAsync(cat);
        var result = await _service.GetCategoriesAsync();

        Assert.Contains(result, c => c.Id == cat.Id && c.Name == "TestKat");
    }

    [Fact]
    public async Task UpdateCategory()
    {
        var cat = new Category { Id = Guid.NewGuid().ToString(), Name = "Alt", Icon = "📦", Color = "#000", Typ = TransactionType.Ausgabe };

        await _service.SaveCategoryAsync(cat);
        cat.Name = "Neu";
        await _service.SaveCategoryAsync(cat);

        var result = await _service.GetCategoriesAsync();
        Assert.Single(result, c => c.Id == cat.Id);
        Assert.Equal("Neu", result.First(c => c.Id == cat.Id).Name);
    }

    [Fact]
    public async Task DeleteCategory()
    {
        var cat = new Category { Id = Guid.NewGuid().ToString(), Name = "Löschbar", Icon = "🗑️", Color = "#000", Typ = TransactionType.Ausgabe };

        await _service.SaveCategoryAsync(cat);
        await _service.DeleteCategoryAsync(cat.Id);

        var result = await _service.GetCategoriesAsync();
        Assert.DoesNotContain(result, c => c.Id == cat.Id);
    }

    #endregion

    #region Transactions

    [Fact]
    public async Task SaveAndGetTransaction_MitDatumfilter()
    {
        var t = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Einkauf",
            Betrag = 42.50m,
            Datum = new DateTime(2026, 2, 15),
            Typ = TransactionType.Ausgabe
        };

        await _service.SaveTransactionAsync(t);

        var feb = await _service.GetTransactionsAsync(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
        Assert.Contains(feb, x => x.Id == t.Id);

        var maerz = await _service.GetTransactionsAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
        Assert.DoesNotContain(maerz, x => x.Id == t.Id);
    }

    [Fact]
    public async Task DeleteTransaction()
    {
        var t = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Löschbar",
            Betrag = 10m,
            Datum = DateTime.Today,
            Typ = TransactionType.Ausgabe
        };

        await _service.SaveTransactionAsync(t);
        await _service.DeleteTransactionAsync(t.Id);

        var result = await _service.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue);
        Assert.DoesNotContain(result, x => x.Id == t.Id);
    }

    #endregion

    #region Recurring Transactions

    [Fact]
    public async Task SaveAndGetRecurringTransaction()
    {
        var r = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Miete",
            Betrag = 800m,
            Typ = TransactionType.Ausgabe,
            Startdatum = new DateTime(2026, 1, 1),
            Aktiv = true
        };

        await _service.SaveRecurringTransactionAsync(r);
        var result = await _service.GetRecurringTransactionsAsync();

        Assert.Contains(result, x => x.Id == r.Id && x.Betrag == 800m);
    }

    [Fact]
    public async Task GeneratePendingRecurringTransactions_ErzeugtTransaktionen()
    {
        var startdatum = DateTime.Today.AddMonths(-2);
        var r = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Abo",
            Betrag = 10m,
            Typ = TransactionType.Ausgabe,
            Startdatum = startdatum,
            Aktiv = true,
            KategorieId = "kat1"
        };

        await _service.SaveRecurringTransactionAsync(r);
        await _service.GeneratePendingRecurringTransactionsAsync();

        var transactions = await _service.GetTransactionsAsync(startdatum, DateTime.Today);
        Assert.True(transactions.Count >= 2, "Mindestens 2 Transaktionen sollten erzeugt worden sein");
        Assert.All(transactions.Where(t => t.DauerauftragId == r.Id), t => Assert.Equal("Abo", t.Titel));
    }

    [Fact]
    public async Task GeneratePendingRecurring_InaktivWirdIgnoriert()
    {
        var r = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Inaktiv",
            Betrag = 10m,
            Typ = TransactionType.Ausgabe,
            Startdatum = DateTime.Today.AddMonths(-2),
            Aktiv = false
        };

        await _service.SaveRecurringTransactionAsync(r);
        await _service.GeneratePendingRecurringTransactionsAsync();

        var transactions = await _service.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue);
        Assert.DoesNotContain(transactions, t => t.DauerauftragId == r.Id);
    }

    #endregion
}
