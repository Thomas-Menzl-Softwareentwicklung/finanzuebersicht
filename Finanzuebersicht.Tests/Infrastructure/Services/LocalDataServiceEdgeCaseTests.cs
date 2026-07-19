using System.Text.Json;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Services;

public class LocalDataServiceEdgeCaseTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalDataService _service;

    public LocalDataServiceEdgeCaseTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"finanz_edge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new SettingsService(Path.Combine(_tempDir, "settings.json"));
        settings.Set(SettingsKeys.DataPath, _tempDir);
        _service = new LocalDataService(settings, new Finanzuebersicht.Core.Services.SystemClock());
    }

    public void Dispose()
    {
        _service.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task LoadAsync_KorrupteJSON_WirftDataCorruptionException()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "categories.json"), "{ not valid json [[[");

        await Assert.ThrowsAsync<DataCorruptionException>(
            () => _service.GetCategoriesAsync());
    }

    [Fact]
    public async Task LoadAsync_DateiExistiertNicht_GibtLeereListeZurueck()
    {
        // Datei existiert nicht → leere Liste (kein Fehler)
        var result = await _service.GetCategoriesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMonthSummary_ErmitteltKorrekteWerte()
    {
        var catId = Guid.NewGuid().ToString();
        var cat = new Category { Id = catId, Name = "Lebensmittel", Icon = "🛒", Color = "#FF0000", Typ = TransactionType.Ausgabe };
        await _service.SaveCategoryAsync(cat);

        await _service.SaveTransactionAsync(new Transaction
        {
            Betrag = 50m, Datum = new DateTime(2025, 6, 10),
            KategorieId = catId, Typ = TransactionType.Ausgabe
        });
        await _service.SaveTransactionAsync(new Transaction
        {
            Betrag = 30m, Datum = new DateTime(2025, 6, 20),
            KategorieId = catId, Typ = TransactionType.Ausgabe
        });
        await _service.SaveTransactionAsync(new Transaction
        {
            Betrag = 100m, Datum = new DateTime(2025, 6, 15),
            KategorieId = catId, Typ = TransactionType.Einnahme
        });

        var summary = await _service.GetMonthSummaryAsync(2025, 6);

        Assert.Equal(80m, summary.Total);
        Assert.Single(summary.ByCategory);
        Assert.Equal("Lebensmittel", summary.ByCategory[0].CategoryName);
        Assert.Equal(80m, summary.ByCategory[0].Total);
    }

    [Fact]
    public async Task GeneratePending_MehrfachaufrufIstIdempotent()
    {
        var r = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Idempotenz-Test",
            Betrag = 10m,
            Typ = TransactionType.Ausgabe,
            Startdatum = DateTime.Today.AddMonths(-1),
            Aktiv = true
        };
        await _service.SaveRecurringTransactionAsync(r);

        await _service.GeneratePendingRecurringTransactionsAsync();
        var countAfterFirst = (await _service.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue))
            .Count(t => t.DauerauftragId == r.Id);

        // Zweiter Aufruf darf keine doppelten Transaktionen erzeugen
        await _service.GeneratePendingRecurringTransactionsAsync();
        var countAfterSecond = (await _service.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue))
            .Count(t => t.DauerauftragId == r.Id);

        Assert.Equal(countAfterFirst, countAfterSecond);
    }

    [Fact]
    public async Task GeneratePending_EnddatumInVergangenheitWirdIgnoriert()
    {
        var r = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Titel = "Abgelaufen",
            Betrag = 5m,
            Typ = TransactionType.Ausgabe,
            Startdatum = DateTime.Today.AddMonths(-3),
            Enddatum = DateTime.Today.AddMonths(-2),
            Aktiv = true
        };
        await _service.SaveRecurringTransactionAsync(r);
        await _service.GeneratePendingRecurringTransactionsAsync();

        var transactions = await _service.GetTransactionsAsync(DateTime.MinValue, DateTime.MaxValue);
        Assert.DoesNotContain(transactions, t => t.DauerauftragId == r.Id);
    }
}
