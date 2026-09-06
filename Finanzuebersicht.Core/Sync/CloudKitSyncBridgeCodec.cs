using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finanzuebersicht.Core.Sync;

/// <summary>
/// Wire format between the Swift CKSyncEngine bridge and the managed transport:
/// record type names, the native <see cref="CloudSyncAccountStatus"/> ordinals and the
/// camelCase JSON array the native records callback hands over.
/// Lives in Core so it is unit-testable without a device.
/// </summary>
public static class CloudKitSyncBridgeCodec
{
    /// <summary>CloudKit record type for deletions (carries entityType + deletedAt).</summary>
    public const string TombstoneRecordType = "Tombstone";

    private static readonly JsonSerializerOptions RecordJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>CloudKit record type for an entity. Must stay in sync with CloudKitSyncBridge.swift.</summary>
    public static string ToRecordType(SyncEntityType type) => type switch
    {
        SyncEntityType.Account => "Account",
        SyncEntityType.Category => "Category",
        SyncEntityType.Transaction => "Transaction",
        SyncEntityType.RecurringTransaction => "RecurringTransaction",
        SyncEntityType.SparZiel => "SparZiel",
        SyncEntityType.SyncMeta => "SyncMeta",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown sync entity type.")
    };

    public static bool TryParseRecordType(string? recordType, out SyncEntityType type)
    {
        switch (recordType)
        {
            case "Account": type = SyncEntityType.Account; return true;
            case "Category": type = SyncEntityType.Category; return true;
            case "Transaction": type = SyncEntityType.Transaction; return true;
            case "RecurringTransaction": type = SyncEntityType.RecurringTransaction; return true;
            case "SparZiel": type = SyncEntityType.SparZiel; return true;
            case "SyncMeta": type = SyncEntityType.SyncMeta; return true;
            default: type = default; return false;
        }
    }

    /// <summary>Native returns the <see cref="CloudSyncAccountStatus"/> ordinal; anything else is unknown.</summary>
    public static CloudSyncAccountStatus ToAccountStatus(int nativeStatus) =>
        Enum.IsDefined(typeof(CloudSyncAccountStatus), nativeStatus)
            ? (CloudSyncAccountStatus)nativeStatus
            : CloudSyncAccountStatus.CouldNotDetermine;

    /// <summary>
    /// Native bridge statuses: 1–5 are <c>CKBridgeStatus</c>. Values ≥ 100 are
    /// <c>100 + CKError.Code.rawValue</c> so TestFlight errors stay diagnosable.
    /// </summary>
    public static string FormatNativeStatus(int status) => status switch
    {
        1 => "unsupported OS",
        2 => "not started",
        3 => "invalid argument",
        4 => "CloudKit request failed",
        5 => "unknown failure",
        >= 100 => FormatCkError(status - 100),
        _ => status.ToString()
    };

    private static string FormatCkError(int code) => code switch
    {
        2 => "CloudKit error 2 (partialFailure)",
        4 => "CloudKit error 4 (networkFailure)",
        5 => "CloudKit error 5 (badContainer)",
        8 => "CloudKit error 8 (missingEntitlement)",
        9 => "CloudKit error 9 (notAuthenticated)",
        26 => "CloudKit error 26 (zoneNotFound)",
        _ => $"CloudKit error {code}"
    };

    /// <summary>
    /// Decodes a batch handed over by the native records callback. Malformed input yields an
    /// empty batch — a callback from CloudKit must never throw back into Swift.
    /// </summary>
    public static IReadOnlyList<CloudSyncRecordDto> DecodeRecords(string? recordsJson)
    {
        if (string.IsNullOrWhiteSpace(recordsJson))
        {
            return [];
        }

        NativeRecord[]? decoded;
        try
        {
            decoded = JsonSerializer.Deserialize<NativeRecord[]>(recordsJson, RecordJsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        if (decoded is null || decoded.Length == 0)
        {
            return [];
        }

        var records = new List<CloudSyncRecordDto>(decoded.Length);
        var seenTombstoneIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var native in decoded)
        {
            if (native is null || string.IsNullOrWhiteSpace(native.Id))
            {
                continue;
            }

            if (!Enum.IsDefined(typeof(SyncEntityType), native.EntityType))
            {
                continue;
            }

            if (native.IsTombstone && !seenTombstoneIds.Add(native.Id))
            {
                // Prefer the Tombstone record's deletedAt over a later now-stamped hard-delete DTO.
                continue;
            }

            records.Add(new CloudSyncRecordDto
            {
                EntityType = (SyncEntityType)native.EntityType,
                Id = native.Id,
                UpdatedAt = ToUtc(native.UpdatedAt),
                PayloadJson = native.IsTombstone ? null : native.PayloadJson,
                IsTombstone = native.IsTombstone,
                DeletedAt = ToUtc(native.DeletedAt)
            });
        }

        return records;
    }

    /// <summary>
    /// Conflict resolution for a CloudKit <c>serverRecordChanged</c> save failure: may our
    /// staged write replace the server copy and be retried? Ties go to the local write that is
    /// retrying, so this matches <see cref="LastWriteWins"/>. A strictly newer server record
    /// stays put and reaches the managed side through the next fetch instead.
    /// Mirrored in <c>CloudKitSyncBridge.swift</c>.
    /// </summary>
    public static bool ShouldOverwriteServerRecord(DateTime? localUpdatedAt, DateTime? serverUpdatedAt)
    {
        var server = ToUtc(serverUpdatedAt);
        if (server is null)
        {
            return true;
        }

        var local = ToUtc(localUpdatedAt);
        return local is not null && local.Value >= server.Value;
    }

    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } => value,
        { Kind: DateTimeKind.Local } => value.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
    };

    private sealed class NativeRecord
    {
        [JsonPropertyName("entityType")]
        public int EntityType { get; set; } = -1;

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("payloadJson")]
        public string? PayloadJson { get; set; }

        [JsonPropertyName("isTombstone")]
        public bool IsTombstone { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }
}
