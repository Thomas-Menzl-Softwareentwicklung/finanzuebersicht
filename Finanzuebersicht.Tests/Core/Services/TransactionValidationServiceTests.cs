using System.Globalization;

namespace Finanzuebersicht.Tests.Services;

public class TransactionValidationServiceTests
{
    private readonly TransactionValidationService _service = new();

    [Fact]
    public void TryValidate_InvalidAmountFormat_ReturnsFalseAndError()
    {
        var result = _service.TryValidate(
            "abc",
            "Test",
            hasCategory: true,
            CultureInfo.GetCultureInfo("de-DE"),
            out _,
            out var error);

        Assert.False(result);
        Assert.Equal(TransactionInputError.InvalidAmountFormat, error);
    }

    [Fact]
    public void TryValidate_AmountLessOrEqualZero_ReturnsFalseAndError()
    {
        var result = _service.TryValidate(
            "0",
            "Test",
            hasCategory: true,
            CultureInfo.GetCultureInfo("de-DE"),
            out _,
            out var error);

        Assert.False(result);
        Assert.Equal(TransactionInputError.AmountMustBePositive, error);
    }

    [Fact]
    public void TryValidate_TitleMissing_ReturnsFalseAndError()
    {
        var result = _service.TryValidate(
            "12,50",
            "   ",
            hasCategory: true,
            CultureInfo.GetCultureInfo("de-DE"),
            out _,
            out var error);

        Assert.False(result);
        Assert.Equal(TransactionInputError.TitleRequired, error);
    }

    [Fact]
    public void TryValidate_CategoryMissing_ReturnsFalseAndError()
    {
        var result = _service.TryValidate(
            "12,50",
            "Test",
            hasCategory: false,
            CultureInfo.GetCultureInfo("de-DE"),
            out _,
            out var error);

        Assert.False(result);
        Assert.Equal(TransactionInputError.CategoryRequired, error);
    }

    [Fact]
    public void TryValidate_ValidInput_ReturnsTrueAndAmount()
    {
        var result = _service.TryValidate(
            "12,50",
            "Test",
            hasCategory: true,
            CultureInfo.GetCultureInfo("de-DE"),
            out var amount,
            out var error);

        Assert.True(result);
        Assert.Equal(12.50m, amount);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_AcceptsCommaAmount_WhenCultureUsesDot()
    {
        var result = _service.TryValidate(
            "12,50",
            "Coffee",
            hasCategory: true,
            CultureInfo.GetCultureInfo("en-US"),
            out var amount,
            out var error);

        Assert.True(result);
        Assert.Equal(12.50m, amount);
        Assert.Null(error);
    }
}
