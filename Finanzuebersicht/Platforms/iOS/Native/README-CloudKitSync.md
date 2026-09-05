# CloudKit Sync bridge (MVP #243)

Native Swift bridge (`CloudKitSyncBridge.swift`) around **CKSyncEngine** for the MAUI host (`CloudKitSyncTransport.cs`). Sync is **not** user-facing yet (`IsCloudSyncImplemented` remains `false`); this folder holds the static library and build tooling.

## Container and zone

| Setting | Value |
|---------|-------|
| iCloud container | `iCloud.de.thomasmenzl.finanzuebersicht` |
| Database | private (current user) |
| Custom zone | `finanzuebersicht-sync` |

Entity record types: `Account`, `Category`, `Transaction`, `RecurringTransaction`, `SparZiel`. Tombstones use record name `tombstone-<entityId>`.

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
3. Enable **Push Notifications** (APS; entitlements use `development` in Debug, `production` in Release / Store).
4. Regenerate Development and App Store provisioning profiles after capability changes.

Entitlements live in:

- iOS Release: `Platforms/iOS/Entitlements.plist`
- iOS Debug: `Platforms/iOS/Entitlements.Debug.plist` (includes `get-task-allow`)
- Mac Catalyst Release: `Platforms/MacCatalyst/Entitlements.plist`
- Mac Catalyst Debug: **no** iCloud / APS (sandbox off so local Debug starts without Team ID — see `Finanzuebersicht.csproj`). To exercise CloudKit on Mac locally, use a **Release** or **Store** Mac Catalyst build with team signing, or a team-signed Debug profile with sandbox + iCloud entitlements.

The Quick Expense Widget `.appex` must **not** receive CloudKit entitlements.

## Background modes

`UIBackgroundModes` → `remote-notification` is set in the iOS and Mac Catalyst `Info.plist` files for CKSyncEngine push handling.

## Privacy manifest

`Platforms/iOS/Resources/PrivacyInfo.xcprivacy` declares `NSPrivacyAccessedAPICategoryUserDefaults` reason **CA92.1** — the bridge persists CKSyncEngine state and staged records in `UserDefaults`.

## Device smoke test

End-to-end sync on a signed device with a provisioned iCloud account is **deferred** (orchestrator + Settings UI land in later tasks). After Portal setup, verify zone creation and fetch/send on hardware before enabling sync for users.
