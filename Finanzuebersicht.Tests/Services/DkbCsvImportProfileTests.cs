using System.IO;
using System.Linq;
using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Services
{
    public class DkbCsvImportProfileTests
    {
        [Fact]
        public void Parse_ShouldParseSampleCsv()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var relative = Path.Combine(repoRoot, "Finanzuebersicht.Tests", "Services", "test_dkb_sample.csv");
            Assert.True(File.Exists(relative), $"Test CSV not found: {relative}");

            Assert.True(CsvTableReader.TryRead(File.ReadAllBytes(relative), out var table, out _));
            var txs = CsvMappingApplier.Apply(table!, DkbCsvImportProfile.Instance).ToList();

            Assert.Equal(4, txs.Count);

            var disney = txs.FirstOrDefault(t => (t.Zahlungsempfaenger ?? string.Empty).Contains("Streaming") || (t.Zahlungsempfaenger ?? string.Empty).Contains("Disney"));
            Assert.NotNull(disney);
            Assert.Equal(-7.99m, disney!.Betrag);

            var salary = txs.FirstOrDefault(t => t.Betrag == 2500.00m);
            Assert.NotNull(salary);
            Assert.Equal("Muster, Max", salary!.Zahlungsempfaenger);
        }

        [Fact]
        public void Parse_ShouldHandleMultilineVerwendungszweck()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var relative = Path.Combine(repoRoot, "Finanzuebersicht.Tests", "Services", "test_dkb_multiline.csv");
            Assert.True(File.Exists(relative), $"Test CSV not found: {relative}");

            Assert.True(CsvTableReader.TryRead(File.ReadAllBytes(relative), out var table, out _));
            var txs = CsvMappingApplier.Apply(table!, DkbCsvImportProfile.Instance).ToList();

            Assert.Single(txs);
            var v = txs[0].Verwendungszweck;
            Assert.Contains("Abonnement Linie1", v);
            Assert.Contains("Abonnement Linie2", v);
            Assert.Contains("Zusatzinfo", v);
        }

        [Fact]
        public void Parse_ShouldSkipMalformedRows()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var relative = Path.Combine(repoRoot, "Finanzuebersicht.Tests", "Services", "test_dkb_malformed.csv");
            Assert.True(File.Exists(relative), $"Test CSV not found: {relative}");

            Assert.True(CsvTableReader.TryRead(File.ReadAllBytes(relative), out var table, out _));
            var txs = CsvMappingApplier.Apply(table!, DkbCsvImportProfile.Instance).ToList();

            // one malformed line should be skipped, expect 2 valid transactions
            Assert.Equal(2, txs.Count);
            Assert.Contains(txs, t => t.Betrag == -120.00m);
            Assert.Contains(txs, t => t.Betrag == 300.00m);
        }
    }
}
