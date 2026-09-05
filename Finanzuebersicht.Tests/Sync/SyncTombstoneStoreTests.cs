using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Tests.Sync;

public class SyncTombstoneStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly SyncTombstoneStore _store;

    public SyncTombstoneStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"sync_tombstone_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        _store = new SyncTombstoneStore(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public async Task Upsert_ThenGetAll_ReturnsTombstone()
    {
        var t = new SyncTombstone
        {
            EntityType = SyncEntityType.Transaction,
            Id = "tx-1",
            DeletedAt = DateTime.UtcNow
        };
        await _store.UpsertAsync(t);
        var all = await _store.GetAllAsync();
        Assert.Contains(all, x => x.Id == "tx-1" && x.EntityType == SyncEntityType.Transaction);
    }

    [Fact]
    public async Task Upsert_ReplacesSameTypeAndId()
    {
        var first = new SyncTombstone
        {
            EntityType = SyncEntityType.Account,
            Id = "acc-1",
            DeletedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var second = new SyncTombstone
        {
            EntityType = SyncEntityType.Account,
            Id = "acc-1",
            DeletedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        await _store.UpsertAsync(first);
        await _store.UpsertAsync(second);

        var all = await _store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(second.DeletedAt, all[0].DeletedAt);
    }

    [Fact]
    public async Task RemoveAsync_RemovesMatchingTypeAndId()
    {
        await _store.UpsertAsync(new SyncTombstone
        {
            EntityType = SyncEntityType.Category,
            Id = "cat-1",
            DeletedAt = DateTime.UtcNow
        });
        await _store.UpsertAsync(new SyncTombstone
        {
            EntityType = SyncEntityType.Category,
            Id = "cat-2",
            DeletedAt = DateTime.UtcNow
        });

        await _store.RemoveAsync(SyncEntityType.Category, "cat-1");

        var all = await _store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("cat-2", all[0].Id);
    }
}
