# Screenshot Automation (fastlane snapshot) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Locally generate App Store screenshots (iPhone + iPad, `de-DE` + `en-US`) via fastlane snapshot + XCUITest, with a Debug-only demo seed and a curated copy path into `docs/screenshots/` for the README.

**Architecture:** Debug launch argument enables an isolated DataPath + fixed fixture seed. Stable `AutomationId`s on Shell tabs and key controls drive XCUITest flows that call `snapshot("…")`. fastlane Snapfile runs the device/locale matrix; a small script maps DE iPhone frames to README filenames.

**Tech Stack:** .NET 10 MAUI (`net10.0-ios`), XCUITest (Swift), fastlane `snapshot`, Ruby Bundler, xUnit for seeder unit tests.

## Global Constraints

- Wave 1 is **local Mac only** (no CI).
- Demo seed **must not** run in Release or `AppDistribution=Store` builds.
- Demo data uses an **isolated DataPath**, never the user’s normal Application Support folder unless overridden only while the launch arg is present.
- Locales: `de-DE`, `en-US`. Devices: one iPhone + one iPad Simulator (pin exact names in Snapfile after `xcrun simctl list`).
- Raw output under `fastlane/screenshots/` is **gitignored**; curated README PNGs under `docs/screenshots/` are committed when refreshed.
- Spec: `docs/superpowers/specs/2026-08-12-screenshot-automation-design.md`.

## File map

| File | Role |
|------|------|
| `Finanzuebersicht.Core/Constants/ScreenshotAutomationIds.cs` | Stable AutomationId string constants |
| `Finanzuebersicht.Core/Services/ScreenshotDemo/ScreenshotDemoFixture.cs` | Builds in-memory fixture entities (deterministic) |
| `Finanzuebersicht.Application/UseCases/ScreenshotDemo/SeedScreenshotDemoDataUseCase.cs` | Clears/seeds repositories from fixture |
| `Finanzuebersicht/Services/ScreenshotDemoBootstrap.cs` | Detects launch arg (DEBUG), sets DataPath + language, runs seed |
| `Finanzuebersicht/App.xaml.cs` | Call bootstrap before/with init (DEBUG only) |
| `Finanzuebersicht/AppShell.xaml` | AutomationIds on ShellContent |
| `Finanzuebersicht/Views/*.xaml` (FABs / pages as needed) | AutomationIds on key controls |
| `Finanzuebersicht/Platforms/iOS/UITests/` | XCUITest sources + SnapshotHelper |
| `fastlane/Snapfile`, `fastlane/Fastfile`, `Gemfile` | snapshot orchestration |
| `scripts/copy-readme-screenshots.sh` | Map DE iPhone shots → `docs/screenshots/` |
| `.gitignore` | Ignore `fastlane/screenshots/`, `fastlane/report.xml`, etc. |
| `docs/APP_STORE.md`, `docs/GUIDE.md`, `README.md` | How to run + screenshot caption |

---

### Task 1: Screenshot demo fixture + seed use case

**Files:**
- Create: `Finanzuebersicht.Core/Constants/ScreenshotAutomationIds.cs`
- Create: `Finanzuebersicht.Core/Services/ScreenshotDemo/ScreenshotDemoFixture.cs`
- Create: `Finanzuebersicht.Application/UseCases/ScreenshotDemo/SeedScreenshotDemoDataUseCase.cs`
- Create: `Finanzuebersicht.Tests/Application/UseCases/SeedScreenshotDemoDataUseCaseTests.cs`
- Modify: `Finanzuebersicht.Application/DependencyInjection/` (or existing Application DI extension) to register the use case if that is how other use cases are registered
- Modify: `Finanzuebersicht.Core/Services/SettingsKeys.cs` — add `ScreenshotDemoDataPath` only if needed; prefer computing path without a new settings key when possible

**Interfaces:**
- Consumes: `ICategoryRepository`, `IAccountRepository`, `ITransactionRepository`, `IRecurringRepository`, `IBudgetRepository`, `ISparZielRepository` (same interfaces as existing use cases)
- Produces: `SeedScreenshotDemoDataUseCase.ExecuteAsync(CancellationToken)` → seeds fixed data; `ScreenshotDemoFixture` static factory returning lists; `ScreenshotAutomationIds` public const strings

