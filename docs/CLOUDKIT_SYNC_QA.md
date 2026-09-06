# CloudKit Sync — Manual QA Checklist

**Issue:** [#243](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/243) — **passed** (iPhone → iPad → iMac, 2026-09-06). Persistenz prep [#300](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/300) is done.

**Prerequisites**

- **Store** build (not Direct); same Apple ID on the devices.
- Sync entitlement: sandbox Sync IAP or Debug license stub (`CanUseCloudSync`).
- Apple Developer Portal: iCloud container `iCloud.de.thomasmenzl.finanzuebersicht`.
- Native bridge rebuilt if Swift changed: [`Finanzuebersicht/Platforms/iOS/Native/README-CloudKitSync.md`](../Finanzuebersicht/Platforms/iOS/Native/README-CloudKitSync.md).
- Mac Catalyst **Debug** has no iCloud entitlements — use Release/Store Mac build for Mac-side QA.

## Core scenarios

| # | Scenario | Pass |
|---|----------|------|
| 1 | Device A: empty cloud, local data → enable Sync → data appears on B after enable/pull | ✅ iPhone → iPad → iMac |
| 2 | Create / update / delete transaction on A → change appears on B | ☐ |
| 3 | Offline edit on A, online on B, then A online → LWW by `UpdatedAt` | ☐ |
| 4 | Both devices have local data → enable blocked; optional „iCloud übernehmen“ empties this device (backup first) and pulls | ☐ |
| 5 | Direct build → no Sync enable UI / no Cloud Sync | ☐ |
| 6 | OS below iOS 17 / Mac Catalyst 17 → unsupported message (if app still runs without Sync) | ☐ |

## Notes for testers

- Sync is **opt-in** (Settings → iCloud-Sync switch when Store + Sync IAP + supported OS).
- Data lives in the user’s **private iCloud**; no first-party sync server.
- Zone: `finanzuebersicht-sync` (private CloudKit DB); LWW + tombstones.

## Known gaps (not blockers for the MVP path)

- `RecurringGenerationService` writes bypass orchestrator notify. Do **not** notify auto-generated transactions until instance ids are stable across devices (`DauerauftragId` + date); notifying two Guid copies would double-book.
- Mac Catalyst **Debug** has no iCloud entitlements — use Release/Store Mac build for Mac-side QA.
- Schema pause is implemented: a cloud `SyncMeta.schemaVersion` newer than the app persists `LastError` and skips apply/upload (`Sync_Error` in Settings).

## Sign-off

| Device A | Device B | Device C | Tester | Date | Result |
|----------|----------|----------|--------|------|--------|
| iPhone | iPad | iMac | Thomas | 2026-09-06 | Pass (Store path, same Apple ID) |
