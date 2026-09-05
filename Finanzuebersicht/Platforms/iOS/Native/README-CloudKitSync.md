# CloudKit Sync bridge (MVP #243)

Native Swift bridge (`CloudKitSyncBridge.swift`) around **CKSyncEngine** for the MAUI host (`CloudKitSyncTransport.cs`). On **Store** Apple builds with iOS 17 / Mac Catalyst 17+, sync is user-facing when the Sync IAP (or Debug stub) is active — Settings → iCloud-Sync. **#243 stays open** until two-device QA passes: [`docs/CLOUDKIT_SYNC_QA.md`](../../../../docs/CLOUDKIT_SYNC_QA.md).

## Container and zone

| Setting | Value |
|---------|-------|
| iCloud container | `iCloud.de.thomasmenzl.finanzuebersicht` |
| Database | private (current user) |
| Custom zone | `finanzuebersicht-sync` |

Entity record types: `Account`, `Category`, `Transaction`, `RecurringTransaction`, `SparZiel`. Schema fence: `SyncMeta` (`schemaVersion`, record name `sync-meta`) — not part of the 0–4 entity ordinals. Tombstones use record name `tombstone-<entityId>`.

## Rebuild `libCloudKitSyncBridge.a`

From the repo root (or this directory):

```bash
bash Finanzuebersicht/Platforms/iOS/Native/build-cloudkit-sync-bridge.sh
```

Output (gitignored):

- `lib/Release-iphoneos/libCloudKitSyncBridge.a`
- `lib/Release-iphonesimulator/libCloudKitSyncBridge.a`
- `lib/Release-maccatalyst/libCloudKitSyncBridge.a`

MAUI runs this script automatically on iOS / Mac Catalyst builds. Skip when the `.a` slices are already present:

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios \
  -p:SkipCloudKitSyncBridgeBuild=true
```

## Xcode / `DEVELOPER_DIR`

**Full Xcode is required** — Command Line Tools alone are not enough (CKSyncEngine needs the iOS and macOS SDKs). The build script discovers Xcode the same way as `build-widgetkit-bridge.sh` and `Widgets/build-release.sh`:

1. Honour `DEVELOPER_DIR` if it points at `…/Xcode*.app/Contents/Developer`.
2. Otherwise try `/Applications/Xcode.app`, `/Applications/Xcode-beta.app`, and common `~/Downloads` paths.

If discovery fails:

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
# or
export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer
```

`dotnet build` / `dotnet publish` for Apple targets also needs a valid `DEVELOPER_DIR` on machines where `xcode-select` still points at CLT.

## Apple Developer Portal

On App ID **`de.thomasmenzl.finanzuebersicht`** (main app only — **not** the Quick Expense Widget extension):

1. Enable **iCloud** → **CloudKit**.
2. Create / assign container **`iCloud.de.thomasmenzl.finanzuebersicht`**.
3. Enable **Push Notifications** only when CKSyncEngine push is implemented (deferred — do not regenerate profiles for unused APS).
4. Regenerate Development and App Store provisioning profiles after **iCloud / CloudKit** capability changes.

Entitlements live in:

- iOS Release: `Platforms/iOS/Entitlements.plist`
- iOS Debug: `Platforms/iOS/Entitlements.Debug.plist` (includes `get-task-allow`)
- Mac Catalyst Release: `Platforms/MacCatalyst/Entitlements.plist`
- Mac Catalyst Debug: **no** iCloud / APS (sandbox off so local Debug starts without Team ID — see `Finanzuebersicht.csproj`). To exercise CloudKit on Mac locally, use a **Release** or **Store** Mac Catalyst build with team signing, or a team-signed Debug profile with sandbox + iCloud entitlements.

The Quick Expense Widget `.appex` must **not** receive CloudKit entitlements.

## Background modes / push

CKSyncEngine remote-notification push is **not** wired (`automaticallySync` is false; the app does not call `RegisterForRemoteNotifications`). `UIBackgroundModes` → `remote-notification` and `aps-environment` were removed so the Store build does not declare an unused push capability. Push remains a follow-up once send/fetch is proven on device.

## Privacy manifest

`Platforms/iOS/Resources/PrivacyInfo.xcprivacy` declares `NSPrivacyAccessedAPICategoryUserDefaults` reason **CA92.1** — the bridge persists CKSyncEngine state and staged records in `UserDefaults`.

## Device QA

Run the manual checklist before closing [#243](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/243): [`docs/CLOUDKIT_SYNC_QA.md`](../../../../docs/CLOUDKIT_SYNC_QA.md). Live CloudKit end-to-end has not been smoke-tested on this branch yet.

Known gaps: `RecurringGenerationService` writes bypass orchestrator notify (do not sync generated instances until ids are stable across devices); Mac Catalyst Debug without iCloud entitlements; live CloudKit end-to-end has not been smoke-tested on this branch yet. Schema pause: a `SyncMeta` record with `schemaVersion` newer than the app stops apply/upload and surfaces `Sync_Error`.