- [ ] **Step 1: Write failing unit test for seeder**

```csharp
[Fact]
public async Task ExecuteAsync_WritesDeterministicAccountsAndTransactions()
{
    var accounts = Substitute.For<IAccountRepository>();
    // … substitute other repos similarly; capture Save* calls
    var sut = new SeedScreenshotDemoDataUseCase(/* deps */);
    await sut.ExecuteAsync();
    await accounts.Received().SaveAccountAsync(Arg.Is<Account>(a => a.Name.Length > 0));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter FullyQualifiedName~SeedScreenshotDemoDataUseCaseTests`

Expected: FAIL (type/use case missing)

- [ ] **Step 3: Add AutomationId constants**

```csharp
namespace Finanzuebersicht.Core.Constants;

public static class ScreenshotAutomationIds
{
    public const string TabDashboard = "tab.dashboard";
    public const string TabTransactions = "tab.transactions";
    public const string TabRecurring = "tab.recurring";
    public const string TabManagement = "tab.management";
    public const string TabSavings = "tab.savings";
    public const string ToolbarSettings = "toolbar.settings";
    public const string FabSchnell = "fab.schnell";
    public const string PageDashboard = "page.dashboard";
    public const string PageTransactions = "page.transactions";
    public const string PageRecurring = "page.recurring";
    public const string PageManagement = "page.management";
    public const string PageSavings = "page.savings";
    public const string PageSettings = "page.settings";
    public const string SheetQuickExpense = "sheet.quick-expense";
}
```

- [ ] **Step 4: Implement fixture + use case**

`ScreenshotDemoFixture` creates:
- 2 accounts (Giro + Sparkonto) with opening balances
- Categories: reuse system keys where possible + enough for donut chart
- ~8–12 transactions across current and previous month (income + expenses)
- 1 active recurring (e.g. rent)
- 1 sparziel with progress
- Optional category budget

`SeedScreenshotDemoDataUseCase`:
1. Delete/clear existing entities if repositories support it; otherwise overwrite via known IDs and delete extras if APIs exist. Prefer writing a dedicated wipe helper that uses existing repository methods only (no raw file IO in Application).
2. Save fixture entities in dependency order (accounts/categories → transactions/recurring/budgets/sparziele).

Register in Application DI the same way as other use cases.

- [ ] **Step 5: Run tests — expect PASS**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter FullyQualifiedName~SeedScreenshotDemoDataUseCaseTests`

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add Finanzuebersicht.Core/Constants/ScreenshotAutomationIds.cs \
  Finanzuebersicht.Core/Services/ScreenshotDemo/ScreenshotDemoFixture.cs \
  Finanzuebersicht.Application/UseCases/ScreenshotDemo/SeedScreenshotDemoDataUseCase.cs \
  Finanzuebersicht.Tests/Application/UseCases/SeedScreenshotDemoDataUseCaseTests.cs \
  # + DI registration file(s)
git commit -m "feat(screenshot): add demo fixture and seed use case"
```

---

### Task 2: Debug bootstrap (launch arg + isolated DataPath)

**Files:**
- Create: `Finanzuebersicht/Services/ScreenshotDemoBootstrap.cs`
- Modify: `Finanzuebersicht/App.xaml.cs` (constructor / `OnStart`)
- Modify: `Finanzuebersicht/MauiProgram.cs` if bootstrap needs DI registration
- Test: `Finanzuebersicht.Tests/` — unit-test pure helpers (arg detection / path resolution) if extracted to Core; otherwise keep logic thin and cover via compile + manual smoke

**Interfaces:**
- Consumes: `SeedScreenshotDemoDataUseCase`, `ISettingsService`, `ILocalizationService`
- Produces: `ScreenshotDemoBootstrap.TryApplyAsync(...)` returns `bool` (true if demo mode active)

- [ ] **Step 1: Implement launch-arg detection (DEBUG only)**

```csharp
public static class ScreenshotDemoBootstrap
{
    public const string LaunchArgument = "--screenshot-demo";

    public static bool IsRequested()
    {
#if !DEBUG
        return false;
#else
        return Environment.GetCommandLineArgs()
            .Any(a => string.Equals(a, LaunchArgument, StringComparison.Ordinal));
#endif
    }

    public static string GetIsolatedDataPath()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Finanzuebersicht",
            "screenshot-demo");
        Directory.CreateDirectory(root);
        return root;
    }
}
```

