# CloudKit Sync MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship optional private-CloudKit sync (iPhone/iPad/Mac, same Apple ID) for accounts, categories, transactions, recurring (+ embedded exceptions), and SparZiele — LWW + tombstones, Sync-Abo gated, OS ≥ iOS 17 / Mac Catalyst 14.

**Architecture:** C# owns LWW, tombstones, enable-guard, and orchestrator; Swift `CKSyncEngine` bridge owns transport only; local JSON remains source of truth. Windows/Direct stay stubbed.

**Tech Stack:** .NET 10 MAUI, xUnit + NSubstitute, CloudKit `CKSyncEngine` (Swift `@_cdecl` static lib, same packaging as WidgetKit bridge), existing `LicenseService` Sync IAP.

**Spec:** `docs/superpowers/specs/2026-08-07-cloudkit-sync-mvp-design.md`  
**Issues:** #243 (feature), #300 (persistenz prep)  
**Note:** Implementation is deferred until you choose to execute; this plan is ready to run later on a feature branch off `develop`.

## Global Constraints

- Sync feature OS floor: iOS 17.0 / Mac Catalyst corresponding to macOS 14+; app-wide `SupportedOSPlatformVersion` may stay 15.0.
- Distribution: Store channel only; Direct builds never sync (`CanUseCloudSync` / stub transport).
- Conflict: last-write-wins per entity via UTC `UpdatedAt`.
- Deletes: tombstones (`entityType`, `id`, `deletedAt`); retain ~90 days then GC.
- First enable: allow only if Cloud zone empty **XOR** local synced data empty.
- `RecurringException` syncs embedded inside `RecurringTransaction` (matches `recurring.json`); no separate CK record type.
- No Core Data / `NSPersistentCloudKitContainer`; no Windows sync; no budgets/settings/widget presets in MVP.
- Schema: CloudKit zone `finanzuebersicht-sync`; record types `Account`, `Category`, `Transaction`, `RecurringTransaction`, `SparZiel`, `Tombstone`, plus `SyncMeta.schemaVersion`.
- Tests: xUnit `[Fact]`, NSubstitute for fakes; run via `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter …`.
- Every task ends with a commit on the implementation branch (not this docs-only branch unless continuing docs work).

## File map (create / modify)

| Path | Role |
|------|------|
| `Finanzuebersicht.Core/Models/{Account,Category,Transaction,RecurringTransaction,SparZiel}.cs` | Add `UpdatedAt`, `ExternalId`, `Source` |
| `Finanzuebersicht.Core/Sync/SyncEntitySource.cs` | Enum / string constants (`Local`, `CloudKit`, …) |
| `Finanzuebersicht.Core/Sync/SyncEntityType.cs` | Enum of syncable types |
| `Finanzuebersicht.Core/Sync/SyncTombstone.cs` | Tombstone model |
| `Finanzuebersicht.Core/Sync/LastWriteWins.cs` | Pure LWW helper |
| `Finanzuebersicht.Core/Sync/ICloudSyncTransport.cs` | Platform transport port |
| `Finanzuebersicht.Core/Sync/ISyncMetadataStore.cs` | Enable flag, last sync, pending |
| `Finanzuebersicht.Core/Sync/ISyncTombstoneStore.cs` | Tombstone persistence port |
| `Finanzuebersicht.Core/Sync/CloudSyncRecordDto.cs` | Transport DTO (type, id, updatedAt, payloadJson, isTombstone) |
| `Finanzuebersicht.Core/Constants/DataFileNames.cs` | Add `SyncTombstones`, `SyncMetadata` |
| `Finanzuebersicht.Application/UseCases/Sync/EnableCloudSyncUseCase.cs` | First-enable guard + seed/pull |
| `Finanzuebersicht.Application/UseCases/Sync/CloudSyncOrchestrator.cs` | Apply remote, enqueue local, debounce |
| `Finanzuebersicht.Infrastructure/Services/SyncTombstoneStore.cs` | JSON store |
| `Finanzuebersicht.Infrastructure/Services/SyncMetadataStore.cs` | JSON store |
| `Finanzuebersicht.Infrastructure/Sync/NullCloudSyncTransport.cs` | Default / Windows / Direct |
| `Finanzuebersicht/Platforms/iOS/Native/CloudKitSyncBridge.swift` | CKSyncEngine |
| `Finanzuebersicht/Platforms/iOS/Native/build-cloudkit-sync-bridge.sh` | Build `.a` |
| `Finanzuebersicht/Platforms/iOS/CloudKitSyncTransport.cs` | `DllImport` adapter |
| `Finanzuebersicht/Platforms/{iOS,MacCatalyst}/Entitlements*.plist` | CloudKit + push |
| `Finanzuebersicht.Infrastructure/Licensing/LicenseService.cs` | `IsCloudSyncImplemented` when platform ready |
| `Finanzuebersicht.Presentation/ViewModels/Settings/LicenseViewModel.cs` (+ Settings XAML if needed) | Enable Sync UI / status |
| Tests under `Finanzuebersicht.Tests/…/Sync/` | LWW, enable guard, orchestrator, stores |

