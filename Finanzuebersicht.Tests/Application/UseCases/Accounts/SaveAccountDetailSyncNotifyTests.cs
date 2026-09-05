using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.Accounts;

public class SaveAccountDetailSyncNotifyTests
{
    [Fact]
    public async Task ExecuteAsync_OnSuccessfulSave_SetsUpdatedAtAndNotifiesOrchestrator()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.SaveAccountAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();
        var before = DateTime.UtcNow.AddMinutes(-1);

        var sut = new SaveAccountDetailUseCase(repository, cloudSyncOrchestrator: orchestrator);

        var result = await sut.ExecuteAsync(null, "Tagesgeld", AccountType.Tagesgeld);

        Assert.True(result.IsSuccess);
        var saved = result.Value!;
        Assert.NotNull(saved.UpdatedAt);
        Assert.True(saved.UpdatedAt >= before);
        await orchestrator.Received(1).NotifyLocalUpsertAsync(
            SyncEntityType.Account,
            saved.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenLicenseBlocksCreate_DoesNotNotifyOrchestrator()
    {
        var accounts = Enumerable.Range(1, FreeTierLimits.MaxAccounts)
            .Select(i => new Account { Id = $"a{i}", Name = $"Konto {i}" })
            .ToList();

        var repository = Substitute.For<IAccountRepository>();
        repository.GetAccountsAsync().Returns(accounts);

        var license = Substitute.For<ILicenseService>();
        license.CheckCreateLimit(LimitedResource.Accounts, FreeTierLimits.MaxAccounts)
            .Returns(new LimitCheckResult(false, FreeTierLimits.MaxAccounts, FreeTierLimits.MaxAccounts));

        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();
        var sut = new SaveAccountDetailUseCase(repository, license, orchestrator);

        var result = await sut.ExecuteAsync(null, "Extra", AccountType.Girokonto);

        Assert.False(result.IsSuccess);
        Assert.Equal(UseCaseErrorCode.LicenseLimitReached, result.Error!.Code);
        await repository.DidNotReceive().SaveAccountAsync(Arg.Any<Account>());
        await orchestrator.DidNotReceive().NotifyLocalUpsertAsync(
            Arg.Any<SyncEntityType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
