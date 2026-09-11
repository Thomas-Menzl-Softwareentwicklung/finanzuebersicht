using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Tests.Application.UseCases.Import;

public class CsvImportUseCaseTests
{
    private static CsvImportOrchestrator BuildOrchestrator(
        ITransactionRepository repo,
        ICategoryRepository? catRepo = null,
        CategorizationService? categorizationService = null)
    {
        var logger = Substitute.For<ILogger<CsvImportOrchestrator>>();
        IUncategorizedCategoryService? uncategorized = catRepo is null
            ? null
            : new UncategorizedCategoryService(catRepo);
        return new CsvImportOrchestrator(repo, logger, catRepo, categorizationService, accountRepository: null, uncategorized);
    }

    private static CommitCsvImportUseCase BuildCommit(
        ITransactionRepository repo,
        ICategoryRepository? catRepo = null)
        => new(BuildOrchestrator(repo, catRepo));

    [Fact]
    public async Task Analyze_ValidRecords_BuildsPreviewWithoutSaving()
    {
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var categories = Substitute.For<ICategoryRepository>();
        categories.GetCategoriesAsync().Returns([
            new Category { Id = "cat-food", Name = "Lebensmittel", Icon = "🛒" }
        ]);

        var dto = new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = -10m, Zahlungsempfaenger = "Supermarkt" };
        var preview = await BuildOrchestrator(repo, categories).AnalyzeDtosAsync([dto], accountId: null);

        Assert.True(preview.Success);
        Assert.Single(preview.Rows);
        Assert.Equal(ImportPreviewRowStatus.Uncategorized, preview.Rows[0].Status);
        await repo.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
        await categories.DidNotReceive().SaveCategoryAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task Analyze_DuplicateWithinBatch_MarksLaterRowAsDuplicate()
    {
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var dtos = new TransactionDto[]
        {
            new() { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Shop" },
            new() { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Shop" }
        };

        var preview = await BuildOrchestrator(repo).AnalyzeDtosAsync(dtos, accountId: null);

        Assert.Equal(2, preview.Rows.Count);
        Assert.Equal(ImportPreviewRowStatus.Uncategorized, preview.Rows[0].Status);
        Assert.Equal(ImportPreviewRowStatus.Duplicate, preview.Rows[1].Status);
    }

    [Fact]
    public async Task Commit_OnlySelectedRowsAreSaved()
    {
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var categories = Substitute.For<ICategoryRepository>();
        categories.GetCategoriesAsync().Returns([
            new Category
            {
                Id = "uncat",
                Name = "Unkategorisiert",
                SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Unkategorisiert
            }
        ]);

        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    Id = "r1",
                    IsIncluded = true,
                    Status = ImportPreviewRowStatus.Uncategorized,
                    Transaction = new Transaction
                    {
                        Id = "t1",
                        Datum = DateTime.Today,
                        Betrag = 10m,
                        Titel = "A"
                    }
                },
                new ImportPreviewRow
                {
                    Id = "r2",
                    IsIncluded = true,
                    Status = ImportPreviewRowStatus.Uncategorized,
                    Transaction = new Transaction
                    {
                        Id = "t2",
                        Datum = DateTime.Today,
                        Betrag = 20m,
                        Titel = "B"
                    }
                }
            ]
        };

        var result = await BuildCommit(repo, categories).ExecuteAsync(preview, ["r2"]);

        Assert.Single(result.Imported);
        Assert.Equal("t2", result.Imported[0].Id);
        await repo.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Id == "t2"));
        await repo.DidNotReceive().SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Id == "t1"));
    }

    [Fact]
    public async Task Commit_CreatesFallbackCategoryOnlyDuringCommit()
    {
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var categories = Substitute.For<ICategoryRepository>();
        categories.GetCategoriesAsync().Returns([]);

        Category? savedCategory = null;
        categories.SaveCategoryAsync(NonNullArg.Do<Category>(c => savedCategory = c))
            .Returns(Task.CompletedTask);

        var preview = new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    Id = "r1",
                    IsIncluded = true,
                    Status = ImportPreviewRowStatus.Uncategorized,
                    Transaction = new Transaction
                    {
                        Id = "t1",
                        Datum = DateTime.Today,
                        Betrag = 11m,
                        Titel = "Fallback"
                    }
                }
            ]
        };

        var result = await BuildCommit(repo, categories).ExecuteAsync(preview);

        Assert.Single(result.Imported);
        Assert.NotNull(savedCategory);
        await categories.Received(1).SaveCategoryAsync(Arg.Any<Category>());
        await repo.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.KategorieId == savedCategory!.Id));
    }

    [Fact]
    public async Task AnalyzeDtos_ThenCommit_ImportsValidAndReportsInvalidRows()
    {
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var categories = Substitute.For<ICategoryRepository>();
        categories.GetCategoriesAsync().Returns([
            new Category
            {
                Id = "uncat",
                Name = "Unkategorisiert",
                SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Unkategorisiert
            }
        ]);

        var dtos = new TransactionDto[]
        {
            new() { Buchungsdatum = default, Betrag = 5m },
            new() { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Valid" }
        };

        var orchestrator = BuildOrchestrator(repo, categories);
        var preview = await orchestrator.AnalyzeDtosAsync(dtos, accountId: null);
        var result = await orchestrator.CommitImportAsync(preview);

        Assert.True(result.Success);
        Assert.Single(result.Imported);
        Assert.Equal(1, preview.InvalidCount);
        await repo.Received(1).SaveTransactionAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task AnalyzeDtos_Cancellation_ThrowsOperationCancelled()
    {
        var repo = Substitute.For<ITransactionRepository>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => BuildOrchestrator(repo).AnalyzeDtosAsync([], accountId: null, cts.Token));
    }

    [Fact]
    public async Task Analyze_TableAndProfile_AppliesThenBuildsPreview()
    {
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        Assert.True(CsvTableReader.TryRead("Date,Amount,Text\n2026-03-01,1.00,A\n"u8.ToArray(), out var table, out _));
        var profile = new CsvImportProfile
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

        var orchestrator = BuildOrchestrator(repo);
        var preview = await new AnalyzeCsvImportUseCase(
                orchestrator,
                new PrepareCsvImportUseCase(new InMemoryCsvImportProfileStore(), orchestrator))
            .ExecuteAsync(table!, profile, accountId: null, CancellationToken.None);

        Assert.True(preview.Success);
        Assert.Single(preview.Rows);
        Assert.Equal("A", preview.Rows[0].Transaction.Titel);
    }
}
