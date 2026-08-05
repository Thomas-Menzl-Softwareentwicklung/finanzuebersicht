# Quick Expense Widget (iOS)

Interactive Home Screen capture for small expenses (**Pro**).

> **Lokaler Cursor-Agent (Mac):** Lies zuerst `.cursor/rules/ios-quick-expense-widget.mdc`, dann den Abschnitt **Mac-Agent: Xcode-Extension bündeln** unten. Die In-App-Schnellerfassung ist fertig — dein Fokus ist nur die WidgetKit-`.appex`.

## Runtime flow

1. Widget App Intent writes `{ amountText, title }` into the App Group file `quick-expense-pending.json`.
2. MAUI app on start/resume drains the file via `ProcessQuickExpenseInboxUseCase` → `CaptureQuickExpenseUseCase`.
3. Result: real `Transaction` (Ausgabe) with system category **Unkategorisiert** + default account.
4. In-app: Transaktionen → **Schnell** sheet (same use case) + filter **Unkategorisiert (n)**.

App Group id: `group.com.thomasmenzl.finanzuebersicht` (`AppGroupIds.Finanzuebersicht`).

JSON shape (camelCase, array):

```json
[
  {
    "id": "uuid",
    "amountText": "3.50",
    "title": "Coffee",
    "createdAt": "2026-08-05T12:00:00+00:00"
  }
]
```

Pro flag for the Intent: App Group `UserDefaults` key `hasPro` (written by `AppGroupQuickExpenseInboxStore.PublishHasPro`).

## In-app (ships without the .appex)

Works on all targets: Transaktionen → Schnell. Inbox processor is ready for widget writes once the extension is bundled.

---

## Mac-Agent: Xcode-Extension bündeln

**Ziel:** Aus den Swift-Quellen ein signiertes Widget Extension Bundle (`.appex`) erzeugen und in die MAUI-iOS-App einbetten. **Nicht** die gesamte App nach Xcode portieren.

### Voraussetzungen

- Mac mit Xcode 16+ (iOS 17+ SDK wegen App Intents)
- Apple Developer Team, App ID `com.thomasmenzl.finanzuebersicht`
- .NET 10 + MAUI workload (`dotnet workload install maui`)

### 1. Developer Portal

1. Capability **App Groups** auf der Haupt-App-ID aktivieren.
2. Gruppe anlegen/zuweisen: `group.com.thomasmenzl.finanzuebersicht`.
3. Neue App ID für die Extension, z. B. `com.thomasmenzl.finanzuebersicht.QuickExpenseWidget`, mit derselben App Group.
4. Provisioning Profiles (Development + ggf. App Store) für Haupt-App **und** Extension aktualisieren / neu erzeugen.

### 2. Extension-Target (kein zweites App-Projekt)

Quellen (bereits im Repo):

- `Finanzuebersicht/Platforms/iOS/Widgets/QuickExpenseWidget/QuickExpenseWidget.swift`
- `Finanzuebersicht/Platforms/iOS/Widgets/QuickExpenseWidget/QuickExpenseWidget.entitlements`

Option A — **XcodeGen** (bevorzugt, reproduzierbar): kleines `project.yml` neben den Swift-Dateien, Target `widgetExtension`, Bundle ID wie oben, Deployment iOS 17+, Entitlements-Datei verknüpfen. `xcodegen generate` → `xcodebuild -scheme … -sdk iphoneos` (bzw. Simulator).

Option B — **Xcode UI einmalig:** File → New → Target → Widget Extension; Swift-Inhalt durch unsere Datei ersetzen; App Group Capability setzen; Bundle ID setzen. Xcode-Projekt idealerweise unter `Finanzuebersicht/Platforms/iOS/Widgets/` versionieren (oder nur die Build-Outputs + Gen-Config).

Erwartetes Produkt: `QuickExpenseWidget.appex`.

### 3. In MAUI-iOS-App einbetten

Muster: [Redth / Maui.Apple.PlatformFeature.Samples — Widgets](https://github.com/Redth/Maui.Apple.PlatformFeature.Samples/tree/main/Widgets).

Typisch:

1. Extension mit `xcodebuild` bauen (Release/Device oder Simulator passend zum MAUI-Target).
2. In `Finanzuebersicht.csproj` (nur `net10.0-ios`) z. B. `NativeReference` mit `Kind=AppExtension` auf das `.appex` **oder** MSBuild-Target, das nach `_CompileAppManifest` / Bundle-Phase nach `PlugIns/` kopiert.
3. Sicherstellen: Extension und Haupt-App dieselbe Team-ID, App Group in **beiden** Entitlements (Haupt-App: `Platforms/iOS/Entitlements*.plist` — App Group ist schon eingetragen).

Nach erfolgreichem Embed: `…/Finanzübersicht.app/PlugIns/QuickExpenseWidget.appex` existiert.

### 4. Manueller Test (Gerät)

1. `dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios -c Debug` (Signing/Profiles wie lokal üblich).
2. App installieren; unter Einstellungen ggf. Pro freischalten (Store-Sandbox) bzw. Direct-Build = immer Pro.
3. Home Screen → Widget hinzufügen → „Quick expense“ / Schnelle Ausgabe.
4. Betrag + Info speichern (Intent).
5. App öffnen (Resume) → Ausgabe unter Unkategorisiert; Saldo aktualisiert.
6. Free/Store ohne Pro: Intent zeigt Pro-Hinweis, keine Buchung.

### 5. Definition of Done (Mac-Teil)

- [ ] App Group im Portal an Haupt-App + Extension
- [ ] `.appex` baut mit Xcode/xcodebuild
- [ ] MAUI-iOS-Build enthält `PlugIns/*.appex`
- [ ] Widget speichert → App drain → Unkategorisiert-Transaktion
- [ ] Pro-Gate im Intent (Shared `hasPro`)
- [ ] Kurz in diesem README oder PR notieren, wie der Embed-Schritt in der `.csproj` heißt (für CI/Mac-Wiederholung)

### Referenz-Konstanten (müssen matchen)

| Symbol | Wert |
|--------|------|
| App Group | `group.com.thomasmenzl.finanzuebersicht` |
| Pending file | `quick-expense-pending.json` |
| Pro key | `hasPro` |
| Haupt Bundle ID | `com.thomasmenzl.finanzuebersicht` |
| Extension Bundle ID (Vorschlag) | `com.thomasmenzl.finanzuebersicht.QuickExpenseWidget` |

---

## Pro gate

- `AppFeature.QuickExpenseCapture` → `HasPro`
- App publishes `hasPro` into App Group `UserDefaults` for the Intent
- Free: Upsell in app; Intent refuses save

## Related

- Cursor-Regel: `.cursor/rules/ios-quick-expense-widget.mdc`
- Anzeige-Widget (Saldo/KPIs): issue #242 (separate)
- Monetization: `docs/MONETIZATION.md`
- App Store: `docs/APP_STORE.md`
