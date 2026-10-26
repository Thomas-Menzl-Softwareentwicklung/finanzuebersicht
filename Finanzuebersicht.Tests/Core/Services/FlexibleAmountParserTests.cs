using System.Globalization;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Tests.Core.Services;

public class FlexibleAmountParserTests
{
    [Theory]
    [InlineData("3.50", 3.50)]
    [InlineData("3,50", 3.50)]
    [InlineData("3,5", 3.5)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("5.00", 5.00)]
    [InlineData("5,00", 5.00)]
    [InlineData("-3,50", -3.50)]
    [InlineData("-3.50", -3.50)]
    [InlineData("-1.234,56", -1234.56)]
    [InlineData("\u22123,50", -3.50)]
    [InlineData("\u2013120,50", -120.50)]
    [InlineData("\u20141.234,56", -1234.56)]
    [InlineData("\uFF0D3.50", -3.50)]
    [InlineData("120,50-", -120.50)]
    [InlineData("1.234,56-", -1234.56)]
    [InlineData("250,00\u2212", -250.00)]
    [InlineData("\u200e-800,00", -800.00)]
    [InlineData("-800,00\u200e", -800.00)]
    [InlineData("\u2066-800,00\u2069", -800.00)]
    [InlineData("\u2066\u2212800,00\u2069", -800.00)]
    [InlineData("\ufeff-800,00", -800.00)]
    [InlineData("-1\u202f234,56", -1234.56)]
    public void TryParse_AcceptsCommaAndDot(string input, decimal expected)
    {
        Assert.True(FlexibleAmountParser.TryParse(input, out var amount));
        Assert.Equal(expected, amount);
    }

    [Fact]
    public void ToInvariantAmountText_RewritesGermanDecimal()
    {
        Assert.Equal("3.50", FlexibleAmountParser.ToInvariantAmountText("3,50"));
    }

    [Fact]
    public void ToDisplayAmountText_UsesCurrentCultureDecimalSeparator()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("3,50", FlexibleAmountParser.ToDisplayAmountText("3.50"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
