using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Tests.Sync;

public class CloudKitSyncBridgeCodecTests
{
    [Theory]
    [InlineData(SyncEntityType.Account, "Account")]
    [InlineData(SyncEntityType.Category, "Category")]
    [InlineData(SyncEntityType.Transaction, "Transaction")]
    [InlineData(SyncEntityType.RecurringTransaction, "RecurringTransaction")]
    [InlineData(SyncEntityType.SparZiel, "SparZiel")]
    [InlineData(SyncEntityType.SyncMeta, "SyncMeta")]
    public void ToRecordType_MapsEveryEntityType(SyncEntityType type, string expected)
    {
        Assert.Equal(expected, CloudKitSyncBridgeCodec.ToRecordType(type));
    }

    [Fact]
    public void ToRecordType_RejectsUnknownEnumValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CloudKitSyncBridgeCodec.ToRecordType((SyncEntityType)99));
    }

    [Fact]
    public void TombstoneRecordType_IsTombstone()
    {
        Assert.Equal("Tombstone", CloudKitSyncBridgeCodec.TombstoneRecordType);
    }

    [Theory]
    [InlineData(0, CloudSyncAccountStatus.Available)]
    [InlineData(1, CloudSyncAccountStatus.NoAccount)]
    [InlineData(2, CloudSyncAccountStatus.Restricted)]
    [InlineData(3, CloudSyncAccountStatus.CouldNotDetermine)]
    [InlineData(4, CloudSyncAccountStatus.TemporarilyUnavailable)]
    public void ToAccountStatus_MapsNativeOrdinals(int native, CloudSyncAccountStatus expected)
    {
        Assert.Equal(expected, CloudKitSyncBridgeCodec.ToAccountStatus(native));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(int.MinValue)]
    public void ToAccountStatus_FallsBackToCouldNotDetermine(int native)
    {
        Assert.Equal(CloudSyncAccountStatus.CouldNotDetermine, CloudKitSyncBridgeCodec.ToAccountStatus(native));
    }

    [Theory]
    [InlineData(1, "unsupported OS")]
    [InlineData(2, "not started")]
    [InlineData(3, "invalid argument")]
    [InlineData(4, "CloudKit request failed")]
    [InlineData(5, "unknown failure")]
    [InlineData(108, "CloudKit error 8 (missingEntitlement)")]
    [InlineData(105, "CloudKit error 5 (badContainer)")]
    [InlineData(109, "CloudKit error 9 (notAuthenticated)")]
    [InlineData(126, "CloudKit error 26 (zoneNotFound)")]
    public void FormatNativeStatus_MapsBridgeAndCkErrorCodes(int status, string expected)
    {
        Assert.Equal(expected, CloudKitSyncBridgeCodec.FormatNativeStatus(status));
    }

    [Fact]
    public void DecodeRecords_ReadsUpsertRecord()
    {
        const string json = """
        [
          {
            "entityType": 2,
            "id": "tx-1",
            "updatedAt": "2026-03-04T10:11:12Z",
            "payloadJson": "{\"amount\":12.5}",
            "isTombstone": false
          }
        ]
        """;

        var records = CloudKitSyncBridgeCodec.DecodeRecords(json);

        var record = Assert.Single(records);
        Assert.Equal(SyncEntityType.Transaction, record.EntityType);
        Assert.Equal("tx-1", record.Id);
        Assert.Equal(new DateTime(2026, 3, 4, 10, 11, 12, DateTimeKind.Utc), record.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, record.UpdatedAt!.Value.Kind);
        Assert.Equal("{\"amount\":12.5}", record.PayloadJson);
        Assert.False(record.IsTombstone);
        Assert.Null(record.DeletedAt);
    }

    [Fact]
    public void DecodeRecords_ReadsTombstoneRecord()
    {
        const string json = """
        [
          {
            "entityType": 4,
            "id": "sz-9",
            "isTombstone": true,
            "deletedAt": "2026-03-04T10:11:12.345Z"
          }
        ]
        """;

        var record = Assert.Single(CloudKitSyncBridgeCodec.DecodeRecords(json));

        Assert.Equal(SyncEntityType.SparZiel, record.EntityType);
        Assert.Equal("sz-9", record.Id);
        Assert.True(record.IsTombstone);
        Assert.Null(record.PayloadJson);
        Assert.Equal(
            new DateTime(2026, 3, 4, 10, 11, 12, 345, DateTimeKind.Utc),
            record.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, record.DeletedAt!.Value.Kind);
    }

    [Fact]
    public void DecodeRecords_ReadsSyncMetaRecord()
    {
        const string json = """
        [
          {
            "entityType": 5,
            "id": "sync-meta",
            "updatedAt": "2026-01-01T00:00:00Z",
            "payloadJson": "{\"schemaVersion\":1}",
            "isTombstone": false
          }
        ]
        """;

        var record = Assert.Single(CloudKitSyncBridgeCodec.DecodeRecords(json));
        Assert.Equal(SyncEntityType.SyncMeta, record.EntityType);
        Assert.Equal(CloudSyncSchema.RecordName, record.Id);
        Assert.Equal("{\"schemaVersion\":1}", record.PayloadJson);
        Assert.False(record.IsTombstone);
    }

    [Fact]
    public void DecodeRecords_WhenTombstoneAndNowStampDelete_KeepsTombstoneDeletedAt()
    {
        const string json = """
        [
          { "entityType": 0, "id": "acc-1", "isTombstone": true, "deletedAt": "2026-01-01T00:00:00Z" },
          { "entityType": 0, "id": "acc-1", "isTombstone": true, "deletedAt": "2026-01-02T00:00:00Z" }
        ]
        """;

        var record = Assert.Single(CloudKitSyncBridgeCodec.DecodeRecords(json));
        Assert.True(record.IsTombstone);
        Assert.Equal("acc-1", record.Id);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), record.DeletedAt);
    }

    [Fact]
    public void DecodeRecords_ReadsMixedBatchInOrder()
    {
        const string json = """
        [
          { "entityType": 0, "id": "acc-1", "updatedAt": "2026-01-01T00:00:00Z", "payloadJson": "{}", "isTombstone": false },
          { "entityType": 1, "id": "cat-1", "isTombstone": true, "deletedAt": "2026-01-02T00:00:00Z" }
        ]
        """;

        var records = CloudKitSyncBridgeCodec.DecodeRecords(json);

        Assert.Equal(2, records.Count);
        Assert.Equal("acc-1", records[0].Id);
        Assert.False(records[0].IsTombstone);
        Assert.Equal("cat-1", records[1].Id);
        Assert.True(records[1].IsTombstone);
    }

    [Fact]
    public void DecodeRecords_SkipsUnknownEntityType()
    {
        const string json = """
        [
          { "entityType": 42, "id": "x-1", "payloadJson": "{}", "isTombstone": false },
          { "entityType": 0, "id": "acc-1", "payloadJson": "{}", "isTombstone": false }
        ]
        """;

        var record = Assert.Single(CloudKitSyncBridgeCodec.DecodeRecords(json));
        Assert.Equal("acc-1", record.Id);
    }

    [Fact]
    public void DecodeRecords_SkipsEntriesWithoutId()
    {
        const string json = """
        [
          { "entityType": 0, "payloadJson": "{}", "isTombstone": false },
          { "entityType": 0, "id": "   ", "payloadJson": "{}", "isTombstone": false }
        ]
        """;

        Assert.Empty(CloudKitSyncBridgeCodec.DecodeRecords(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    [InlineData("not json")]
    [InlineData("{\"id\":\"acc-1\"}")]
    public void DecodeRecords_ReturnsEmptyForUnusableInput(string? json)
    {
        Assert.Empty(CloudKitSyncBridgeCodec.DecodeRecords(json));
    }

    [Fact]
    public void ShouldOverwriteServerRecord_LocalNewer_Retries()
    {
        var local = new DateTime(2026, 3, 4, 10, 0, 1, DateTimeKind.Utc);
        var server = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc);

        Assert.True(CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(local, server));
    }

    [Fact]
    public void ShouldOverwriteServerRecord_ServerNewer_DoesNotOverwrite()
    {
        var local = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        var server = new DateTime(2026, 3, 4, 10, 0, 1, DateTimeKind.Utc);

        Assert.False(CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(local, server));
    }

    [Fact]
    public void ShouldOverwriteServerRecord_EqualTimestamps_Retries()
    {
        var stamp = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc);

        Assert.True(CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(stamp, stamp));
    }

    [Fact]
    public void ShouldOverwriteServerRecord_ServerWithoutTimestamp_Retries()
    {
        var local = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc);

        Assert.True(CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(local, null));
        Assert.True(CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(null, null));
    }

    [Fact]
    public void ShouldOverwriteServerRecord_LocalWithoutTimestamp_DoesNotOverwriteStampedServerRecord()
    {
        var server = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc);

        Assert.False(CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(null, server));
    }

    [Fact]
    public void ShouldOverwriteServerRecord_NormalisesTimestampsToUtc()
    {
        var server = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        var localAsLocalTime = server.AddSeconds(1).ToLocalTime();

        Assert.True(CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(localAsLocalTime, server));
        Assert.False(
            CloudKitSyncBridgeCodec.ShouldOverwriteServerRecord(server.AddSeconds(-1).ToLocalTime(), server));
    }
}
