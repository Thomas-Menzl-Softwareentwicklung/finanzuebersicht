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
