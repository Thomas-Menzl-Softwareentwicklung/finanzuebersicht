using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
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
}
