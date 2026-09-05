using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases.Sync;

public class EnableCloudSyncUseCaseTests
{
    [Fact]
    public async Task Execute_WhenBothHaveData_ReturnsBlockedBothHaveData()
    {
        var transport = CreateSupportedTransport(cloudEmpty: false);
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync()
            .Returns([new Account { Id = "acc-1", Name = "Giro" }]);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata());
        var license = CreateLicensedService();
        var sut = CreateSut(transport, metadataStore, accountRepository, license);

        var result = await sut.ExecuteAsync();

        Assert.Equal(EnableCloudSyncStatus.BlockedBothHaveData, result.Status);
        Assert.Equal("EnableCloudSync.BlockedBothHaveData", result.MessageKey);
        await transport.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await metadataStore.DidNotReceive().SaveAsync(Arg.Is<SyncMetadata>(m => m.SyncEnabled));
    }

    [Fact]
    public async Task Execute_WhenCloudEmptyAndLocalHasData_SeedsAndEnables()
    {
        var transport = CreateSupportedTransport(cloudEmpty: true);
        var accountRepository = Substitute.For<IAccountRepository>();
        var userAccount = new Account { Id = "acc-1", Name = "Giro" };
        accountRepository.GetAccountsAsync().Returns([userAccount]);
        var metadata = new SyncMetadata();
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);
        var license = CreateLicensedService();
        var sut = CreateSut(transport, metadataStore, accountRepository, license);

        var result = await sut.ExecuteAsync();

        Assert.Equal(EnableCloudSyncStatus.Enabled, result.Status);
        Assert.Null(result.MessageKey);
        Assert.True(metadata.SyncEnabled);
        await metadataStore.Received(1).SaveAsync(metadata);
        await transport.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await transport.Received(1).EnqueueUpsertAsync(
            Arg.Is<CloudSyncRecordDto>(r =>
                r.EntityType == SyncEntityType.Account &&
                r.Id == userAccount.Id &&
                !r.IsTombstone &&
                !string.IsNullOrEmpty(r.PayloadJson)),
            Arg.Any<CancellationToken>());
        await transport.DidNotReceive().FetchChangesAsync(Arg.Any<CancellationToken>());
        await transport.Received(1).SendChangesAsync(Arg.Any<CancellationToken>());
        await accountRepository.Received().SaveAccountAsync(Arg.Is<Account>(a =>
            a.Id == userAccount.Id && a.UpdatedAt != null));
    }

    [Fact]
    public async Task Execute_WhenNoEntitlement_ReturnsBlockedNoEntitlement()
    {
        var transport = Substitute.For<ICloudSyncTransport>();
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        var license = Substitute.For<ILicenseService>();
        license.CanUseCloudSync.Returns(false);
        var sut = CreateSut(transport, metadataStore, license: license);

        var result = await sut.ExecuteAsync();

        Assert.Equal(EnableCloudSyncStatus.BlockedNoEntitlement, result.Status);
        Assert.Equal("EnableCloudSync.BlockedNoEntitlement", result.MessageKey);
        await transport.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await metadataStore.DidNotReceive().SaveAsync(Arg.Any<SyncMetadata>());
    }

    [Fact]
    public async Task Execute_WhenUnsupported_ReturnsBlockedUnsupported()
    {
        var transport = Substitute.For<ICloudSyncTransport>();
        transport.IsSupported.Returns(false);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        var license = CreateLicensedService();
        var sut = CreateSut(transport, metadataStore, license: license);

        var result = await sut.ExecuteAsync();

        Assert.Equal(EnableCloudSyncStatus.BlockedUnsupported, result.Status);
        Assert.Equal("EnableCloudSync.BlockedUnsupported", result.MessageKey);
        await transport.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await metadataStore.DidNotReceive().SaveAsync(Arg.Any<SyncMetadata>());
    }

    [Fact]
    public async Task Execute_WhenNoICloud_ReturnsBlockedNoICloud()
    {
        var transport = Substitute.For<ICloudSyncTransport>();
        transport.IsSupported.Returns(true);
        transport.GetAccountStatusAsync(Arg.Any<CancellationToken>())
            .Returns(CloudSyncAccountStatus.NoAccount);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        var license = CreateLicensedService();
        var sut = CreateSut(transport, metadataStore, license: license);

        var result = await sut.ExecuteAsync();

        Assert.Equal(EnableCloudSyncStatus.BlockedNoICloud, result.Status);
        Assert.Equal("EnableCloudSync.BlockedNoICloud", result.MessageKey);
        await transport.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await metadataStore.DidNotReceive().SaveAsync(Arg.Any<SyncMetadata>());
    }

    [Fact]
    public async Task Execute_WhenLocalSystemOnlyAndCloudNotEmpty_PullsWithoutSeed()
    {
        var transport = CreateSupportedTransport(cloudEmpty: false);
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync()
            .Returns([new Account { Id = "sys-1", Name = "Bar", SystemKey = "cash" }]);
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync()
            .Returns([new Category { Id = "cat-sys", Name = "Lebensmittel", SystemKey = "food" }]);
        var metadata = new SyncMetadata();
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);
        var license = CreateLicensedService();
        var sut = CreateSut(transport, metadataStore, accountRepository, license, categoryRepository);

        var result = await sut.ExecuteAsync();

        Assert.Equal(EnableCloudSyncStatus.Enabled, result.Status);
        Assert.True(metadata.SyncEnabled);
        await transport.Received(1).FetchChangesAsync(Arg.Any<CancellationToken>());
        await transport.Received().EnqueueUpsertAsync(
            Arg.Is<CloudSyncRecordDto>(r => r.EntityType == SyncEntityType.SyncMeta && r.Id == CloudSyncSchema.RecordName),
            Arg.Any<CancellationToken>());
        await transport.DidNotReceive().EnqueueUpsertAsync(
            Arg.Is<CloudSyncRecordDto>(r => r.EntityType != SyncEntityType.SyncMeta),
            Arg.Any<CancellationToken>());
        Assert.Equal(CloudSyncSchema.CurrentVersion, metadata.SchemaVersionSeen);
    }

    private static ILicenseService CreateLicensedService()
    {
        var license = Substitute.For<ILicenseService>();
        license.CanUseCloudSync.Returns(true);
        return license;
    }

    private static ICloudSyncTransport CreateSupportedTransport(bool cloudEmpty)
    {
        var transport = Substitute.For<ICloudSyncTransport>();
        transport.IsSupported.Returns(true);
        transport.GetAccountStatusAsync(Arg.Any<CancellationToken>())
            .Returns(CloudSyncAccountStatus.Available);
        transport.IsZoneEmptyAsync(Arg.Any<CancellationToken>()).Returns(cloudEmpty);
        return transport;
    }

    private static EnableCloudSyncUseCase CreateSut(
        ICloudSyncTransport transport,
        ISyncMetadataStore metadataStore,
        IAccountRepository? accountRepository = null,
        ILicenseService? license = null,
        ICategoryRepository? categoryRepository = null,
        ITransactionRepository? transactionRepository = null,
        IRecurringTransactionRepository? recurringRepository = null,
        ISparZielRepository? sparZielRepository = null)
    {
        if (accountRepository is null)
        {
            accountRepository = Substitute.For<IAccountRepository>();
            accountRepository.GetAccountsAsync().Returns([]);
        }

        if (categoryRepository is null)
        {
            categoryRepository = Substitute.For<ICategoryRepository>();
            categoryRepository.GetCategoriesAsync().Returns([]);
        }

        if (transactionRepository is null)
        {
            transactionRepository = Substitute.For<ITransactionRepository>();
            transactionRepository.GetAllTransactionsAsync(Arg.Any<CancellationToken>()).Returns([]);
        }

        if (recurringRepository is null)
        {
            recurringRepository = Substitute.For<IRecurringTransactionRepository>();
            recurringRepository.GetRecurringTransactionsAsync().Returns([]);
        }

        if (sparZielRepository is null)
        {
            sparZielRepository = Substitute.For<ISparZielRepository>();
            sparZielRepository.GetSparZieleAsync().Returns([]);
        }

        license ??= CreateLicensedService();

        return new EnableCloudSyncUseCase(
            transport,
            metadataStore,
            accountRepository,
            categoryRepository,
            transactionRepository,
            recurringRepository,
            sparZielRepository,
            license);
    }
}
