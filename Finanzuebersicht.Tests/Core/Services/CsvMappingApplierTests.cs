using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvMappingApplierTests
{
    [Fact]
    public void Apply_DkbProfile_OnSampleFixture_MatchesGoldenAmounts()
    {
        var bytes = File.ReadAllBytes(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            "Finanzuebersicht.Tests", "Services", "test_dkb_sample.csv"));
        Assert.True(CsvTableReader.TryRead(bytes, out var table, out _));
        var dtos = CsvMappingApplier.Apply(table!, DkbCsvImportProfile.Instance);
        Assert.Equal(4, dtos.Count);
        Assert.Equal(2500.00m, dtos.Single(d => d.Zahlungsempfaenger.Contains("Muster")).Betrag);
        Assert.Equal(-7.99m, dtos.Single(d => d.Zahlungsempfaenger.Contains("Streaming")).Betrag);
        Assert.Equal("Gehaltszahlung März", dtos.Single(d => d.Betrag == 2500.00m).Verwendungszweck);
    }

    [Fact]
    public void Apply_TypeColumn_OverridesUnsignedAmount()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Datum", "Betrag", "Typ", "Text"],
            DataRows =
            [
                ["01.03.26", "10,00", "Ausgang", "Shop"],
                ["02.03.26", "20,00", "Eingang", "Pay"]
            ],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ';',
            Headers = table.Headers,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping
            {
                Date = "Datum",
                Amount = "Betrag",
                AmountSign = "Typ",
                Title = "Text"
            }
        };
        var dtos = CsvMappingApplier.Apply(table, profile);
        Assert.Equal(-10.00m, dtos[0].Betrag);
        Assert.Equal(20.00m, dtos[1].Betrag);
    }

    [Fact]
    public void Apply_UnknownType_FallsBackToAmountSign()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Datum", "Betrag", "Typ", "Text"],
            DataRows = [["01.03.26", "-5,00", "???", "X"]],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ';',
            Headers = table.Headers,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping
            {
                Date = "Datum", Amount = "Betrag", AmountSign = "Typ", Title = "Text"
            }
        };
        Assert.Equal(-5.00m, CsvMappingApplier.Apply(table, profile)[0].Betrag);
    }

    [Fact]
    public void Apply_IsoDateAndPointDecimal()
    {
        var table = new CsvTable
        {
            Delimiter = ',',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Date", "Amount", "Text"],
            DataRows = [["2026-03-01", "12.50", "Coffee"]],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ',',
            Headers = table.Headers,
            DateFormat = "yyyy-MM-dd",
            DecimalStyle = CsvDecimalStyle.Point,
            Columns = new CsvColumnMapping { Date = "Date", Amount = "Amount", Title = "Text" }
        };
        var dto = Assert.Single(CsvMappingApplier.Apply(table, profile));
        Assert.Equal(new DateTime(2026, 3, 1), dto.Buchungsdatum.Date);
        Assert.Equal(12.50m, dto.Betrag);
    }

    [Fact]
    public void Apply_IncludesUnparsableDate_WithDefaultBookingDate()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Datum", "Betrag", "Text"],
            DataRows =
            [
                ["nope", "1,00", "A"],
                ["01.03.26", "2,00", "B"]
            ],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ';',
            Headers = table.Headers,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping { Date = "Datum", Amount = "Betrag", Title = "Text" }
        };
        var dtos = CsvMappingApplier.Apply(table, profile);
        Assert.Equal(2, dtos.Count);
        Assert.Equal(default, dtos[0].Buchungsdatum);
        Assert.Equal("A", dtos[0].Zahlungsempfaenger);
        Assert.Equal(1.00m, dtos[0].Betrag);
        Assert.Equal(new DateTime(2026, 3, 1), dtos[1].Buchungsdatum.Date);
        Assert.Equal("B", dtos[1].Zahlungsempfaenger);
    }

    [Fact]
    public void Apply_IncludesUnparsableAmount()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Datum", "Betrag", "Text"],
            DataRows =
            [
                ["01.03.26", "nope", "A"],
                ["02.03.26", "2,00", "B"]
            ],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ';',
            Headers = table.Headers,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping { Date = "Datum", Amount = "Betrag", Title = "Text" }
        };
        var dtos = CsvMappingApplier.Apply(table, profile);
        Assert.Equal(2, dtos.Count);
        Assert.True(dtos[0].HasUnparsableAmount);
        Assert.Equal(new DateTime(2026, 3, 1), dtos[0].Buchungsdatum.Date);
        Assert.Equal("A", dtos[0].Zahlungsempfaenger);
        Assert.False(dtos[1].HasUnparsableAmount);
        Assert.Equal(2.00m, dtos[1].Betrag);
        Assert.Equal("B", dtos[1].Zahlungsempfaenger);
    }
}
