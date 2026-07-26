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
        license.When(l => l.EnsureCanCreate(LimitedResource.Accounts, FreeTierLimits.MaxAccounts))
            .Do(_ => throw new FeatureGateException(
                LimitedResource.Accounts,
                FreeTierLimits.MaxAccounts,
                FreeTierLimits.MaxAccounts,
                "limit"));

        var sut = new SaveAccountDetailUseCase(repository, license);

        await Assert.ThrowsAsync<FeatureGateException>(() =>
            sut.ExecuteAsync(null, "Extra", AccountType.Girokonto));

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

        await sut.ExecuteAsync(existing, "Giro Neu", AccountType.Girokonto);

        license.DidNotReceive().EnsureCanCreate(Arg.Any<LimitedResource>(), Arg.Any<int>());
        await repository.Received(1).SaveAccountAsync(Arg.Any<Account>());
    }
}