On iOS Simulator, also check `App.Current` / process args that snapshot injects (`-AppleLanguages`, and custom args via `launch_arguments` in Snapfile). Document that Snapfile must pass `--screenshot-demo`.

- [ ] **Step 2: Wire bootstrap in `App` before data init**

When `IsRequested()`:
1. `settings.Set(SettingsKeys.DataPath, GetIsolatedDataPath())` (or pending path pattern if DataPath is only applied on restart — if so, set active DataPath the same way tests do in `LocalDataServiceEdgeCaseTests`).
2. Clear `SettingsKeys.LanguageCode` so UI follows Simulator locale.
3. Set `SettingsKeys.OnboardingCompleted` to `"true"`.
4. Set theme to Light for consistency: `SettingsKeys.Theme` = Light (or System).
5. After `InitializeAsync` (or instead of empty-state defaults), call `SeedScreenshotDemoDataUseCase.ExecuteAsync()`.

Ensure Store/Release: entire path compiled out or no-ops via `#if DEBUG`.

- [ ] **Step 3: Manual smoke (Simulator)**

Run (adjust SDK path if needed):

```bash
export DEVELOPER_DIR="/Users/thomas/Downloads/Xcode-beta 2.app/Contents/Developer"
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios -c Debug \
  -p:RuntimeIdentifier=iossimulator-arm64
# Launch with --screenshot-demo via `xcrun simctl launch` or Xcode scheme args
```

Expected: App opens with seeded demo data, not empty onboarding; real DataPath untouched.

- [ ] **Step 4: Commit**

```bash
git add Finanzuebersicht/Services/ScreenshotDemoBootstrap.cs Finanzuebersicht/App.xaml.cs Finanzuebersicht/MauiProgram.cs
git commit -m "feat(screenshot): bootstrap demo mode with isolated DataPath"
```

---

### Task 3: AutomationIds on Shell and key pages

**Files:**
- Modify: `Finanzuebersicht/AppShell.xaml` (+ `AppShell.xaml.cs` if ToolbarItem needs code-behind AutomationId)
- Modify: `Finanzuebersicht/Views/DashboardPage.xaml`
- Modify: `Finanzuebersicht/Views/TransactionsPage.xaml`
- Modify: `Finanzuebersicht/Views/RecurringTransactionsPage.xaml`
- Modify: `Finanzuebersicht/Views/CategoriesPage.xaml`
- Modify: `Finanzuebersicht/Views/SparZielePage.xaml`
- Modify: `Finanzuebersicht/Views/SettingsPage.xaml`
- Modify: Schnell FAB control / Dashboard FAB binding site for `ScreenshotAutomationIds.FabSchnell`
- Modify: Quick expense sheet root view for `ScreenshotAutomationIds.SheetQuickExpense`

**Interfaces:**
- Consumes: `ScreenshotAutomationIds` constants
- Produces: Elements discoverable in XCUITest via `app.otherElements["tab.dashboard"]` (or `buttons` / `tabBars` — verify with Accessibility Inspector; adjust query type in Task 4 if Shell maps ids differently)

- [ ] **Step 1: Set ShellContent AutomationIds**

```xml
<ShellContent
    AutomationId="{x:Static constants:ScreenshotAutomationIds.TabDashboard}"
    ...
    Route="DashboardPage" />
```

Add `xmlns:constants` to AppShell. Repeat for all five tabs.

- [ ] **Step 2: Set page root AutomationIds** on each main `ContentPage` (`AutomationId` on the page or root layout).

- [ ] **Step 3: Settings toolbar**

Set `AutomationId` on the Settings `ToolbarItem` to `toolbar.settings` (may require code-behind if XAML property unsupported on ToolbarItem — use `SetValue` / platform handler if needed).

- [ ] **Step 4: FAB + sheet**

Assign `fab.schnell` and `sheet.quick-expense` on the Dashboard Schnell control and the quick-expense sheet root.

