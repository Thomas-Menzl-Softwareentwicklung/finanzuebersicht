# Quick Expense Widget (iOS)

Interactive Home Screen capture for small expenses (**Pro**).

## Runtime flow

1. Widget App Intent writes `{ amountText, title }` into the App Group file `quick-expense-pending.json`.
2. MAUI app on start/resume drains the file via `ProcessQuickExpenseInboxUseCase` → `CaptureQuickExpenseUseCase`.
3. Result: real `Transaction` (Ausgabe) with system category **Unkategorisiert** + default account.
4. In-app: Transaktionen → **Schnell** sheet (same use case) + filter **Unkategorisiert (n)**.

App Group id: `group.com.thomasmenzl.finanzuebersicht` (`AppGroupIds.Finanzuebersicht`).

## In-app (ships with this change)

Works on all targets without the extension: Transaktionen → Schnell.

## Widget extension (Mac + Xcode)

Swift sources live in `Finanzuebersicht/Platforms/iOS/Widgets/QuickExpenseWidget/`.

The Linux/CI environment cannot compile WidgetKit. On a Mac:

1. App Store Connect / Developer portal: enable **App Groups** on App ID `com.thomasmenzl.finanzuebersicht` and create a Widget Extension App ID (e.g. `com.thomasmenzl.finanzuebersicht.QuickExpenseWidget`) with the same group.
2. Add both App IDs to the group `group.com.thomasmenzl.finanzuebersicht`.
3. Create an Xcode Widget Extension target (or use [xcodegen](https://github.com/yonaskolb/XcodeGen)) pointing at `QuickExpenseWidget.swift` + entitlements.
4. Bundle the `.appex` into the MAUI iOS app (see [Redth Maui Apple Widgets sample](https://github.com/Redth/Maui.Apple.PlatformFeature.Samples/tree/main/Widgets)).
5. Main app entitlements already declare the App Group (`Platforms/iOS/Entitlements*.plist`).

Until the `.appex` is bundled, users still get full value from the in-app Schnell sheet; the inbox processor is ready for widget writes.

## Pro gate

- `AppFeature.QuickExpenseCapture` → `HasPro`
- App publishes `hasPro` into App Group `UserDefaults` for the Intent to check
- Free: Upsell in app; Intent refuses save

## Related

- Anzeige-Widget (Saldo/KPIs): issue #242 (separate)
- Monetization: `docs/MONETIZATION.md`
