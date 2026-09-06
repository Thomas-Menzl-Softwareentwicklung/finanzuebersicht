#if IOS || MACCATALYST
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Services;

namespace Finanzuebersicht.Platforms.iOS;

/// <summary>
/// <see cref="ICloudSyncTransport"/> on top of CKSyncEngine. CloudKit's sync engine is
/// Swift-only, so all calls go through <c>CloudKitSyncBridge.swift</c> (@_cdecl, linked
/// statically as libCloudKitSyncBridge.a).
/// </summary>
public sealed class CloudKitSyncTransport : ICloudSyncTransport
{
    private const string NativeLibrary = "__Internal";

    /// <summary>Swift formats and parses UTC ISO-8601 with millisecond precision.</summary>
    private const string IsoUtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private static readonly object InstanceGate = new();
    private static CloudKitSyncTransport? _active;

    private bool? _isSupported;

    public CloudKitSyncTransport()
    {
        lock (InstanceGate)
        {
            _active = this;
        }

        if (IsSupported)
        {
            TryInvokeNative(RegisterRecordsCallback, "set records callback");
        }
    }

    /// <summary>
    /// Must not sit inside a lambda: taking <c>&amp;HandleNativeRecords</c> is only legal
    /// from a method that can form a function pointer (iOS AOT reverse P/Invoke).
    /// </summary>
    private static unsafe void RegisterRecordsCallback() =>
        NativeSetRecordsCallback(&HandleNativeRecords);

    public event EventHandler<IReadOnlyList<CloudSyncRecordDto>>? RecordsChanged;

    public bool IsSupported => _isSupported ??= QueryIsSupported();

    public Task<CloudSyncAccountStatus> GetAccountStatusAsync(CancellationToken ct = default) =>
        // Works below iOS 17 too — plain CKContainer.accountStatus, no CKSyncEngine.
        Task.Run(
            () =>
            {
                try
                {
                    return CloudKitSyncBridgeCodec.ToAccountStatus(NativeAccountStatus());
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                {
                    CrashLog.Write("CloudKitSyncTransport: native bridge missing", ex);
                    return CloudSyncAccountStatus.CouldNotDetermine;
                }
            },
            ct);

    public Task<bool> IsZoneEmptyAsync(CancellationToken ct = default)
    {
        if (!IsSupported)
        {
            return Task.FromResult(true);
        }

        return Task.Run(
            () =>
            {
                var result = NativeIsZoneEmpty();
                if (result < 0)
                {
                    throw NativeFailure("is_zone_empty", -result);
                }
                return result == 1;
            },
            ct);
    }

    public Task StartAsync(CancellationToken ct = default) =>
        RunNativeAsync(NativeStart, "start", ct);

    public Task StopAsync(CancellationToken ct = default) =>
        RunNativeAsync(NativeStop, "stop", ct);

    public Task EnqueueUpsertAsync(CloudSyncRecordDto record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!IsSupported)
        {
            return Task.CompletedTask;
        }

        var recordType = CloudKitSyncBridgeCodec.ToRecordType(record.EntityType);
        var updatedAt = ToIsoUtc(record.UpdatedAt ?? DateTime.UtcNow);
        var payload = record.PayloadJson ?? "{}";

        return RunNativeAsync(
            () => NativeEnqueueUpsert(recordType, record.Id, updatedAt, payload),
            "enqueue_upsert",
            ct);
    }

    public Task EnqueueDeleteAsync(SyncEntityType type, string id, DateTime deletedAt, CancellationToken ct = default)
    {
        if (!IsSupported)
        {
            return Task.CompletedTask;
        }

        var recordType = CloudKitSyncBridgeCodec.ToRecordType(type);
        var deletedAtIso = ToIsoUtc(deletedAt);

        return RunNativeAsync(
            () => NativeEnqueueDelete(recordType, id, deletedAtIso),
            "enqueue_delete",
            ct);
    }

    public Task FetchChangesAsync(CancellationToken ct = default) =>
        RunNativeAsync(NativeFetchChanges, "fetch_changes", ct);

    public Task SendChangesAsync(CancellationToken ct = default) =>
        RunNativeAsync(NativeSendChanges, "send_changes", ct);

    private bool QueryIsSupported()
    {
        try
        {
            return NativeIsSupported() == 1;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            CrashLog.Write("CloudKitSyncTransport: native bridge missing", ex);
            return false;
        }
    }

    /// <summary>Unsupported OS versions no-op — <c>EnableCloudSyncUseCase</c> already blocks them.</summary>
    private Task RunNativeAsync(Func<int> call, string operation, CancellationToken ct)
    {
        if (!IsSupported)
        {
            return Task.CompletedTask;
        }

        return Task.Run(
            () =>
            {
                var status = call();
                if (status != 0)
                {
                    throw NativeFailure(operation, status);
                }
            },
            ct);
    }

    private static void TryInvokeNative(Action call, string operation)
    {
        try
        {
            call();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            CrashLog.Write($"CloudKitSyncTransport: {operation} failed — native bridge missing", ex);
        }
    }

    private static InvalidOperationException NativeFailure(string operation, int status) =>
        new($"CloudKit bridge '{operation}' failed with native status {status}.");

    private static string ToIsoUtc(DateTime value) =>
        (value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime())
            .ToString(IsoUtcFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Called from a CloudKit background thread. The C string is only valid for the
    /// duration of the call, so copy first and never let an exception escape into Swift.
    /// UnmanagedCallersOnly is required for iOS AOT reverse P/Invoke (TestFlight abort otherwise).
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void HandleNativeRecords(IntPtr recordsJsonUtf8)
    {
        try
        {
            var json = Marshal.PtrToStringUTF8(recordsJsonUtf8);
            var records = CloudKitSyncBridgeCodec.DecodeRecords(json);
            if (records.Count == 0)
            {
                return;
            }

            CloudKitSyncTransport? target;
            lock (InstanceGate)
            {
                target = _active;
            }

            target?.RaiseRecordsChanged(records);
        }
        catch (Exception ex)
        {
            CrashLog.Write("CloudKitSyncTransport: records callback failed", ex);
        }
    }

    private void RaiseRecordsChanged(IReadOnlyList<CloudSyncRecordDto> records)
    {
        var handler = RecordsChanged;
        if (handler is null)
        {
            return;
        }

        // Remote records end up in repositories that feed the UI — same thread as local writes.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                handler(this, records);
            }
            catch (Exception ex)
            {
                CrashLog.Write("CloudKitSyncTransport: RecordsChanged handler failed", ex);
            }
        });
    }

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_is_supported")]
    private static extern int NativeIsSupported();

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_account_status")]
    private static extern int NativeAccountStatus();

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_is_zone_empty")]
    private static extern int NativeIsZoneEmpty();

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_start")]
    private static extern int NativeStart();

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_stop")]
    private static extern int NativeStop();

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_enqueue_upsert")]
    private static extern int NativeEnqueueUpsert(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string type,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string id,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string updatedAtIso,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string payloadJson);

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_enqueue_delete")]
    private static extern int NativeEnqueueDelete(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string type,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string id,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string deletedAtIso);

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_fetch_changes")]
    private static extern int NativeFetchChanges();

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_send_changes")]
    private static extern int NativeSendChanges();

    [DllImport(NativeLibrary, EntryPoint = "finanzuebersicht_ck_set_records_callback")]
    private static extern unsafe void NativeSetRecordsCallback(delegate* unmanaged[Cdecl]<IntPtr, void> callback);
}
#endif
