using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Tests.Application.UseCases.Import;

public sealed class InMemoryCsvImportProfileStore : ICsvImportProfileStore
{
    private readonly List<CsvImportProfile> _items = [];

    public Task<IReadOnlyList<CsvImportProfile>> GetUserProfilesAsync()
        => Task.FromResult<IReadOnlyList<CsvImportProfile>>(_items.ToList());

    public Task UpsertAsync(CsvImportProfile profile)
    {
        var i = _items.FindIndex(p => p.Id == profile.Id);
        if (i >= 0) _items[i] = profile; else _items.Add(profile);
        return Task.CompletedTask;
    }

    public Task ReplaceAllAsync(IEnumerable<CsvImportProfile> profiles)
    {
        _items.Clear();
        _items.AddRange(profiles);
        return Task.CompletedTask;
    }
}

public class PrepareCsvImportUseCaseTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static byte[] ReadDkbSample() =>
        File.ReadAllBytes(Path.Combine(RepoRoot, "Finanzuebersicht.Tests", "Services", "test_dkb_sample.csv"));

    private static PrepareCsvImportUseCase BuildPrepare(ICsvImportProfileStore store, ITransactionRepository repo)
    {
        var logger = Substitute.For<ILogger<CsvImportOrchestrator>>();
        var orchestrator = new CsvImportOrchestrator(repo, logger);
        return new PrepareCsvImportUseCase(store, orchestrator);
    }

    private static ITransactionRepository EmptyRepo()
    {
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);
        return repo;
    }

    private static CsvImportProfile CommaProfile() => new()
    {
        Id = "user-comma",
        Name = "Comma",
        Delimiter = ',',
        Headers = ["Date", "Amount", "Text"],
        DateFormat = "yyyy-MM-dd",
        DecimalStyle = CsvDecimalStyle.Point,
        Columns = new CsvColumnMapping
        {
            Date = "Date",
            Amount = "Amount",
            Title = "Text"
        }
    };

    [Fact]
    public async Task Prepare_DkbSample_ReturnsPreview()
    {
        var useCase = BuildPrepare(new InMemoryCsvImportProfileStore(), EmptyRepo());
        using var stream = new MemoryStream(ReadDkbSample());

        var result = await useCase.ExecuteAsync(stream, accountId: null, CancellationToken.None);

        Assert.False(result.NeedsMapping);
        Assert.NotNull(result.Preview);
        Assert.Equal(4, result.Preview.Rows.Count);
    }

    [Fact]
    public async Task Prepare_UnknownCommaCsv_NeedsMapping()
    {
        var useCase = BuildPrepare(new InMemoryCsvImportProfileStore(), EmptyRepo());
        using var stream = new MemoryStream("Date,Amount,Text\n2026-03-01,1.00,A\n"u8.ToArray());

        var result = await useCase.ExecuteAsync(stream, accountId: null, CancellationToken.None);

        Assert.True(result.NeedsMapping);
        Assert.Null(result.Preview);
        Assert.Contains("Date", result.Table!.Headers);
    }

    [Fact]
    public async Task Prepare_UserProfile_SkipsMapping()
    {
        var store = new InMemoryCsvImportProfileStore();
        await store.UpsertAsync(CommaProfile());
        var useCase = BuildPrepare(store, EmptyRepo());
        using var stream = new MemoryStream("Date,Amount,Text\n2026-03-01,1.00,A\n"u8.ToArray());

        var result = await useCase.ExecuteAsync(stream, accountId: null, CancellationToken.None);

        Assert.False(result.NeedsMapping);
        Assert.NotNull(result.Preview);
        Assert.Single(result.Preview.Rows);
    }

    [Fact]
    public async Task Prepare_Empty_Error()
    {
        var useCase = BuildPrepare(new InMemoryCsvImportProfileStore(), EmptyRepo());
        using var stream = new MemoryStream(Array.Empty<byte>());

        var result = await useCase.ExecuteAsync(stream, accountId: null, CancellationToken.None);

        Assert.Equal(ImportMessageKeys.CsvNotTabular, result.ErrorMessage);
    }

    [Fact]
    public async Task Prepare_Cancelled_Throws()
    {
        var useCase = BuildPrepare(new InMemoryCsvImportProfileStore(), EmptyRepo());
        using var stream = new MemoryStream(ReadDkbSample());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => useCase.ExecuteAsync(stream, accountId: null, cts.Token));
    }

    [Fact]
    public async Task Prepare_SavedHeaderRowIndex_ReturnsPreviewNotNeedsMapping()
    {
        var store = new InMemoryCsvImportProfileStore();
        await store.UpsertAsync(new CsvImportProfile
        {
            Id = "user-header-override",
            Name = "Override",
            Delimiter = ';',
            Headers = ["Datum", "Betrag", "Text"],
            HeaderRowIndex = 1,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping
            {
                Date = "Datum",
                Amount = "Betrag",
                Title = "Text"
            }
        });
        var useCase = BuildPrepare(store, EmptyRepo());
        using var stream = new MemoryStream("skip-me;x;y\nDatum;Betrag;Text\n01.03.26;1,00;A\n"u8.ToArray());

        var result = await useCase.ExecuteAsync(stream, accountId: null, CancellationToken.None);

        Assert.False(result.NeedsMapping);
        Assert.NotNull(result.Preview);
        Assert.Equal(1, result.Table!.HeaderRowIndex);
        Assert.Equal(["Datum", "Betrag", "Text"], result.Table.Headers);
        var row = Assert.Single(result.Preview.Rows);
        Assert.Equal("A", row.Transaction.Titel);
    }

    [Fact]
    public async Task Prepare_UnparsableAmount_MarksPreviewRowInvalid()
    {
        var store = new InMemoryCsvImportProfileStore();
        await store.UpsertAsync(CommaProfile());
        var useCase = BuildPrepare(store, EmptyRepo());
        using var stream = new MemoryStream("Date,Amount,Text\n2026-03-01,not-a-number,A\n2026-03-02,1.00,B\n"u8.ToArray());

        var result = await useCase.ExecuteAsync(stream, accountId: null, CancellationToken.None);

        Assert.False(result.NeedsMapping);
        Assert.NotNull(result.Preview);
        Assert.Equal(2, result.Preview.Rows.Count);
        Assert.Equal(ImportPreviewRowStatus.Invalid, result.Preview.Rows[0].Status);
        Assert.False(result.Preview.Rows[0].IsIncluded);
        Assert.Equal(ImportMessageKeys.UnparsableAmount, result.Preview.Rows[0].StatusMessage);
        Assert.NotEqual(ImportPreviewRowStatus.Invalid, result.Preview.Rows[1].Status);
        Assert.Equal("B", result.Preview.Rows[1].Transaction.Titel);
    }
}
