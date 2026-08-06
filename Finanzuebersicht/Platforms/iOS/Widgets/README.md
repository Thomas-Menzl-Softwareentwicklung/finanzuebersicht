# Quick Expense Widget (iOS)

Interactive Home Screen capture for small expenses (**Pro**). In-App „Schnell“ + WidgetKit-`.appex` are implemented.

> **Lokaler Cursor-Agent (Mac):** Bei Swift-/Embed-/Resign-Änderungen zuerst `.cursor/rules/ios-quick-expense-widget.mdc`, dann den Abschnitt **Mac-Agent: Xcode-Extension bündeln** unten. Domain/UI nicht neu bauen.

## Runtime flow

1. Widget App Intent writes `{ amountText, title }` into the App Group file `quick-expense-pending.json`.
2. MAUI app on start/resume drains the file via `ProcessQuickExpenseInboxUseCase` → `CaptureQuickExpenseUseCase` (after license refresh; without Pro, pending items stay).
3. Result: real `Transaction` (Ausgabe) with system category **Unkategorisiert** + default account.
4. In-app: Transaktionen → **Schnell** sheet (same use case) + filter **Unkategorisiert (n)**.
5. Pencil / Anpassen: deep link `finanzuebersicht://quick-expense` opens Schnell with preset prefill.

App Group id: `group.de.thomasmenzl.finanzuebersicht` (`AppGroupIds.Finanzuebersicht`).

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

## In-app (all targets)

Transaktionen → Schnell works on iOS, Mac Catalyst, and Windows. The Home Screen widget is **iOS-only**; Mac/Windows use In-App only.

---

## Mac-Agent: Xcode-Extension bündeln

**Ziel:** Aus den Swift-Quellen ein signiertes Widget Extension Bundle (`.appex`) erzeugen und in die MAUI-iOS-App einbetten. **Nicht** die gesamte App nach Xcode portieren.

### Voraussetzungen

- Mac mit Xcode 16+ (iOS 17+ SDK wegen App Intents)
- Apple Developer Team, App ID `de.thomasmenzl.finanzuebersicht`
- .NET 10 + MAUI workload (`dotnet workload install maui`)

### 1. Developer Portal

1. Capability **App Groups** auf der Haupt-App-ID aktivieren.
2. Gruppe anlegen/zuweisen: `group.de.thomasmenzl.finanzuebersicht`.
3. Neue App ID für die Extension, z. B. `de.thomasmenzl.finanzuebersicht.QuickExpenseWidget`, mit derselben App Group.
4. Provisioning Profiles (Development + ggf. App Store) für Haupt-App **und** Extension aktualisieren / neu erzeugen.

### 2. Extension-Target (kein zweites App-Projekt)

Quellen und XcodeGen-Config (im Repo):

| Datei | Rolle |
|-------|--------|
| `Platforms/iOS/Widgets/project.yml` | XcodeGen: Host `WidgetHost` + Extension `QuickExpenseWidget` |
| `Platforms/iOS/Widgets/QuickExpenseWidget/` | Swift, `Info.plist`, Entitlements, Assets |
| `Platforms/iOS/Widgets/WidgetHost/` | Throwaway-Host (wird nicht mit der MAUI-App ausgeliefert) |
| `Platforms/iOS/Widgets/build-release.sh` | Device + Simulator `.appex` → `Finanzuebersicht/WidgetExtensions/` (außerhalb von `Platforms/`) |

```bash
brew install xcodegen
cd Finanzuebersicht/Platforms/iOS/Widgets
xcodegen generate
./build-release.sh
# oder: wird bei `dotnet build -f net10.0-ios` via Target BuildWidgetExtension automatisch gebaut
```

Erwartetes Produkt: `QuickExpenseWidget.appex` (Bundle ID `de.thomasmenzl.finanzuebersicht.QuickExpenseWidget`).

### 3. In MAUI-iOS-App einbetten

