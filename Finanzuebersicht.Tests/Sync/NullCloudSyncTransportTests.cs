using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Infrastructure.Sync;

namespace Finanzuebersicht.Tests.Sync;

public class NullCloudSyncTransportTests
{
    private readonly NullCloudSyncTransport _transport = new();

    [Fact]
    public void IsSupported_IsFalse()
    {
        Assert.False(_transport.IsSupported);
    }

    [Fact]
    public async Task GetAccountStatusAsync_ReturnsNoAccount()
    {
        var status = await _transport.GetAccountStatusAsync();
        Assert.Equal(CloudSyncAccountStatus.NoAccount, status);
    }

    [Fact]
    public async Task IsZoneEmptyAsync_ReturnsTrue()
    {
        var empty = await _transport.IsZoneEmptyAsync();
        Assert.True(empty);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow()
    {
        await _transport.StartAsync();
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        await _transport.StopAsync();
    }

    [Fact]
    public async Task FetchChangesAsync_DoesNotThrow()
    {
        await _transport.FetchChangesAsync();
    }

    [Fact]
    public async Task SendChangesAsync_DoesNotThrow()
    {
        await _transport.SendChangesAsync();
    }

    [Fact]
    public async Task EnqueueUpsertAsync_DoesNotThrow()
    {
        var record = new CloudSyncRecordDto
        {
            EntityType = SyncEntityType.Account,
            Id = "acc-1",
            UpdatedAt = DateTime.UtcNow,
            PayloadJson = "{}"
        };
        await _transport.EnqueueUpsertAsync(record);
    }

    [Fact]
    public async Task EnqueueDeleteAsync_DoesNotThrow()
    {
        await _transport.EnqueueDeleteAsync(
            SyncEntityType.Transaction,
            "tx-1",
            DateTime.UtcNow);
    }
}