- [ ] **Step 5: Build iOS simulator target**

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64
```

Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Finanzuebersicht/AppShell.xaml Finanzuebersicht/Views/ Finanzuebersicht/Controls/
git commit -m "feat(screenshot): add AutomationIds for UITest navigation"
```

---

### Task 4: XCUITest target + SnapshotHelper + one flow

**Files:**
- Create: `Finanzuebersicht/Platforms/iOS/UITests/FinanzuebersichtUITests.swift` (or `.m` — prefer Swift)
- Create: `Finanzuebersicht/Platforms/iOS/UITests/SnapshotHelper.swift` (from fastlane snapshot template)
- Create: `Finanzuebersicht/Platforms/iOS/UITests/project.yml` (XcodeGen) **or** checked-in `.xcodeproj` — pick XcodeGen to match widget workflow
- Create: `Finanzuebersicht/Platforms/iOS/UITests/README.md` — how to regenerate/open project
- Modify: build scripts only if needed to point snapshot at the `.app` under `bin/Debug/net10.0-ios/iossimulator-*/`

**Interfaces:**
- Consumes: App bundle id `de.thomasmenzl.finanzuebersicht`; AutomationIds from Task 1
- Produces: UITest that launches app with `--screenshot-demo` and takes at least `snapshot("01-dashboard")`

- [ ] **Step 1: Scaffold UITest project via XcodeGen**

`project.yml` target `FinanzuebersichtUITests` type `bundle.ui-testing`, host app configured for bundle id `de.thomasmenzl.finanzuebersicht`.

- [ ] **Step 2: Add SnapshotHelper**

Run `fastlane snapshot init` once locally and copy `SnapshotHelper.swift` into `Platforms/iOS/UITests/`, or vendor the current helper from fastlane’s template.

- [ ] **Step 3: Write first UI test**

```swift
func testScreenshots() throws {
    let app = XCUIApplication()
    setupSnapshot(app)
    app.launchArguments += ["--screenshot-demo"]
    app.launch()

    XCTAssertTrue(app.otherElements["page.dashboard"].waitForExistence(timeout: 30))
    snapshot("01-dashboard")
}
```

If `otherElements` fails, try `app.descendants(matching: .any)["page.dashboard"]` and document the working query.

- [ ] **Step 4: Build MAUI app for simulator, then run UITest for one device**

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64
# Open/generate UITests xcodeproj; set TEST_HOST / app path per UITests README
xcodebuild test -project … -scheme FinanzuebersichtUITests \
  -destination 'platform=iOS Simulator,name=iPhone 16 Pro'
```

Expected: Test passes; a snapshot PNG appears under `fastlane/screenshots` or DerivedData snapshot folder (depending on helper config).

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht/Platforms/iOS/UITests/
git commit -m "feat(screenshot): add XCUITest target and first snapshot flow"
```

---

### Task 5: Full UITest flow set + fastlane Snapfile

**Files:**
- Modify: `Finanzuebersicht/Platforms/iOS/UITests/FinanzuebersichtUITests.swift` — remaining screens
- Create: `Gemfile`
- Create: `fastlane/Snapfile`
- Create: `fastlane/Fastfile`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: UITest from Task 4
- Produces: `bundle exec fastlane screenshots` → PNGs for 2 devices × 2 locales

- [ ] **Step 1: Extend UITest snapshots**

After dashboard, navigate and snapshot:

| Order | Name | Action |
|------:|------|--------|
| 02 | `02-transactions` | Tap `tab.transactions` |
| 03 | `03-quick-expense` | Tap `fab.schnell` (return to Dashboard first if needed); wait for `sheet.quick-expense` |
| 04 | `04-recurring` | Dismiss sheet if open; tap `tab.recurring` |
| 05 | `05-management` | Tap `tab.management` |
| 06 | `06-savings` | Tap `tab.savings` |
| 07 | `07-settings` | Tap `toolbar.settings` |

Use short waits on page AutomationIds between taps.

- [ ] **Step 2: Add Gemfile**

```ruby
source "https://rubygems.org"
gem "fastlane"
```

- [ ] **Step 3: Snapfile**

