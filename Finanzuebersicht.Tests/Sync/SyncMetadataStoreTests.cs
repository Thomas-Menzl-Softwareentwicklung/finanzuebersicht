using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Tests.Sync;

public class SyncMetadataStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly SyncMetadataStore _store;

    public SyncMetadataStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"sync_metadata_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        _store = new SyncMetadataStore(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsDefault()
    {
        var meta = await _store.GetAsync();
        Assert.False(meta.SyncEnabled);
        Assert.Null(meta.LastSyncUtc);
        Assert.Equal(0, meta.SchemaVersionSeen);
        Assert.Null(meta.LastError);
    }

    [Fact]
    public async Task SaveAndGet_RoundtripsMetadata()
    {
        var expected = new SyncMetadata
        {
            SyncEnabled = true,
            LastSyncUtc = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            SchemaVersionSeen = 2,
            LastError = "timeout"
        };

        await _store.SaveAsync(expected);
        var loaded = await _store.GetAsync();

        Assert.Equal(expected.SyncEnabled, loaded.SyncEnabled);
        Assert.Equal(expected.LastSyncUtc, loaded.LastSyncUtc);
        Assert.Equal(expected.SchemaVersionSeen, loaded.SchemaVersionSeen);
        Assert.Equal(expected.LastError, loaded.LastError);
    }
}
