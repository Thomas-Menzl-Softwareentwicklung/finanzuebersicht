using System.Text.Json;
using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases.Sync;

public class CloudSyncOrchestratorTests
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task ApplyRemote_WhenRemoteNewer_OverwritesLocalAccount()
    {
        var transport = new FakeCloudSyncTransport();
        var accountRepository = Substitute.For<IAccountRepository>();
        var local = new Account
        {
            Id = "acc-1",
            Name = "Local",
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        accountRepository.GetAccountsAsync().Returns([local]);
        var metadataStore = CreateEnabledMetadataStore();
        var sut = CreateSut(transport, metadataStore, accountRepository);

        await sut.StartIfEnabledAsync();
        transport.RaiseRecordsChanged([
            CreateAccountRecord("acc-1", "Remote", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc))
        ]);
        await sut.WaitForLastApplyForTestsAsync();

        await accountRepository.Received(1).SaveAccountAsync(Arg.Is<Account>(a =>
            a.Id == "acc-1" &&
            a.Name == "Remote" &&
            a.Source == EntitySources.CloudKit &&
            a.UpdatedAt == new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task ApplyRemote_WhenLocalNewer_KeepsLocalAccount()
    {
        var transport = new FakeCloudSyncTransport();
        var accountRepository = Substitute.For<IAccountRepository>();
        var local = new Account
        {
            Id = "acc-1",
            Name = "Local",
            UpdatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
        };
        accountRepository.GetAccountsAsync().Returns([local]);
        var metadataStore = CreateEnabledMetadataStore();
        var sut = CreateSut(transport, metadataStore, accountRepository);

        await sut.StartIfEnabledAsync();
        transport.RaiseRecordsChanged([
            CreateAccountRecord("acc-1", "Remote", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc))
        ]);
        await sut.WaitForLastApplyForTestsAsync();

        await accountRepository.DidNotReceive().SaveAccountAsync(Arg.Any<Account>());
    }

    [Fact]
    public async Task ApplyRemote_WhenTombstoneRemoteWins_DeletesLocalAndUpsertsTombstone()
    {
        var transport = new FakeCloudSyncTransport();
        var accountRepository = Substitute.For<IAccountRepository>();
        var local = new Account
        {
            Id = "acc-1",
            Name = "Local",
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        accountRepository.GetAccountsAsync().Returns([local]);
        var tombstoneStore = Substitute.For<ISyncTombstoneStore>();
        var metadataStore = CreateEnabledMetadataStore();
        var sut = CreateSut(transport, metadataStore, accountRepository, tombstoneStore: tombstoneStore);

        await sut.StartIfEnabledAsync();
        var deletedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        transport.RaiseRecordsChanged([
            new CloudSyncRecordDto
            {
                EntityType = SyncEntityType.Account,
                Id = "acc-1",
                IsTombstone = true,
                DeletedAt = deletedAt
            }
        ]);
        await sut.WaitForLastApplyForTestsAsync();

        await accountRepository.Received(1).DeleteAccountAsync("acc-1");
        await tombstoneStore.Received(1).UpsertAsync(Arg.Is<SyncTombstone>(t =>
            t.EntityType == SyncEntityType.Account &&
            t.Id == "acc-1" &&
            t.DeletedAt == deletedAt));
    }

    [Fact]
    public async Task ApplyRemote_WhenLocalNewerThanTombstone_SkipsDeleteAndTombstoneUpsert()
    {
        var transport = new FakeCloudSyncTransport();
        var accountRepository = Substitute.For<IAccountRepository>();
        var local = new Account
        {
            Id = "acc-1",
            Name = "Local",
            UpdatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
        };
        accountRepository.GetAccountsAsync().Returns([local]);
        var tombstoneStore = Substitute.For<ISyncTombstoneStore>();
        var metadataStore = CreateEnabledMetadataStore();
        var sut = CreateSut(transport, metadataStore, accountRepository, tombstoneStore: tombstoneStore);

        await sut.StartIfEnabledAsync();
        transport.RaiseRecordsChanged([
            new CloudSyncRecordDto
            {
                EntityType = SyncEntityType.Account,
                Id = "acc-1",
                IsTombstone = true,
                DeletedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            }
        ]);
        await sut.WaitForLastApplyForTestsAsync();

        await accountRepository.DidNotReceive().DeleteAccountAsync(Arg.Any<string>());
        await tombstoneStore.DidNotReceive().UpsertAsync(Arg.Any<SyncTombstone>());
    }

    [Fact]
    public async Task NotifyLocalUpsert_DebouncesRapidCalls_ToSingleEnqueue()
    {
        var transport = new FakeCloudSyncTransport();
        var accountRepository = Substitute.For<IAccountRepository>();
        var first = new Account { Id = "acc-1", Name = "First", UpdatedAt = DateTime.UtcNow };
        var second = new Account { Id = "acc-1", Name = "Second", UpdatedAt = DateTime.UtcNow };
        accountRepository.GetAccountsAsync().Returns([first], [second]);
        var metadataStore = CreateEnabledMetadataStore();
        var sut = CreateSut(transport, metadataStore, accountRepository);

        await sut.NotifyLocalUpsertAsync(SyncEntityType.Account, "acc-1");
        await sut.NotifyLocalUpsertAsync(SyncEntityType.Account, "acc-1");
        await sut.FlushPendingForTestsAsync();

        Assert.Single(transport.EnqueuedUpserts);
        Assert.Equal("Second", JsonSerializer.Deserialize<Account>(transport.EnqueuedUpserts[0].PayloadJson!, PayloadJsonOptions)!.Name);
    }

    [Fact]
    public async Task NotifyLocalDelete_WritesTombstoneAndEnqueuesDelete()
    {
        var transport = new FakeCloudSyncTransport();
        var tombstoneStore = Substitute.For<ISyncTombstoneStore>();
        var metadataStore = CreateEnabledMetadataStore();
        var sut = CreateSut(transport, metadataStore, tombstoneStore: tombstoneStore);

        await sut.NotifyLocalDeleteAsync(SyncEntityType.Account, "acc-1");

        await tombstoneStore.Received(1).UpsertAsync(Arg.Is<SyncTombstone>(t =>
            t.EntityType == SyncEntityType.Account && t.Id == "acc-1"));
        Assert.Single(transport.EnqueuedDeletes);
        Assert.Equal(("acc-1", SyncEntityType.Account), (transport.EnqueuedDeletes[0].id, transport.EnqueuedDeletes[0].type));
    }

    [Fact]
    public async Task SyncNow_FetchesSendsAndUpdatesLastSyncUtc()
    {
        var transport = new FakeCloudSyncTransport();
        var metadata = new SyncMetadata { SyncEnabled = true };
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);
        var sut = CreateSut(transport, metadataStore);

        await sut.SyncNowAsync();

        Assert.True(transport.FetchChangesCalled);
        Assert.True(transport.SendChangesCalled);
        Assert.NotNull(metadata.LastSyncUtc);
        await metadataStore.Received(1).SaveAsync(metadata);
    }

    [Fact]
    public async Task NotifyLocalUpsert_WhenSyncDisabled_IsNoOp()
    {
        var transport = new FakeCloudSyncTransport();
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns([new Account { Id = "acc-1", Name = "Local" }]);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata { SyncEnabled = false });
        var sut = CreateSut(transport, metadataStore, accountRepository);

        await sut.NotifyLocalUpsertAsync(SyncEntityType.Account, "acc-1");
        await sut.FlushPendingForTestsAsync();

        Assert.Empty(transport.EnqueuedUpserts);
        await accountRepository.DidNotReceive().SaveAccountAsync(Arg.Any<Account>());
    }

    private static CloudSyncRecordDto CreateAccountRecord(string id, string name, DateTime updatedAt)
    {
        var account = new Account { Id = id, Name = name, UpdatedAt = updatedAt };
        return new CloudSyncRecordDto
        {
            EntityType = SyncEntityType.Account,
            Id = id,
            UpdatedAt = updatedAt,
            PayloadJson = JsonSerializer.Serialize(account, PayloadJsonOptions),
            IsTombstone = false
        };
    }

    private static ISyncMetadataStore CreateEnabledMetadataStore()
    {
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata { SyncEnabled = true });
        return metadataStore;
    }

    private static CloudSyncOrchestrator CreateSut(
        FakeCloudSyncTransport transport,
        ISyncMetadataStore metadataStore,
        IAccountRepository? accountRepository = null,
        ICategoryRepository? categoryRepository = null,
        ITransactionRepository? transactionRepository = null,
        IRecurringTransactionRepository? recurringRepository = null,
        ISparZielRepository? sparZielRepository = null,
        ISyncTombstoneStore? tombstoneStore = null)
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

        tombstoneStore ??= Substitute.For<ISyncTombstoneStore>();

        return new CloudSyncOrchestrator(
            transport,
            metadataStore,
            tombstoneStore,
            accountRepository,
            categoryRepository,
            transactionRepository,
            recurringRepository,
            sparZielRepository);
    }

    private sealed class FakeCloudSyncTransport : ICloudSyncTransport
    {
        public bool IsSupported => true;
        public event EventHandler<IReadOnlyList<CloudSyncRecordDto>>? RecordsChanged;
        public List<CloudSyncRecordDto> EnqueuedUpserts { get; } = [];
        public List<(SyncEntityType type, string id, DateTime deletedAt)> EnqueuedDeletes { get; } = [];
        public bool FetchChangesCalled { get; private set; }
        public bool SendChangesCalled { get; private set; }

        public void RaiseRecordsChanged(IReadOnlyList<CloudSyncRecordDto> records) =>
            RecordsChanged?.Invoke(this, records);

        public Task<CloudSyncAccountStatus> GetAccountStatusAsync(CancellationToken ct = default) =>
            Task.FromResult(CloudSyncAccountStatus.Available);

        public Task<bool> IsZoneEmptyAsync(CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task EnqueueUpsertAsync(CloudSyncRecordDto record, CancellationToken ct = default)
        {
            EnqueuedUpserts.Add(record);
            return Task.CompletedTask;
        }

        public Task EnqueueDeleteAsync(SyncEntityType type, string id, DateTime deletedAt, CancellationToken ct = default)
        {
            EnqueuedDeletes.Add((type, id, deletedAt));
            return Task.CompletedTask;
        }

        public Task FetchChangesAsync(CancellationToken ct = default)
        {
            FetchChangesCalled = true;
            return Task.CompletedTask;
        }

        public Task SendChangesAsync(CancellationToken ct = default)
        {
            SendChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
