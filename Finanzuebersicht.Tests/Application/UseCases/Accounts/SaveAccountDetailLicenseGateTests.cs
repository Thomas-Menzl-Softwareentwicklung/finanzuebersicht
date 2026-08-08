using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.Accounts;

public class SaveAccountDetailLicenseGateTests
{
    [Fact]
    public async Task ExecuteAsync_BlocksCreateWhenFreeLimitReached()
    {
        var accounts = Enumerable.Range(1, FreeTierLimits.MaxAccounts)
            .Select(i => new Account { Id = $"a{i}", Name = $"Konto {i}" })
            .ToList();

        var repository = Substitute.For<IAccountRepository>();
        repository.GetAccountsAsync().Returns(accounts);

        var license = Substitute.For<ILicenseService>();
        license.CheckCreateLimit(LimitedResource.Accounts, FreeTierLimits.MaxAccounts)
            .Returns(new LimitCheckResult(false, FreeTierLimits.MaxAccounts, FreeTierLimits.MaxAccounts));

        var sut = new SaveAccountDetailUseCase(repository, license);

        var result = await sut.ExecuteAsync(null, "Extra", AccountType.Girokonto);

        Assert.False(result.IsSuccess);
        Assert.Equal(UseCaseErrorCode.LicenseLimitReached, result.Error!.Code);
        Assert.Equal([FreeTierLimits.MaxAccounts, FreeTierLimits.MaxAccounts], result.Error.FormatArgs);
        await repository.DidNotReceive().SaveAccountAsync(Arg.Any<Account>());
    }

    [Fact]
    public async Task ExecuteAsync_AllowsEditWhenOverLimit()
    {
        var existing = new Account { Id = "a1", Name = "Giro" };
        var repository = Substitute.For<IAccountRepository>();
        repository.SaveAccountAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        var license = Substitute.For<ILicenseService>();
        var sut = new SaveAccountDetailUseCase(repository, license);

        var result = await sut.ExecuteAsync(existing, "Giro Neu", AccountType.Girokonto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Giro Neu", result.Value!.Name);
        license.DidNotReceive().CheckCreateLimit(Arg.Any<LimitedResource>(), Arg.Any<int>());
        await repository.Received(1).SaveAccountAsync(Arg.Any<Account>());
    }
}
