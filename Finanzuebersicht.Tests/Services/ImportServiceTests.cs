using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Finanzuebersicht.Tests.Services
{
    public class ImportServiceTests
    {
        private static ImportService BuildService(
            IStatementParser parser,
            ITransactionRepository repo,
            ICategoryRepository? catRepo = null,
            CategorizationService? categorizationService = null)
        {
            var logger = Substitute.For<ILogger<ImportService>>();
            return new ImportService([parser], repo, logger, catRepo, categorizationService);
        }

        [Fact]
        public async Task AnalyzeCsvAsync_ValidRecords_BuildsPreviewWithoutSaving()
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

            var preview = await BuildService(parser, repo, categories).AnalyzeCsvAsync(new MemoryStream());

            Assert.True(preview.Success);
            Assert.Single(preview.Rows);
            Assert.Equal(ImportPreviewRowStatus.Uncategorized, preview.Rows[0].Status);
            await repo.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
            await categories.DidNotReceive().SaveCategoryAsync(Arg.Any<Category>());
        }

        [Fact]
        public async Task AnalyzeCsvAsync_DuplicateWithinBatch_MarksLaterRowAsDuplicate()
        {
            var parser = Substitute.For<IStatementParser>();
            var repo = Substitute.For<ITransactionRepository>();
            repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns([]);

            parser.Parse(Arg.Any<Stream>()).Returns([
                new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Shop" },
                new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Shop" }
            ]);

            var preview = await BuildService(parser, repo).AnalyzeCsvAsync(new MemoryStream());

            Assert.Equal(2, preview.Rows.Count);
            Assert.Equal(ImportPreviewRowStatus.Uncategorized, preview.Rows[0].Status);
            Assert.Equal(ImportPreviewRowStatus.Duplicate, preview.Rows[1].Status);
        }

        [Fact]
        public async Task CommitImportAsync_OnlySelectedRowsAreSaved()
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

            var service = BuildService(parser, repo, categories);
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

            var result = await service.CommitImportAsync(preview, ["r2"]);

            Assert.Single(result.Imported);
            Assert.Equal("t2", result.Imported[0].Id);
            await repo.Received(1).SaveTransactionAsync(Arg.Is<Transaction>(t => t != null && t.Id == "t2"));
            await repo.DidNotReceive().SaveTransactionAsync(Arg.Is<Transaction>(t => t != null && t.Id == "t1"));
        }

        [Fact]
        public async Task CommitImportAsync_CreatesFallbackCategoryOnlyDuringCommit()
        {
            var parser = Substitute.For<IStatementParser>();
            var repo = Substitute.For<ITransactionRepository>();
            repo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns([]);

            var categories = Substitute.For<ICategoryRepository>();
            categories.GetCategoriesAsync().Returns([]);

            Category? savedCategory = null;
            categories.SaveCategoryAsync(Arg.Do<Category>(c => savedCategory = c))
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

            var result = await BuildService(parser, repo, categories).CommitImportAsync(preview);

            Assert.Single(result.Imported);
            Assert.NotNull(savedCategory);
            await categories.Received(1).SaveCategoryAsync(Arg.Any<Category>());
            await repo.Received(1).SaveTransactionAsync(Arg.Is<Transaction>(t =>
                t != null && t.KategorieId == savedCategory!.Id));
        }

        [Fact]
        public async Task ImportFromCsvAsync_CompatibilityWrapper_ImportsAndReportsInvalidRows()
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

            var result = await BuildService(parser, repo, categories).ImportFromCsvAsync(new MemoryStream());

            Assert.True(result.Success);
            Assert.Single(result.Imported);
            Assert.Equal(1, result.SkippedMalformed);
            await repo.Received(1).SaveTransactionAsync(Arg.Any<Transaction>());
        }

        [Fact]
        public async Task ImportFromCsvAsync_NoParserMatches_ReturnsError()
        {
            var parser = Substitute.For<IStatementParser>();
            parser.Parse(Arg.Any<Stream>()).Returns([]);
            var repo = Substitute.For<ITransactionRepository>();

            var result = await BuildService(parser, repo).ImportFromCsvAsync(new MemoryStream());

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task ImportFromCsvAsync_ParserThrows_TriesNextParser()
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

            var logger = Substitute.For<ILogger<ImportService>>();
            var svc = new ImportService([failingParser, workingParser], repo, logger);

            var result = await svc.ImportFromCsvAsync(new MemoryStream());

            Assert.True(result.Success);
            Assert.Single(result.Imported);
        }

        [Fact]
        public async Task ImportFromCsvAsync_Cancellation_ThrowsOperationCancelled()
        {
            var parser = Substitute.For<IStatementParser>();
            var repo = Substitute.For<ITransactionRepository>();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var logger = Substitute.For<ILogger<ImportService>>();
            var svc = new ImportService([parser], repo, logger);

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => svc.ImportFromCsvAsync(new MemoryStream(), cancellationToken: cts.Token));
        }
    }
}
