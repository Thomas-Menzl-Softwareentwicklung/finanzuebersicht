using System.Text;
using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvTableReaderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static byte[] ReadFixture(string name) =>
        File.ReadAllBytes(Path.Combine(RepoRoot, "Finanzuebersicht.Tests", "Services", name));

    [Fact]
    public void TryRead_DkbSample_FindsHeaderAfterPreamble()
    {
        Assert.True(CsvTableReader.TryRead(ReadFixture("test_dkb_sample.csv"), out var table, out var error));
        Assert.Null(error);
        Assert.Equal(';', table!.Delimiter);
        Assert.Equal("Buchungsdatum", table.Headers[0]);
        Assert.Equal(4, table.DataRows.Count);
        Assert.Contains(table.Headers, h => h.StartsWith("Betrag", StringComparison.Ordinal));
    }

    [Fact]
    public void TryRead_MultilineQuotedField_KeepsNewlinesAndSemicolon()
    {
        Assert.True(CsvTableReader.TryRead(ReadFixture("test_dkb_multiline.csv"), out var table, out _));
        Assert.Single(table!.DataRows);
        var purpose = table.DataRows[0][5];
        Assert.Contains("Abonnement Linie1", purpose);
        Assert.Contains("Abonnement Linie2", purpose);
        Assert.Contains("Zusatzinfo", purpose);
    }

    [Fact]
    public void TryRead_CommaDelimited_DetectsComma()
    {
        var csv = "Date,Amount,Text\n2026-03-01,12.50,Coffee\n";
        Assert.True(CsvTableReader.TryRead(Encoding.UTF8.GetBytes(csv), out var table, out _));
        Assert.Equal(',', table!.Delimiter);
        Assert.Equal(["Date", "Amount", "Text"], table.Headers);
        Assert.Equal("12.50", table.DataRows[0][1]);
    }

    [Fact]
    public void TryRead_TabDelimited_DetectsTab()
    {
        var csv = "Date\tAmount\n01.03.26\t10,00\n";
        Assert.True(CsvTableReader.TryRead(Encoding.UTF8.GetBytes(csv), out var table, out _));
        Assert.Equal('\t', table!.Delimiter);
    }

    [Fact]
    public void TryRead_Windows1252Umlauts_DecodesWhenNotUtf8()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var latin1 = Encoding.GetEncoding(1252);
        var csv = "Datum;Betrag;Text\n01.03.26;-10,00;Gebühr\n";
        Assert.True(CsvTableReader.TryRead(latin1.GetBytes(csv), out var table, out _));
        Assert.Equal("Gebühr", table!.DataRows[0][2]);
    }

    [Fact]
    public void WithHeaderRow_OverridesHeuristic()
    {
        var csv = "skip-me;x;y\nDatum;Betrag;Text\n01.03.26;1,00;A\n";
        Assert.True(CsvTableReader.TryRead(Encoding.UTF8.GetBytes(csv), out var table, out _));
        var overridden = table!.WithHeaderRow(1);
        Assert.Equal("Datum", overridden.Headers[0]);
        Assert.Single(overridden.DataRows);
    }

    [Fact]
    public void TryRead_EmptyBytes_Fails()
    {
        Assert.False(CsvTableReader.TryRead([], out var table, out var error));
        Assert.Null(table);
        Assert.Equal(ImportMessageKeys.CsvNotTabular, error);
    }
}
