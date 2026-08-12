# Finanzübersicht — XCUITest / fastlane snapshot

UI tests and `SnapshotHelper` for App Store screenshot automation. The **host app is built by .NET MAUI**, not by this Xcode project.

## Prerequisites

- Mac with **Xcode 16+** (iOS 17+ SDK)
- `brew install xcodegen`
- .NET 10 MAUI workload
- Apple Developer team `XY663DU933`

If `xcode-select` points to Command Line Tools only, set Xcode explicitly (same as widget builds):

```bash
export DEVELOPER_DIR="/Users/thomas/Downloads/Xcode-beta 2.app/Contents/Developer"
```

## Regenerate the Xcode project

```bash
cd Finanzuebersicht/Platforms/iOS/UITests
xcodegen generate
open FinanzuebersichtUITests.xcodeproj
```

Scheme: **FinanzuebersichtUITests**.

`ScreenshotHost` is a throwaway iOS app so Xcode can produce a UI test bundle. It is **not** shipped and is **not** the app under test.

## Build the MAUI app (simulator)

```bash
export DEVELOPER_DIR="/Users/thomas/Downloads/Xcode-beta 2.app/Contents/Developer"

dotnet build Finanzuebersicht/Finanzuebersicht.csproj \
  -f net10.0-ios \
  -c Debug \
  -p:RuntimeIdentifier=iossimulator-arm64
```

Typical `.app` path (arm64 simulator; name matches `ApplicationTitle` in csproj):

```text
Finanzuebersicht/bin/Debug/net10.0-ios/iossimulator-arm64/Finanzübersicht.app
```

Bundle id (unchanged): `de.thomasmenzl.finanzuebersicht`.

## Install MAUI app on a booted simulator

```bash
APP="Finanzuebersicht/bin/Debug/net10.0-ios/iossimulator-arm64/Finanzübersicht.app"
xcrun simctl boot "iPhone 17" 2>/dev/null || true
xcrun simctl install booted "$APP"
```

`bundle exec fastlane screenshots` boots each Snapfile device and installs the MAUI `.app` automatically before capture (see `fastlane/Fastfile`).

Tests use `XCUIApplication(bundleIdentifier: "de.thomasmenzl.finanzuebersicht")` so they launch the **installed MAUI build**, not `ScreenshotHost`.

## Compile UI tests only

```bash
export DEVELOPER_DIR="/Users/thomas/Downloads/Xcode-beta 2.app/Contents/Developer"
cd Finanzuebersicht/Platforms/iOS/UITests

xcodebuild build-for-testing \
  -project FinanzuebersichtUITests.xcodeproj \
  -scheme FinanzuebersichtUITests \
  -destination 'platform=iOS Simulator,name=iPhone 17' \
  -derivedDataPath DerivedData
```

## Run screenshot test (local)

1. Build and install the MAUI `.app` (steps above).
2. Generate Xcode project if needed (`xcodegen generate`).
3. Run tests:

```bash
export DEVELOPER_DIR="/Users/thomas/Downloads/Xcode-beta 2.app/Contents/Developer"
cd Finanzuebersicht/Platforms/iOS/UITests

xcodebuild test \
  -project FinanzuebersichtUITests.xcodeproj \
  -scheme FinanzuebersichtUITests \
  -destination 'platform=iOS Simulator,name=iPhone 17' \
  -derivedDataPath DerivedData \
  -only-testing:FinanzuebersichtUITests/FinanzuebersichtUITests/testScreenshots
```

Snapshot PNGs (when `fastlane snapshot` language/locale files are present) land under the simulator cache:

```text
~/Library/Developer/CoreSimulator/.../Library/Caches/tools.fastlane/screenshots/
```

For a manual run without fastlane prep, `SnapshotHelper` still captures; output path depends on `SIMULATOR_HOST_HOME`.

## Launch arguments

The test passes `--screenshot-demo` after `setupSnapshot(app)` so demo mode wins over any fastlane `snapshot-launch_arguments.txt` entries.

`Snapfile` (Task 5) must also include `--screenshot-demo` in `launch_arguments`.

## AutomationId queries

| Target | AutomationId | Query used in test |
|--------|--------------|-------------------|
| Dashboard page | `page.dashboard` | `app.descendants(matching: .any)["page.dashboard"]` |
| Transactions / Recurring / Management / Savings pages | `page.*` | same `descendants` pattern |
| Shell tabs | `tab.*` (XAML) | `app.tabBars.buttons.element(boundBy: N)` — AutomationIds do not reach UITabBar on iOS |
| Schnell FAB / sheet | `fab.schnell` / `sheet.quick-expense` | `descendants(matching: .any)` |
| Settings toolbar / page | `toolbar.settings` / `page.settings` | nav bar button or `descendants` fallback |

`app.otherElements["page.dashboard"]` did not match on iOS Simulator (iOS 27 beta); use `descendants(matching: .any)` instead.

Snapshot flow (`testScreenshots`): `01-dashboard` → `07-settings` — see `FinanzuebersichtUITests.swift`.

## fastlane snapshot

From repo root (`fastlane/Fastfile` installs the MAUI `.app` on each Snapfile simulator before capture):

```bash
export DEVELOPER_DIR="/Users/thomas/Downloads/Xcode-beta 2.app/Contents/Developer"
bundle install
bundle exec fastlane screenshots
```

`Snapfile` pins local simulator names (`iPhone 17`, `iPad Pro 13-inch (M5)`) and passes `--screenshot-demo`. PNGs land under `fastlane/screenshots/` (gitignored).

## SnapshotHelper updates

Vendored from [fastlane SnapshotHelper.swift](https://github.com/fastlane/fastlane/blob/master/snapshot/lib/assets/SnapshotHelper.swift) (version marker `SnapshotHelperVersion [1.30]`).

To refresh:

```bash
fastlane snapshot update
# or copy from the URL above
```

## Widget builds

This folder is independent of `Platforms/iOS/Widgets/`. MAUI widget embed (`BuildWidgetExtension`) is unchanged.