---

### Task 1: Sync persistence fields on models (#300 + UpdatedAt)

**Files:**
- Modify: `Finanzuebersicht.Core/Models/Account.cs`
- Modify: `Finanzuebersicht.Core/Models/Category.cs`
- Modify: `Finanzuebersicht.Core/Models/Transaction.cs`
- Modify: `Finanzuebersicht.Core/Models/RecurringTransaction.cs`
- Modify: `Finanzuebersicht.Core/Models/SparZiel.cs`
- Create: `Finanzuebersicht.Core/Sync/SyncEntitySource.cs`
- Test: `Finanzuebersicht.Tests/Services/AccountStoreTests.cs` (extend) or `Finanzuebersicht.Tests/Sync/SyncFieldRoundtripTests.cs`

**Interfaces:**
- Consumes: existing JSON stores (camelCase System.Text.Json)
- Produces: optional properties on entities — missing in old files ⇒ defaults

- [ ] **Step 1: Write the failing roundtrip test**

```csharp
// Finanzuebersicht.Tests/Sync/SyncFieldRoundtripTests.cs
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Sync;

public class SyncFieldRoundtripTests : IDisposable
{
    private readonly string _dir;
    private readonly AccountStore _store;

    public SyncFieldRoundtripTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"sync_fields_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        _store = new AccountStore(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public async Task Save_PersistsUpdatedAtExternalIdAndSource()
    {
        var when = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);
        var account = new Account
        {
            Name = "Giro",
            UpdatedAt = when,
            ExternalId = "ck-1",
            Source = SyncEntitySource.CloudKit
        };

        await _store.SaveAccountAsync(account);
        var loaded = (await _store.GetAccountsAsync()).Single(a => a.Id == account.Id);

        Assert.Equal(when, loaded.UpdatedAt);
        Assert.Equal("ck-1", loaded.ExternalId);
        Assert.Equal(SyncEntitySource.CloudKit, loaded.Source);
    }

    [Fact]
    public async Task Load_LegacyJsonWithoutSyncFields_SucceedsWithDefaults()
    {
        var path = Path.Combine(_dir, DataFileNames.Accounts);
        await File.WriteAllTextAsync(path, """[{"id":"legacy-1","name":"Alt","type":0}]""");

        var loaded = await _store.GetAccountsAsync();
        var a = Assert.Single(loaded);
        Assert.Equal("legacy-1", a.Id);
        Assert.Null(a.ExternalId);
        Assert.Null(a.Source);
        Assert.Null(a.UpdatedAt);
    }
}
```

- [ ] **Step 2: Run test — expect FAIL (properties missing)**

```bash
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~SyncFieldRoundtripTests" -v n
```

Expected: compile error or FAIL on missing members.

- [ ] **Step 3: Add source constants + model fields**