Muster: [Redth / Maui.Apple.PlatformFeature.Samples — Widgets](https://github.com/Redth/Maui.Apple.PlatformFeature.Samples/tree/main/Widgets).

**Umgesetzt in `Finanzuebersicht.csproj` (nur `net10.0-ios`):**

| MSBuild | Bedeutung |
|---------|-----------|
| `BuildWidgetExtension` | Nach `ResolveReferences`: `xcodegen` (falls nötig) + `xcodebuild` → staged `.appex` |
| `AdditionalAppExtensions` | Name=`QuickExpenseWidget`, Output `Release-iphoneos` / `Release-iphonesimulator` |
| `CodesignEntitlements` | `Platforms/iOS/Entitlements.WidgetExtension.plist` (App Group) |
| `-p:SkipWidgetBuild=true` | `.appex` nicht neu bauen (bereits gestaged) |
| `-p:EmbedQuickExpenseWidget=false` | Kein Widget-Embed (z. B. ohne Xcode) |

Nach erfolgreichem Embed: `…/Finanzübersicht.app/PlugIns/QuickExpenseWidget.appex` existiert.

Haupt-App Entitlements (`Platforms/iOS/Entitlements*.plist`) enthalten die App Group bereits.

### 4. Manueller Test (Gerät)

1. `dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios -c Debug` (Signing/Profiles wie lokal üblich).
2. App installieren; unter Einstellungen ggf. Pro freischalten (Store-Sandbox) bzw. Direct-Build = immer Pro.
3. Home Screen → Widget hinzufügen → „Quick expense“ / Schnelle Ausgabe.
4. Betrag + Info speichern (Intent).
5. App öffnen (Resume) → Ausgabe unter Unkategorisiert; Saldo aktualisiert.
6. Free/Store ohne Pro: Intent zeigt Pro-Hinweis, keine Buchung.

### 5. Definition of Done (Mac-Teil)

- [ ] App Group im Portal an Haupt-App + Extension (manuell im Developer Portal)
- [x] XcodeGen `project.yml` + Host + Extension-Quellen + `build-release.sh`
- [x] MAUI-Embed: `BuildWidgetExtension` + `AdditionalAppExtensions` (`QuickExpenseWidget`) in `Finanzuebersicht.csproj`
- [x] `.appex` baut mit Xcode/`build-release.sh` (Device + Simulator)
- [x] MAUI-iOS-Build enthält `PlugIns/QuickExpenseWidget.appex`
- [ ] Widget speichert → App drain → Unkategorisiert-Transaktion (Gerätetest)
- [ ] Pro-Gate im Intent (Shared `hasPro`) auf Gerät verifizieren

**Embed-Namen für Wiederholung:** Target `BuildWidgetExtension` (ruft `Platforms/iOS/Widgets/build-release.sh` auf); Item `AdditionalAppExtensions` mit `<Name>QuickExpenseWidget</Name>`; Staging unter `Finanzuebersicht/WidgetExtensions/Release-{iphoneos|iphonesimulator}/` (**nicht** unter `Platforms/`, sonst packt MAUI ein unsigniertes Duplikat mit).

`CFBundleShortVersionString` / `CFBundleVersion` im `.appex` werden aus NBGV (`MajorMinorVersion` / `BuildVersionSimple`) gesetzt — beim Widget-Build und erneut beim Store-Resign (`sign-widget-store.sh`), damit ASC 90473 nicht greift.

Wenn `xcode-select` nur auf die Command Line Tools zeigt: `build-release.sh` sucht automatisch Xcode unter `/Applications` und `~/Downloads`, oder `export DEVELOPER_DIR=…/Xcode*.app/Contents/Developer`.

### Referenz-Konstanten (müssen matchen)

| Symbol | Wert |
|--------|------|
| App Group | `group.de.thomasmenzl.finanzuebersicht` |
| Pending file | `quick-expense-pending.json` |
| Pro key | `hasPro` |
| Haupt Bundle ID | `de.thomasmenzl.finanzuebersicht` |
| Extension Bundle ID (Vorschlag) | `de.thomasmenzl.finanzuebersicht.QuickExpenseWidget` |

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
