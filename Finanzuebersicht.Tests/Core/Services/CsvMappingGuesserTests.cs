using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvMappingGuesserTests
{
    [Fact]
    public void Guess_DkbHeaders_FillsAllTargets()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = DkbCsvImportProfile.Instance.Headers,
            DataRows = [],
            AllRows = []
        };
        var g = CsvMappingGuesser.Guess(table);
        Assert.Equal("Buchungsdatum", g.Date);
        Assert.Equal("Betrag (€)", g.Amount);
        Assert.Equal("Zahlungsempfänger*in", g.Title);
        Assert.Equal("Verwendungszweck", g.Purpose);
        Assert.Equal("Umsatztyp", g.AmountSign);
        Assert.Equal("IBAN", g.Iban);
    }

    [Fact]
    public void Guess_DoesNotTreatUmsatztypAsAmount()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Umsatztyp", "Betrag"],
            DataRows = [],
            AllRows = []
        };
        var g = CsvMappingGuesser.Guess(table);
        Assert.Equal("Betrag", g.Amount);
        Assert.Equal("Umsatztyp", g.AmountSign);
    }

    [Fact]
    public void DetectDateFormat_IsoAndGerman()
    {
        Assert.Equal("yyyy-MM-dd", CsvFormatDetector.DetectDateFormat(["2026-03-01", "2026-03-05"]));
        Assert.Equal("dd.MM.yy", CsvFormatDetector.DetectDateFormat(["01.03.26", "05.03.26"]));
        Assert.Equal("dd.MM.yyyy", CsvFormatDetector.DetectDateFormat(["01.03.2026", "05.03.2026"]));
    }

    [Fact]
    public void DetectDecimalStyle_CommaVsPoint()
    {
        Assert.Equal(CsvDecimalStyle.Comma, CsvFormatDetector.DetectDecimalStyle(["1.234,56", "-45,20"]));
        Assert.Equal(CsvDecimalStyle.Point, CsvFormatDetector.DetectDecimalStyle(["1234.56", "-45.20"]));
    }
}