```csharp
// Finanzuebersicht.Core/Sync/SyncEntitySource.cs
namespace Finanzuebersicht.Core.Sync;

public static class SyncEntitySource
{
    public const string Local = "Local";
    public const string CloudKit = "CloudKit";
    public const string OpenBanking = "OpenBanking"; // reserved for #245; unused in MVP
}
```

On each of Account, Category, Transaction, RecurringTransaction, SparZiel add:

```csharp
public DateTime? UpdatedAt { get; set; }
public string? ExternalId { get; set; }
public string? Source { get; set; }
```

(`using Finanzuebersicht.Core.Sync;` not required if Source stays `string?`.)

- [ ] **Step 4: Re-run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Core/Models Finanzuebersicht.Core/Sync/SyncEntitySource.cs Finanzuebersicht.Tests/Sync/SyncFieldRoundtripTests.cs
git commit -m "feat(sync): add UpdatedAt/ExternalId/Source on syncable models (#300)"
```

---

### Task 2: Tombstone + sync metadata stores

**Files:**
- Modify: `Finanzuebersicht.Core/Constants/DataFileNames.cs`
- Create: `Finanzuebersicht.Core/Sync/SyncEntityType.cs`
- Create: `Finanzuebersicht.Core/Sync/SyncTombstone.cs`
- Create: `Finanzuebersicht.Core/Sync/ISyncTombstoneStore.cs`
- Create: `Finanzuebersicht.Core/Sync/ISyncMetadataStore.cs`
- Create: `Finanzuebersicht.Core/Sync/SyncMetadata.cs`
- Create: `Finanzuebersicht.Infrastructure/Services/SyncTombstoneStore.cs`
- Create: `Finanzuebersicht.Infrastructure/Services/SyncMetadataStore.cs`
- Modify: `Finanzuebersicht.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`
- Test: `Finanzuebersicht.Tests/Sync/SyncTombstoneStoreTests.cs`

**Interfaces:**
- Produces:
  - `ISyncTombstoneStore`: `Task<IReadOnlyList<SyncTombstone>> GetAllAsync(); Task UpsertAsync(SyncTombstone t); Task RemoveAsync(SyncEntityType type, string id);`
  - `ISyncMetadataStore`: `Task<SyncMetadata> GetAsync(); Task SaveAsync(SyncMetadata meta);`
  - `SyncMetadata`: `bool SyncEnabled`, `DateTime? LastSyncUtc`, `int SchemaVersionSeen`, `string? LastError`

- [ ] **Step 1: Failing store test**

```csharp
[Fact]
public async Task Upsert_ThenGetAll_ReturnsTombstone()
{
    var store = new SyncTombstoneStore(_dir);
    var t = new SyncTombstone
    {
        EntityType = SyncEntityType.Transaction,
        Id = "tx-1",
        DeletedAt = DateTime.UtcNow
    };
    await store.UpsertAsync(t);
    var all = await store.GetAllAsync();
    Assert.Contains(all, x => x.Id == "tx-1" && x.EntityType == SyncEntityType.Transaction);
}
```

- [ ] **Step 2: Run — expect FAIL**

```bash
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~SyncTombstoneStoreTests" -v n
```

- [ ] **Step 3: Implement models + JSON stores**

Add to `DataFileNames.cs`:

```csharp
public const string SyncTombstones = "sync-tombstones.json";
public const string SyncMetadata = "sync-metadata.json";
```

```csharp
namespace Finanzuebersicht.Core.Sync;

public enum SyncEntityType
{
    Account = 0,
    Category = 1,
    Transaction = 2,
    RecurringTransaction = 3,
    SparZiel = 4
}

