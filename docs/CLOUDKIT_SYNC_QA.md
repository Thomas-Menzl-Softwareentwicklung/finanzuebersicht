# CloudKit Sync — Manual QA Checklist

**Issue:** [#243](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/243) stays **open** until this checklist passes on two physical devices. Persistenz prep [#300](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/300) is done; do not close via API.

**Prerequisites**

- **Store** build (not Direct); same Apple ID on both devices.
- Sync entitlement: sandbox Sync IAP or Debug license stub (`CanUseCloudSync`).
- Apple Developer Portal: iCloud container `iCloud.de.thomasmenzl.finanzuebersicht` + Push on main app ID.
- Native bridge rebuilt if Swift changed: [`Finanzuebersicht/Platforms/iOS/Native/README-CloudKitSync.md`](../Finanzuebersicht/Platforms/iOS/Native/README-CloudKitSync.md).
- Mac Catalyst **Debug** has no iCloud entitlements — use Release/Store Mac build for Mac-side QA.

## Core scenarios (two devices)

| # | Scenario | Pass |
|---|----------|------|
| 1 | Device A: empty cloud, local data → enable Sync → data appears on B after enable/pull | ☐ |
| 2 | Create / update / delete transaction on A → change appears on B | ☐ |
| 3 | Offline edit on A, online on B, then A online → LWW by `UpdatedAt` | ☐ |
| 4 | Both devices have local data → enable blocked; optional „iCloud übernehmen“ empties this device (backup first) and pulls | ☐ |
| 5 | Direct build → no Sync enable UI / no Cloud Sync | ☐ |
| 6 | OS below iOS 17 / Mac Catalyst 17 → unsupported message (if app still runs without Sync) | ☐ |

## Notes for testers

- Sync is **opt-in** (Settings → iCloud-Sync switch when Store + Sync IAP + supported OS).
- Data lives in the user’s **private iCloud**; no first-party sync server.
- Zone: `finanzuebersicht-sync` (private CloudKit DB); LWW + tombstones.

## Known gaps (not blockers for checklist design)

- `RecurringGenerationService` writes bypass orchestrator notify. Do **not** notify auto-generated transactions until instance ids are stable across devices (`DauerauftragId` + date); notifying two Guid copies would double-book.
- Live CloudKit end-to-end has **not** been smoke-tested on this branch before device QA.
- Mac Catalyst **Debug** has no iCloud entitlements — use Release/Store Mac build for Mac-side QA.
- Schema pause is implemented: a cloud `SyncMeta.schemaVersion` newer than the app persists `LastError` and skips apply/upload (`Sync_Error` in Settings).

#243 stays open.

## Sign-off

| Device A | Device B | Tester | Date | Result |
|----------|----------|--------|------|--------|
| | | | | |

When all rows pass, update [#243](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/243) in GitHub (manual close only after review).
