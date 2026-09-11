using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvImportFingerprintTests
{
    [Fact]
    public void Matches_DkbHeaders_AgainstBuiltIn()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 3,
            Headers = DkbCsvImportProfile.Instance.Headers,
            DataRows = [],
            AllRows = []
        };
        Assert.True(CsvImportFingerprint.Matches(table, DkbCsvImportProfile.Instance));
    }

    [Fact]
    public void Matches_IgnoresHeaderCaseAndTrim()
    {
        var dkb = DkbCsvImportProfile.Instance;
        var headers = dkb.Headers.Select(h => " " + h.ToUpperInvariant() + " ").ToList();
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = headers,
            DataRows = [],
            AllRows = []
        };
        Assert.True(CsvImportFingerprint.Matches(table, dkb));
    }

    [Fact]
    public void Matches_ExtraColumn_IsDifferentPattern()
    {
        var headers = DkbCsvImportProfile.Instance.Headers.Append("Extra").ToList();
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = headers,
            DataRows = [],
            AllRows = []
        };
        Assert.False(CsvImportFingerprint.Matches(table, DkbCsvImportProfile.Instance));
    }

    [Fact]
    public void DkbProfile_MapsRequiredExportHeaders()
    {
        var p = DkbCsvImportProfile.Instance;
        Assert.Equal("builtin-dkb", p.Id);
        Assert.True(p.IsBuiltIn);
        Assert.Equal("Buchungsdatum", p.Columns.Date);
        Assert.Equal("Betrag (€)", p.Columns.Amount);
        Assert.Equal("Zahlungsempfänger*in", p.Columns.Title);
        Assert.Equal("Verwendungszweck", p.Columns.Purpose);
        Assert.Equal("Umsatztyp", p.Columns.AmountSign);
        Assert.Equal("IBAN", p.Columns.Iban);
        Assert.Equal("dd.MM.yy", p.DateFormat);
        Assert.Equal(CsvDecimalStyle.Comma, p.DecimalStyle);
    }
}