```ruby
devices([
  "iPhone 16 Pro",
  "iPad Pro 13-inch (M4)"
])
languages(["de-DE", "en-US"])
scheme("FinanzuebersichtUITests")
output_directory("./fastlane/screenshots")
clear_previous_screenshots(true)
override_status_bar(true)
launch_arguments(["--screenshot-demo"])
# project / workspace paths per UITests README
```

Adjust device names to whatever `xcrun simctl list devices available` shows on the machine.

- [ ] **Step 4: Fastfile lane**

```ruby
lane :screenshots do
  capture_screenshots
end
```

Document prerequisite: build MAUI `.app` for simulator before snapshot if the UITest host does not build MAUI itself.

- [ ] **Step 5: Gitignore raw output**

```
fastlane/screenshots/
fastlane/report.xml
fastlane/test_output/
fastlane/Preview.html
vendor/bundle/
```

- [ ] **Step 6: Run full matrix once**

```bash
bundle install
bundle exec fastlane screenshots
```

Expected: PNGs under `fastlane/screenshots/de-DE/` and `en-US/` for both devices.

- [ ] **Step 7: Commit** (no PNGs)

```bash
git add Gemfile Gemfile.lock fastlane/Snapfile fastlane/Fastfile .gitignore \
  Finanzuebersicht/Platforms/iOS/UITests/
git commit -m "feat(screenshot): wire fastlane snapshot for iPhone/iPad DE+EN"
```

---

### Task 6: README bridge script + documentation

**Files:**
- Create: `scripts/copy-readme-screenshots.sh`
- Modify: `docs/APP_STORE.md` — screenshot automation section
- Modify: `docs/GUIDE.md` — short “Screenshots” subsection
- Modify: `README.md` — caption + link to how-to; after first successful run, replace `docs/screenshots/*.png` and set caption to v1.20

**Interfaces:**
- Consumes: `fastlane/screenshots/de-DE/<iPhone>/01-dashboard.png` etc.
- Produces: Updated `docs/screenshots/` filenames matching README

- [ ] **Step 1: Write copy script**

```bash
#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="${ROOT}/fastlane/screenshots/de-DE"
# Resolve first iPhone folder under de-DE (name varies)
IPHONE_DIR="$(find "$SRC" -maxdepth 1 -type d -name 'iPhone*' | head -1)"
DEST="${ROOT}/docs/screenshots"
cp "${IPHONE_DIR}/01-dashboard.png" "${DEST}/dashboard-monat.png"
cp "${IPHONE_DIR}/02-transactions.png" "${DEST}/transaktionen.png"
# … map remaining existing README assets that have a counterpart;
# leave unmapped legacy files until a matching shot exists
echo "Copied README screenshots from ${IPHONE_DIR}"
```

Make executable: `chmod +x scripts/copy-readme-screenshots.sh`.

- [ ] **Step 2: Document in APP_STORE.md**

Add section: prerequisites (Xcode, Bundler, Simulator names), `dotnet build … iossimulator`, `bundle exec fastlane screenshots`, `scripts/copy-readme-screenshots.sh`, note that raw shots are gitignored.

- [ ] **Step 3: GUIDE.md + README caption**

After a successful local run (or leave caption until PNGs replaced): update README screenshot note from v1.17 to the version that produced the new images.

- [ ] **Step 4: Commit docs + script (+ PNG refresh if generated in this session)**

```bash
git add scripts/copy-readme-screenshots.sh docs/APP_STORE.md docs/GUIDE.md README.md docs/screenshots/
git commit -m "docs(screenshot): document fastlane flow and README copy script"
```

---

## Spec coverage check

| Spec item | Task |
|-----------|------|
| Demo seed + launch arg + isolated path | 1–2 |
| AutomationIds | 1, 3 |
| XCUITest flows (7 screens) | 4–5 |
| fastlane devices + locales | 5 |
| README bridge | 6 |
| No CI / no frameit / Debug-only safety | Global + Task 2 |
| gitignore raw screenshots | 5 |

## Placeholder / consistency check

- Launch arg name fixed: `--screenshot-demo`
- AutomationId strings fixed in `ScreenshotAutomationIds`
- Snapshot names fixed `01-…` through `07-…`
- Device strings may need local adjustment — Snapfile comment requires verifying with `simctl list`

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-12-screenshot-automation.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks  
2. **Inline Execution** — execute tasks in this session with checkpoints  

Which approach?
