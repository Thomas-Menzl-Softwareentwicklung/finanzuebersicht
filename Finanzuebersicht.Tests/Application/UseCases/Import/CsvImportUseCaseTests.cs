using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Finanzuebersicht.Tests.Application.UseCases.Import;

public class CsvImportUseCaseTests
{
    private static CsvImportOrchestrator BuildOrchestrator(
        IStatementParser parser,
        ITransactionRepository repo,
        ICategoryRepository? catRepo = null,
        CategorizationService? categorizationService = null,
        params IStatementParser[] extraParsers)
    {
        var parsers = extraParsers.Length == 0
            ? new[] { parser }
            : new[] { parser }.Concat(extraParsers).ToArray();
        var logger = Substitute.For<ILogger<CsvImportOrchestrator>>();
        IUncategorizedCategoryService? uncategorized = catRepo is null
            ? null
            : new UncategorizedCategoryService(catRepo);
        return new CsvImportOrchestrator(
            parsers,
            repo,
            logger,
            catRepo,
            categorizationService,
            accountRepository: null,
            uncategorized);
    }

    private static AnalyzeCsvImportUseCase BuildAnalyze(
        IStatementParser parser,
        ITransactionRepository repo,
        ICategoryRepository? catRepo = null)
        => new(BuildOrchestrator(parser, repo, catRepo));

    private static CommitCsvImportUseCase BuildCommit(
        IStatementParser parser,
        ITransactionRepository repo,
        ICategoryRepository? catRepo = null)
        => new(BuildOrchestrator(parser, repo, catRepo));

    [Fact]
    public async Task Analyze_ValidRecords_BuildsPreviewWithoutSaving()
    {
        var parser = Substitute.For<IStatementParser>();
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var categories = Substitute.For<ICategoryRepository>();
        categories.GetCategoriesAsync().Returns([
            new Category { Id = "cat-food", Name = "Lebensmittel", Icon = "🛒" }
        ]);

        parser.Parse(Arg.Any<Stream>()).Returns([
            new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = -10m, Zahlungsempfaenger = "Supermarkt" }
        ]);

        var preview = await BuildAnalyze(parser, repo, categories).ExecuteAsync(new MemoryStream());

        Assert.True(preview.Success);
        Assert.Single(preview.Rows);
        Assert.Equal(ImportPreviewRowStatus.Uncategorized, preview.Rows[0].Status);
        await repo.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
        await categories.DidNotReceive().SaveCategoryAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task Analyze_DuplicateWithinBatch_MarksLaterRowAsDuplicate()
    {
        var parser = Substitute.For<IStatementParser>();
        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        parser.Parse(Arg.Any<Stream>()).Returns([
            new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Shop" },
            new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Shop" }
        ]);

        var preview = await BuildAnalyze(parser, repo).ExecuteAsync(new MemoryStream());

        Assert.Equal(2, preview.Rows.Count);
        Assert.Equal(ImportPreviewRowStatus.Uncategorized, preview.Rows[0].Status);
        Assert.Equal(ImportPreviewRowStatus.Duplicate, preview.Rows[1].Status);
    }

    [Fact]
    public async Task Commit_OnlySelectedRowsAreSaved()
    {
        var parser = Substitute.For<IStatementParser>();
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

        var result = await BuildCommit(parser, repo, categories).ExecuteAsync(preview, ["r2"]);

        Assert.Single(result.Imported);
        Assert.Equal("t2", result.Imported[0].Id);
        await repo.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Id == "t2"));
        await repo.DidNotReceive().SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Id == "t1"));
    }

    [Fact]
    public async Task Commit_CreatesFallbackCategoryOnlyDuringCommit()
    {
        var parser = Substitute.For<IStatementParser>();
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

        var result = await BuildCommit(parser, repo, categories).ExecuteAsync(preview);

        Assert.Single(result.Imported);
        Assert.NotNull(savedCategory);
        await categories.Received(1).SaveCategoryAsync(Arg.Any<Category>());
        await repo.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.KategorieId == savedCategory!.Id));
    }

    [Fact]
    public async Task ImportFromCsv_CompatibilityWrapper_ImportsAndReportsInvalidRows()
    {
        var parser = Substitute.For<IStatementParser>();
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

        parser.Parse(Arg.Any<Stream>()).Returns([
            new TransactionDto { Buchungsdatum = default, Betrag = 5m },
            new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Valid" }
        ]);

        var result = await BuildOrchestrator(parser, repo, categories).ImportFromCsvAsync(new MemoryStream());

        Assert.True(result.Success);
        Assert.Single(result.Imported);
        Assert.Equal(1, result.SkippedMalformed);
        await repo.Received(1).SaveTransactionAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task ImportFromCsv_NoParserMatches_ReturnsMessageKey()
    {
        var parser = Substitute.For<IStatementParser>();
        parser.Parse(Arg.Any<Stream>()).Returns([]);
        var repo = Substitute.For<ITransactionRepository>();

        var result = await BuildOrchestrator(parser, repo).ImportFromCsvAsync(new MemoryStream());

        Assert.False(result.Success);
        Assert.Equal(ImportMessageKeys.NoParserMatched, result.ErrorMessage);
    }

    [Fact]
    public async Task ImportFromCsv_ParserThrows_TriesNextParser()
    {
        var failingParser = Substitute.For<IStatementParser>();
        failingParser.Parse(Arg.Any<Stream>()).Throws(new Exception("parse error"));

        var workingParser = Substitute.For<IStatementParser>();
        workingParser.Parse(Arg.Any<Stream>()).Returns([
            new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = 5m, Zahlungsempfaenger = "OK" }
        ]);

        var repo = Substitute.For<ITransactionRepository>();
        repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var result = await BuildOrchestrator(failingParser, repo, null, null, workingParser)
            .ImportFromCsvAsync(new MemoryStream());

        Assert.True(result.Success);
        Assert.Single(result.Imported);
    }

    [Fact]
    public async Task ImportFromCsv_Cancellation_ThrowsOperationCancelled()
    {
        var parser = Substitute.For<IStatementParser>();
        var repo = Substitute.For<ITransactionRepository>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => BuildOrchestrator(parser, repo).ImportFromCsvAsync(new MemoryStream(), cancellationToken: cts.Token));
    }
}
