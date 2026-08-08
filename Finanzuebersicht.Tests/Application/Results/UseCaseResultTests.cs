using Finanzuebersicht.Application.Results;

namespace Finanzuebersicht.Tests.Application.Results;

public class UseCaseResultTests
{
    [Fact]
    public void Ok_IsSuccess_WithoutError()
    {
        var result = UseCaseResult.Ok();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_IsFailure_WithCode()
    {
        var result = UseCaseResult.Fail(UseCaseErrorCode.AccountNotFound);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(UseCaseErrorCode.AccountNotFound, result.Error!.Code);
        Assert.Empty(result.Error.FormatArgs);
    }

    [Fact]
    public void Fail_WithFormatArgs_PreservesArgs()
    {
        var result = UseCaseResult.Fail(UseCaseErrorCode.LicenseLimitReached, 3, 3);

        Assert.False(result.IsSuccess);
        Assert.Equal(UseCaseErrorCode.LicenseLimitReached, result.Error!.Code);
        Assert.Equal([3, 3], result.Error.FormatArgs);
    }

    [Fact]
    public void GenericOk_CarriesValue()
    {
        var result = UseCaseResult.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GenericFail_HasNoValue()
    {
        var result = UseCaseResult.Fail<int>(UseCaseErrorCode.AccountArchived);

        Assert.False(result.IsSuccess);
        Assert.Equal(default, result.Value);
        Assert.Equal(UseCaseErrorCode.AccountArchived, result.Error!.Code);
    }
}