public sealed class SyncTombstone
{
    public SyncEntityType EntityType { get; set; }
    public string Id { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}

public sealed class SyncMetadata
{
    public bool SyncEnabled { get; set; }
    public DateTime? LastSyncUtc { get; set; }
    public int SchemaVersionSeen { get; set; }
    public string? LastError { get; set; }
}
```

Implement `SyncTombstoneStore` / `SyncMetadataStore` mirroring `AccountStore` (inherit `JsonDataStoreBase`, semaphore, camelCase). Register both as singletons in `InfrastructureServiceCollectionExtensions`.

- [ ] **Step 4: Tests PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(sync): add tombstone and sync-metadata JSON stores"
```

---

### Task 3: Last-write-wins helper

**Files:**
- Create: `Finanzuebersicht.Core/Sync/LastWriteWins.cs`
- Test: `Finanzuebersicht.Tests/Sync/LastWriteWinsTests.cs`

**Interfaces:**
- Produces: `public static class LastWriteWins { public static bool RemoteWins(DateTime? localUpdatedAt, DateTime? remoteUpdatedAt); }`
  - Rules: remote wins if remote has timestamp and (local null OR remote > local); equal timestamps → remote wins (deterministic); both null → remote wins (first apply).

- [ ] **Step 1: Write matrix tests**

```csharp
public class LastWriteWinsTests
{
    [Theory]
    [InlineData(null, "2026-01-02", true)]
    [InlineData("2026-01-02", null, false)]
    [InlineData("2026-01-01", "2026-01-02", true)]
    [InlineData("2026-01-02", "2026-01-01", false)]
    [InlineData("2026-01-02", "2026-01-02", true)]
    [InlineData(null, null, true)]
    public void RemoteWins_Matrix(string? local, string? remote, bool expectRemote)
    {
        DateTime? L = local is null ? null : DateTime.Parse(local, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        DateTime? R = remote is null ? null : DateTime.Parse(remote, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        Assert.Equal(expectRemote, LastWriteWins.RemoteWins(L, R));
    }
}
```

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Implement**

```csharp
namespace Finanzuebersicht.Core.Sync;

public static class LastWriteWins
{
    public static bool RemoteWins(DateTime? localUpdatedAt, DateTime? remoteUpdatedAt)
    {
        if (remoteUpdatedAt is null)
            return false;
        if (localUpdatedAt is null)
            return true;
        return remoteUpdatedAt.Value >= localUpdatedAt.Value;
    }
}
```

- [ ] **Step 4: PASS + commit**

```bash
git commit -m "feat(sync): add LastWriteWins helper"
```

---

### Task 4: Transport port + Null transport + DTOs

**Files:**
- Create: `Finanzuebersicht.Core/Sync/CloudSyncRecordDto.cs`
- Create: `Finanzuebersicht.Core/Sync/ICloudSyncTransport.cs`
- Create: `Finanzuebersicht.Core/Sync/CloudSyncAccountStatus.cs`
- Create: `Finanzuebersicht.Infrastructure/Sync/NullCloudSyncTransport.cs`
- Modify: DI registration — `ICloudSyncTransport` → `NullCloudSyncTransport` by default
- Test: `Finanzuebersicht.Tests/Sync/NullCloudSyncTransportTests.cs`

**Interfaces:**
- Produces:

```csharp
public enum CloudSyncAccountStatus { Available, NoAccount, Restricted, CouldNotDetermine, TemporarilyUnavailable }

public sealed class CloudSyncRecordDto
{
    public SyncEntityType EntityType { get; init; }
    public string Id { get; init; } = "";
    public DateTime? UpdatedAt { get; init; }
    public string? PayloadJson { get; init; } // null if tombstone
    public bool IsTombstone { get; init; }
    public DateTime? DeletedAt { get; init; }
}

public interface ICloudSyncTransport
{
    bool IsSupported { get; } // false on Null / Windows / OS < 17
    Task<CloudSyncAccountStatus> GetAccountStatusAsync(CancellationToken ct = default);
    Task<bool> IsZoneEmptyAsync(CancellationToken ct = default);
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task EnqueueUpsertAsync(CloudSyncRecordDto record, CancellationToken ct = default);
    Task EnqueueDeleteAsync(SyncEntityType type, string id, DateTime deletedAt, CancellationToken ct = default);
    Task FetchChangesAsync(CancellationToken ct = default);
    Task SendChangesAsync(CancellationToken ct = default);
    event EventHandler<IReadOnlyList<CloudSyncRecordDto>>? RecordsChanged;
}
```

`NullCloudSyncTransport`: `IsSupported => false`; methods no-op / return `NoAccount` or `CouldNotDetermine`; `IsZoneEmptyAsync => true`.

- [ ] **Step 1–4:** Test that Null is not supported and enqueue does not throw; implement; register; commit

```bash
git commit -m "feat(sync): add ICloudSyncTransport port and null implementation"
```

---

### Task 5: EnableCloudSyncUseCase (first-enable guard)

**Files:**
- Create: `Finanzuebersicht.Application/UseCases/Sync/EnableCloudSyncResult.cs`
- Create: `Finanzuebersicht.Application/UseCases/Sync/EnableCloudSyncUseCase.cs`
- Modify: `Finanzuebersicht.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs`
- Test: `Finanzuebersicht.Tests/Application/UseCases/Sync/EnableCloudSyncUseCaseTests.cs`

**Interfaces:**
- Consumes: `ICloudSyncTransport`, `ISyncMetadataStore`, `IAccountRepository`, `ICategoryRepository`, `ITransactionRepository`, `IRecurringTransactionRepository`, `ISparZielRepository`, `ILicenseService`
- Produces: `EnableCloudSyncUseCase.ExecuteAsync()` → `EnableCloudSyncResult` with enum `Enabled | BlockedBothHaveData | BlockedNoEntitlement | BlockedUnsupported | BlockedNoICloud`

```csharp
public sealed class EnableCloudSyncResult
{
    public EnableCloudSyncStatus Status { get; init; }
    public string? MessageKey { get; init; } // resource key for UI
}

public enum EnableCloudSyncStatus
{
    Enabled,
    BlockedBothHaveData,
    BlockedNoEntitlement,
    BlockedUnsupported,
    BlockedNoICloud
}
```

Logic:
1. If `!license.CanUseCloudSync` → `BlockedNoEntitlement`
2. If `!transport.IsSupported` → `BlockedUnsupported`
3. If account status not `Available` → `BlockedNoICloud`
4. `localEmpty` = all five repos empty (no user entities; system categories/accounts: treat “local sync data empty” as **no user-created** data — define helper: zero transactions AND zero non-system accounts AND zero non-system categories AND zero recurring AND zero SparZiele). Be explicit in code comments; mirror backup empty checks if any exist.
5. `cloudEmpty = await transport.IsZoneEmptyAsync()`
6. If `!localEmpty && !cloudEmpty` → `BlockedBothHaveData`
7. Else set metadata `SyncEnabled=true`, `await transport.StartAsync()`, then either enqueue all local records (seed) or `FetchChangesAsync` (pull). Return `Enabled`.

- [ ] **Step 1: Write tests with NSubstitute**

```csharp
[Fact]
public async Task Execute_WhenBothHaveData_ReturnsBlockedBothHaveData()
{
    // transport.IsSupported=true, IsZoneEmptyAsync=false
    // accountRepo returns one non-system account
    // license.CanUseCloudSync=true
    var result = await sut.ExecuteAsync();
    Assert.Equal(EnableCloudSyncStatus.BlockedBothHaveData, result.Status);
}

[Fact]
public async Task Execute_WhenCloudEmptyAndLocalHasData_SeedsAndEnables()
{
    // IsZoneEmptyAsync=true, local has account
    // Expect EnqueueUpsertAsync received, metadata SyncEnabled
}
```

- [ ] **Step 2: FAIL → Step 3 implement → Step 4 PASS → Step 5 commit**

```bash
git commit -m "feat(sync): add EnableCloudSyncUseCase with empty XOR guard"
```

---

### Task 6: CloudSyncOrchestrator (apply remote + enqueue local)

**Files:**
- Create: `Finanzuebersicht.Application/UseCases/Sync/CloudSyncOrchestrator.cs`
- Create: `Finanzuebersicht.Application/UseCases/Sync/ICloudSyncOrchestrator.cs`
- Modify: Application DI
- Test: `Finanzuebersicht.Tests/Application/UseCases/Sync/CloudSyncOrchestratorTests.cs`

**Interfaces:**
- Consumes: transport, all MVP repos, tombstone store, metadata store, JSON serializer options matching stores
- Produces:

```csharp
public interface ICloudSyncOrchestrator
{
    Task StartIfEnabledAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task NotifyLocalUpsertAsync(SyncEntityType type, string id, CancellationToken ct = default);
    Task NotifyLocalDeleteAsync(SyncEntityType type, string id, CancellationToken ct = default);
    Task SyncNowAsync(CancellationToken ct = default); // fetch + send
}
```

Behavior:
- Subscribe to `transport.RecordsChanged`.
- On remote upsert: load local by id; if missing save; if present and `LastWriteWins.RemoteWins` then replace (set `Source=CloudKit`, keep remote `UpdatedAt`).
- On remote tombstone: delete local if present; upsert local tombstone; if local `UpdatedAt` > tombstone `DeletedAt`, skip delete (LWW favors newer local edit — optional MVP simplification: always apply remote tombstone if `DeletedAt >= local.UpdatedAt`).
- `NotifyLocalUpsertAsync`: load entity, set `UpdatedAt=UtcNow` if null/stale caller didn’t, serialize payload, debounce 1500ms per key `(type,id)`, then `EnqueueUpsertAsync`.
- `NotifyLocalDeleteAsync`: write tombstone, `EnqueueDeleteAsync`.
- `SyncNowAsync`: `FetchChangesAsync` + `SendChangesAsync`; update `LastSyncUtc`.

- [ ] **Step 1: Tests** — remote newer overwrites; local newer keeps; tombstone removes; debounce coalesces two rapid upserts into one enqueue (use fake transport capturing calls + `Task.Delay` or injectable `TimeProvider` if available; otherwise test coalescing map without real delay by exposing internal flush for tests).

Prefer injectable `Func<Task> delay` or flush method `internal Task FlushPendingForTestsAsync()` to avoid flaky timing.

- [ ] **Step 2–5:** implement, PASS, commit

```bash
git commit -m "feat(sync): add CloudSyncOrchestrator with LWW apply and debounce"
```

---

### Task 7: Wire write paths to stamp UpdatedAt + notify orchestrator

**Files:** (hook at use-case or store layer — prefer **use cases** that already mutate, to avoid double-notify)

Identify save/delete use cases for Account, Category, Transaction, Recurring, SparZiel (search `Save*UseCase` / `Delete*UseCase` under `Finanzuebersicht.Application/UseCases/`). For each successful save/delete:

1. Ensure `entity.UpdatedAt = DateTime.UtcNow` before persist (saves).
2. After persist: `_orchestrator.NotifyLocalUpsertAsync(...)` or `NotifyLocalDeleteAsync(...)`.
3. Only notify when `ISyncMetadataStore` has `SyncEnabled` (orchestrator can no-op if disabled — still OK to call).

Also: app resume / `MauiProgram` or existing lifecycle — call `orchestrator.StartIfEnabledAsync` and `SyncNowAsync` on foreground. Find existing lifecycle hooks (settings / App.xaml.cs / Mac Catalyst resume) and attach there.

**Tests:** extend one use-case test (e.g. account save) to verify `NotifyLocalUpsertAsync` received when sync enabled (substitute orchestrator).

- [ ] **Step 1–5:** TDD one use case fully; then apply same pattern to remaining entity use cases in the same commit if small, or split commits per entity family.

```bash
git commit -m "feat(sync): notify orchestrator from entity write use cases"
```

---

### Task 8: Swift CKSyncEngine bridge (scaffold + C API)

**Files:**
- Create: `Finanzuebersicht/Platforms/iOS/Native/CloudKitSyncBridge.swift`
- Create: `Finanzuebersicht/Platforms/iOS/Native/build-cloudkit-sync-bridge.sh` (clone patterns from `build-widgetkit-bridge.sh`)
- Modify: `Finanzuebersicht/Finanzuebersicht.csproj` — `NativeReference` + build target for iOS **and** Mac Catalyst (shared Swift if possible)
- Create: `Finanzuebersicht/Platforms/iOS/CloudKitSyncTransport.cs` (`#if IOS || MACCATALYST`)

**C API (stable names):**

```c
// return 0 = ok
int finanzuebersicht_ck_is_supported(void); // 1 if @available iOS 17 / macOS 14
int finanzuebersicht_ck_account_status(void); // map to enum ints
int finanzuebersicht_ck_is_zone_empty(void);
int finanzuebersicht_ck_start(void);
int finanzuebersicht_ck_stop(void);
int finanzuebersicht_ck_enqueue_upsert(const char* type, const char* id, const char* updatedAtIso, const char* payloadJson);
int finanzuebersicht_ck_enqueue_delete(const char* type, const char* id, const char* deletedAtIso);
int finanzuebersicht_ck_fetch_changes(void);
int finanzuebersicht_ck_send_changes(void);
// Callback into managed: register function pointer for batch of JSON records
typedef void (*finanzuebersicht_ck_records_cb)(const char* recordsJson);
void finanzuebersicht_ck_set_records_callback(finanzuebersicht_ck_records_cb cb);
```

Swift: private CloudKit container (use App’s iCloud container id — create in developer portal; document id in README, e.g. `iCloud.de.thomasmenzl.finanzuebersicht`). Zone `finanzuebersicht-sync`. Map record type string ↔ `SyncEntityType`. Store payload in `payload` String field + `updatedAt` Date.

`CloudKitSyncTransport` implements `ICloudSyncTransport` via `DllImport("__Internal")`. On non-Apple TFMs, do not compile this type.

DI: in `MauiProgram.cs` (or platform extension), replace `NullCloudSyncTransport` with `CloudKitSyncTransport` when `License` Store + OS supported.

- [ ] **Step 1:** Add Swift file that compiles and exports `finanzuebersicht_ck_is_supported` returning 0/1.
- [ ] **Step 2:** Wire csproj build like WidgetKit bridge; verify iOS build links.
- [ ] **Step 3:** Implement enqueue/fetch/send minimally with CKSyncEngine (follow Apple sample structure).
- [ ] **Step 4:** Manual smoke on device: enable container, see zone create.
- [ ] **Step 5: Commit**

```bash
git commit -m "feat(ios): add CKSyncEngine CloudKit sync bridge scaffold"
```

---

### Task 9: Entitlements, push, privacy copy

**Files:**
- Modify: `Finanzuebersicht/Platforms/iOS/Entitlements.plist` (+ Debug)
- Modify: `Finanzuebersicht/Platforms/MacCatalyst/Entitlements.plist` (+ Debug)
- Modify: `Finanzuebersicht/Platforms/iOS/Info.plist` — remote notification background mode if required
- Modify: `docs/APP_STORE.md` / privacy strings as needed for Sync
- Create: short `Finanzuebersicht/Platforms/iOS/Native/README-CloudKitSync.md` — container id, how to rebuild `.a`

Entitlements to add (Store Release):
- `com.apple.developer.icloud-container-identifiers`
- `com.apple.developer.icloud-services` = `CloudKit`
- `aps-environment` = `production` / `development` for Debug

- [ ] Implement + commit

```bash
git commit -m "chore(ios): add CloudKit and push entitlements for sync"
```

---

### Task 10: Settings UI — enable Sync + status

**Files:**
- Modify: `Finanzuebersicht.Presentation/ViewModels/Settings/LicenseViewModel.cs`
- Modify: Settings page XAML under `Finanzuebersicht/Views/` (find Sync section via `SyncLabel` binding)
- Modify: `ResourceKeys.cs` + `AppResources.resx` / `.de.resx` — blocker strings, status, enable button
- Test: ViewModel tests if pattern exists for LicenseViewModel

Behavior:
- If `CanUseCloudSync && IsCloudSyncImplemented`: show Switch/Button “iCloud-Sync”.
- On enable → `EnableCloudSyncUseCase`; map `BlockedBothHaveData` to alert with backup guidance.
- Show `LastSyncUtc` / error from metadata.
- Do not block transaction UI.

- [ ] TDD ViewModel command where feasible; commit

```bash
git commit -m "feat(ui): add CloudKit sync enable and status in settings"
```

---

### Task 11: Flip IsCloudSyncImplemented + license tests

**Files:**
- Modify: `Finanzuebersicht.Infrastructure/Licensing/LicenseService.cs`
- Modify: `Finanzuebersicht.Tests/Core/Licensing/LicenseServiceTests.cs`

```csharp
public bool IsCloudSyncImplemented =>
    Channel == DistributionChannel.Store
    && CloudSyncPlatform.IsFeatureAvailable; // static helper: IOS/MACCATALYST && OS version
```

Keep Direct ⇒ false. Update tests that assert `IsCloudSyncImplemented` false for Direct; add Store+stub path.

Only flip after Tasks 8–10 are functionally usable; until then keep `false` and ship behind compile flag if needed:

```csharp
#if CLOUDKIT_SYNC_MVP
    public bool IsCloudSyncImplemented => Channel == DistributionChannel.Store && …;
#else
    public bool IsCloudSyncImplemented => false;
#endif
```

Prefer explicit platform helper over forever-false once QA passes.

- [ ] Commit

```bash
git commit -m "feat(licensing): mark Cloud Sync implemented on supported Store Apple builds"
```

---

### Task 12: Docs, manual QA checklist, issue links

**Files:**
- Modify: `docs/GUIDE.md` (CloudKit line), `docs/MONETIZATION.md` phase note if Sync ships
- Modify: `.github/copilot-instructions.md` — sync no longer “idea only” when merged
- Optional: close/update #300 when Task 1–2 land; leave #243 open until device QA done

**Manual QA (two physical devices, same Apple ID, Sync sandbox/IAP stub):**
1. Device A empty cloud, local data → enable → data appears on B after enable/pull.
2. Create/update/delete transaction on A → appears on B.
3. Offline edit on A, online on B, then A online → LWW by `UpdatedAt`.
4. Both have data → enable blocked.
5. Direct build → no enable.
6. OS 15 device → unsupported message (if you can still run app without Sync).

- [ ] Commit docs

```bash
git commit -m "docs: document CloudKit sync MVP behavior and QA checklist"
```

---

## Spec coverage (self-review)

| Spec requirement | Task(s) |
|------------------|---------|
| #300 ExternalId/Source + UpdatedAt | 1 |
| Tombstones store | 2 |
| LWW per entity | 3, 6 |
| Auto sync start/foreground/debounce/push | 6, 7, 8 |
| First enable XOR empty | 5, 10 |
| CKSyncEngine Swift bridge | 8 |
| Private DB zone + record types | 8 |
| Schema version pause | 8 (SyncMeta) + 6 apply path |
| License / OS / Direct gates | 5, 11 |
| Settings status UI | 10 |
| Unit tests LWW/guard/orchestrator | 3, 5, 6 |
| Entitlements + privacy | 9, 12 |
| Embedded RecurringException | 6/8 payload = full RecurringTransaction JSON |
| Out of scope (budgets, Windows, Core Data) | respected — no tasks |

**Refinement vs earlier draft spec:** `RecurringException` is not a separate CloudKit record type; synced inside parent (spec file updated accordingly).

## Suggested merge strategy (when implementing)

1. Land Tasks 1–2 on `develop` as #300 (safe, no CloudKit).
2. Continue Tasks 3–7 with `NullCloudSyncTransport` (testable without devices).
3. Tasks 8–12 on Store Apple builds; feature-flag until device QA green.
