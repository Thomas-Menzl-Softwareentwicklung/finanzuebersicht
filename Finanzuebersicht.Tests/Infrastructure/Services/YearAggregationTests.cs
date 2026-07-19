using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Finanzuebersicht.Models;
using Xunit;

namespace Finanzuebersicht.Tests.Services
{
    public class YearAggregationTests : IDisposable
    {
        private readonly string _tempDir;
        public YearAggregationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public async Task GetYearSummary_ReturnsCorrectTotals()
        {
            // Arrange: create categories and transactions
            var categories = new List<Category>
            {
                new Category { Id = "c1", Name = "Essen" },
                new Category { Id = "c2", Name = "Transport" }
            };

            var transactions = new List<Transaction>
            {
                new Transaction { Betrag = 100m, Datum = new DateTime(2025,1,5), KategorieId = "c1", Typ = TransactionType.Ausgabe },
                new Transaction { Betrag = 50m, Datum = new DateTime(2025,2,10), KategorieId = "c2", Typ = TransactionType.Ausgabe },
                new Transaction { Betrag = 25m, Datum = new DateTime(2025,1,20), KategorieId = "c1", Typ = TransactionType.Ausgabe }
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await File.WriteAllTextAsync(Path.Combine(_tempDir, DataFileNames.Categories), JsonSerializer.Serialize(categories, jsonOptions));
            await File.WriteAllTextAsync(Path.Combine(_tempDir, DataFileNames.Transactions), JsonSerializer.Serialize(transactions, jsonOptions));

            // Use SettingsService to point LocalDataService to temp dir
            var settings = new SettingsService(Path.Combine(_tempDir, "settings.json"));
            settings.Set(SettingsKeys.DataPath, _tempDir);
            var ds = new LocalDataService(settings, new Finanzuebersicht.Core.Services.SystemClock());

            // Act
            var summary = await ds.GetYearSummaryAsync(2025);

            // Assert
            Assert.Equal(175m, summary.Total);
            Assert.Equal(2, summary.ByCategory.Count);
            var essen = summary.ByCategory.Find(c => c.CategoryId == "c1");
            Assert.NotNull(essen);
            Assert.Equal(125m, essen.Total);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
