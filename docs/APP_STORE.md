# App Store Vorbereitung (iPhone / iPad) + TestFlight

Legal-/Support-Seiten: separates Repo
[`finanzuebersicht-site`](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht-site)
→ `https://finanzuebersicht.thomasmenzl.de/`.

Monetarisierung: [`MONETIZATION.md`](MONETIZATION.md).

## Status

| Thema | Status |
|-------|--------|
| Bundle ID `de.thomasmenzl.finanzuebersicht` | vorhanden |
| iPhone + iPad | vorhanden |
| Privacy Manifest + Export Compliance | gesetzt |
| iOS Release-Entitlements (ohne `get-task-allow`) | gesetzt (App Group für Quick-Expense-Widget; **iCloud CloudKit** + **Push** für Sync #243) |
| Quick Expense Widget (Pro); In-App Schnell Free | ✅ In-App alle Targets; WidgetKit-`.appex` eingebettet — `Platforms/iOS/Widgets/README.md` |
| Support / Privacy Site | eigenes Repo `finanzuebersicht-site` |
| License-Gates Free/Pro/Sync | vorhanden |
| StoreKit (Pro kaufen / Restore) | vorhanden (Store-Build, iOS/Mac Catalyst) |
| License-Stub-UI (Dev-Toggles) | nur Debug; Release ignoriert Stub-Entitlements |
| Sync-IAP Verkauf | **1.21:** Engine + Listing + Privacy-Site; Verkauf nach ASC-Freigabe (`IsCloudSyncImplemented` = `true` auf unterstützten Store-Apple-Builds) |
| App Store Connect App + Zertifikate | **manuell** |
| TestFlight IPA Upload | **manuell auf dem Mac** |
| Store-Screenshots | Automatisierung lokal (`fastlane snapshot`) — siehe [Screenshot-Automatisierung](#screenshot-automatisierung) |
| Listing-Texte DE/EN + Screenshot-Upload | API lokal (`bundle exec fastlane upload_listing`) — siehe [Listing hochladen](#listing-hochladen-app-store-connect) |

## Product IDs (App Store Connect)

| Produkt | Typ | Product ID |
|---------|-----|------------|
| Finanzübersicht Pro | Non-Consumable | `de.thomasmenzl.finanzuebersicht.pro` |
| Finanzübersicht Sync | Auto-Renewable (1 Jahr) | `de.thomasmenzl.finanzuebersicht.sync.yearly` |

Sync-Engine ist im Store-Apple-Binary (`IsCloudSyncImplemented = true` bei Store + iOS 17 / Mac Catalyst 17). Listing (DE/EN) und öffentliche Privacy-/Support-Seiten beschreiben optionales iCloud-Sync; das Jahresabo ist erst nach ASC-Freigabe von **1.21** im Store käuflich. Drei-Geräte-QA (iPhone → iPad → iMac) ist in [`docs/CLOUDKIT_SYNC_QA.md`](CLOUDKIT_SYNC_QA.md) dokumentiert ([#243](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/243)). Sync ist **opt-in**; Finanzdaten liegen in der **privaten iCloud** des Nutzers (kein eigener Sync-Server von Finanzübersicht). Technik: `Finanzuebersicht/Platforms/iOS/Native/README-CloudKitSync.md`.

## 1. Apple Developer + App Store Connect

1. App ID `de.thomasmenzl.finanzuebersicht` (Capabilities: In-App Purchase; **App Groups** `group.de.thomasmenzl.finanzuebersicht` für Quick-Expense-Widget; **iCloud (CloudKit)** mit Container `iCloud.de.thomasmenzl.finanzuebersicht`. Push Notifications für CKSyncEngine sind **nicht** verdrahtet und derzeit nicht in den Entitlements — nur Haupt-App, nicht die Widget-Extension).
2. Zertifikate: **Apple Development** + **Apple Distribution**.
3. Profiles: Development + **App Store**.
4. ASC: iOS-App anlegen (gleiche Bundle-ID).
5. ASC → Monetization → In-App Purchases:
   - Pro (Non-Consumable), Preis z. B. 5,99 €
   - Sync (Auto-Renewable Yearly), unabhängig von Pro
6. Sandbox-Tester unter Users and Access → Sandbox.

## 2. Legal-URLs

Site-Repo: [finanzuebersicht-site](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht-site)

In App Store Connect:

- **Support URL** → `https://finanzuebersicht.thomasmenzl.de/`
- **Privacy Policy URL** → `https://finanzuebersicht.thomasmenzl.de/privacy.html`
- **Terms of Use (EULA)** → Apple-Standard-EULA in der **App-Beschreibung** (kein Custom-EULA in App Information): `https://www.apple.com/legal/internet-services/itunes/dev/stdeula/`

Bei Auto-Renew-Abos prüft Apple die Produktseite auf einen klickbaren EULA-Link (Guideline 3.1.2). Der Standard-EULA erscheint nicht von selbst als Link — deshalb steht die URL in `fastlane/metadata/*/description.txt`. In der App: Einstellungen → Lizenz.

Fallback: `https://thomas-menzl-softwareentwicklung.github.io/finanzuebersicht-site/`

## 3. Store-Build lokal (Mac)

**Vor jedem Upload:** Änderungen committen. Nerdbank.GitVersioning setzt `CFBundleVersion` aus der Git-Höhe — ohne neuen Commit bleibt die Build-Nummer gleich und App Store Connect lehnt den Upload ab. Danach publishen (nie uncommittete Store-IPA bauen).

Store-Distribution einschalten (`APP_DISTRIBUTION_STORE`):

```bash
dotnet publish Finanzuebersicht/Finanzuebersicht.csproj \
  -f net10.0-ios \
  -c Release \
  -p:AppDistribution=Store \
  -p:ArchiveOnBuild=true \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Distribution" \
  -p:CodesignProvision="NAME_DES_APP_STORE_PROFILES"
```

Oder in Xcode: Archive öffnen → Distribute App → **App Store Connect** → Upload (TestFlight).

Nach dem Publish (Working Tree muss sauber sein):

```bash
bundle exec fastlane upload_ipa
```

Das lädt die IPA per App-Store-Connect-API nach TestFlight (`upload_to_testflight`). Kein Transporter.app, kein Review. IPA-Pfad: `Finanzuebersicht/bin/Release/net10.0-ios/ios-arm64/publish/Finanzuebersicht.ipa`.

### TestFlight-Checkliste

1. IPA hochladen: `bundle exec fastlane upload_ipa` (oder Transporter / Xcode Organizer).
2. In ASC → TestFlight: Export Compliance beantworten (`ITSAppUsesNonExemptEncryption=false` → in der Regel „Nein“).
3. Interne Tester hinzufügen (oder externe Gruppe + Beta-Review).
4. Auf Gerät mit **Sandbox Apple ID** Pro-Kauf testen (Einstellungen → Lizenz → Pro freischalten / Käufe wiederherstellen).
5. Direct/GitHub-Builds bleiben ohne StoreKit-Limits (weiterhin voll lokal).

## 4. Listing hochladen (App Store Connect)

Texte liegen im Repo unter `fastlane/metadata/` (`de-DE` + `en-US`). Dieselbe Dateien gelten für iOS und Mac. Display-Name in ASC nicht per Datei überschreiben (kein `name.txt`).

API-Key (lokal, nicht im Repo) — derselbe wie SimpleTD:

| Stück | Ort |
|------|-----|
| `.p8` | `~/.appstoreconnect/private_keys/AuthKey_<KEY_ID>.p8` |
| Fastlane-JSON | `~/.appstoreconnect/api_key.json` |

```bash
python3 scripts/check-asc-metadata.py
bundle exec fastlane upload_listing
```

`upload_listing` setzt Version **1.25** falls nötig, lädt DE/EN-Texte und überschreibt iOS-Screenshots. Kein Binary, kein Review. iOS und Mac teilen dieselbe ASC-Version (`ASC_APP_VERSION` in `fastlane/Fastfile`).

Mac-Listing (Texte + Store-Kompositionen aus `fastlane/screenshots-mac-store/`, 16:10 — typisch Retina 2560×1600). Die macOS-App muss in ASC existieren, sonst schlägt die Lane fehl:

```bash
# Rohaufnahmen + Store-Kompositionen (isolierte Debug-Demo-Szenen)
bundle exec fastlane screenshots_mac
# oder: ./scripts/capture-mac-screenshots.sh [--build]

bundle exec fastlane upload_listing_mac
```

README (DE-Mac → `docs/screenshots/`): `./scripts/copy-readme-screenshots-mac.sh`

Beide hintereinander:

```bash
bundle exec fastlane upload_listing_all
```

Vor dem iOS-Upload: PNGs unter `fastlane/screenshots/` (`bundle exec fastlane screenshots`). Deliver mappt nach Pixelgröße, nicht nach Simulator-Namen.

Review-Notes liegen unter `fastlane/metadata/review_information/notes.txt` und gehen mit den Listing-Lanes mit (Kontaktfelder in ASC nicht überschreiben). Age Rating und Privacy Nutrition Labels bleiben manuell in ASC.

Erstversion: `deliver` crasht sonst beim Laden eines noch nicht existierenden Review-Details (`No data`, [fastlane#20538](https://github.com/fastlane/fastlane/issues/20538)). Die Listing-Lanes überspringen den Attachment-Schritt, solange keine `app_review_attachment_file` gesetzt ist.

## 5. Technik-Hinweise StoreKit

- Implementierung: StoreKit **1** (wie Microsoft MAUI BillingService-Sample; StoreKit 2 wartet auf bessere .NET-Swift-Interop).
- Code: `Finanzuebersicht/Services/Billing/StoreKitBillingService.cs` (nur `IOS`/`MACCATALYST` + `AppDistribution=Store`).
- Direct-Builds registrieren `UnavailableStoreBillingService`.
- Stub-Toggles nur in **Debug**-Store-Builds (Simulator / lokale Dev). Release, TestFlight und App Store: UI ausgeblendet, Stub-Flags werden ignoriert.
- **Restore:** leeres StoreKit-Owned-Set darf den gecachten Pro-Status nicht löschen.
- **Resume:** License-Refresh vor Widget-Inbox-Drain; ohne Pro bleiben Pending-Items erhalten.

## Feature-Gates

Siehe `MONETIZATION.md`. Kurz: Direct = immer Pro, kein Sync. Store = Free-Limits + Pro-IAP; optionales Sync-Jahresabo ohne Pro-Pflicht.

## Screenshot-Automatisierung

Lokal App-Store-Screenshots (iPhone + iPad, `de-DE` + `en-US`) und ausgewählte DE-Mac-Captures fürs README erzeugen. Rohdaten unter `fastlane/screenshots/` und `fastlane/screenshots-mac/` sind **gitignored**; kuratierte README-PNGs liegen in `docs/screenshots/`.

### Voraussetzungen

- macOS mit **Xcode** (Simulator-Namen in `fastlane/Snapfile` ggf. anpassen — `xcrun simctl list devices available`)
- **Ruby + Bundler:** `bundle install` (Repo-Root, `Gemfile`)
- **.NET 10 + MAUI:** iOS-Simulator-Build der Host-App

### Ablauf

1. **MAUI-App bauen und auf Simulator installieren** (vor jedem Lauf, wenn sich die App geändert hat):

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj \
  -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64
xcrun simctl install booted \
  Finanzuebersicht/bin/Debug/net10.0-ios/iossimulator-arm64/Finanzübersicht.app
```

2. **Screenshots aufnehmen** (fastlane snapshot, 9 Rohaufnahmen × 3 Geräte × 2 Sprachen; daraus 7 Store-Motive pro Gerät/Sprache — iPhone 17 Pro Max für ASC **6,9″** / skaliert **6,5″**, iPhone 17 für 6,3″, iPad Pro 13″):

```bash
bundle exec fastlane screenshots
```

PNG-Ausgabe: `fastlane/screenshots/<locale>/<Gerät>-<shot>.png` (flaches Layout, z. B. `de-DE/iPhone 17-01-dashboard.png` … `07-settings.png`).  
Details zu UITest-Flow und Simulator-Pfaden: `Finanzuebersicht/Platforms/iOS/UITests/README.md`.

3. **README-Bilder aktualisieren** (DE-Mac Catalyst → sechs kuratierte `docs/screenshots/`-Namen):

```bash
bundle exec fastlane screenshots_mac
./scripts/copy-readme-screenshots-mac.sh
git add docs/screenshots/
```

Das Skript mappt sechs der sieben Aufnahmen. Die leere Schnellerfassung (`03-quick-expense`) wird bewusst nicht in der README gezeigt. Der iOS-Screenshot-Lauf aus Schritt 2 dient separat den App-Store-Bildern und wird nicht in `docs/screenshots/` kopiert.

4. **App Store Connect:** `bundle exec fastlane upload_listing` (siehe [Listing hochladen](#listing-hochladen-app-store-connect)). Die Store-Kompositionen erzeugt `scripts/render-store-screenshots.py` mit Pillow.

Demo-Daten: Launch-Argument `--screenshot-demo` (nur Debug; isolierter Demo-Pfad). Release/Store-Builds sind nicht betroffen.


### Store-Bilder mit Nutzenüberschriften (ab 1.25)

`fastlane screenshots` baut die aktuelle Debug-App automatisch, installiert sie auf den drei konfigurierten Simulatoren, erstellt neun Rohaufnahmen pro Sprache/Gerät und rendert sieben Store-Bilder. Die Simulatorarchitektur wird passend zum Ruby-Host gewählt (`iossimulator-x64` auf Intel, `iossimulator-arm64` auf Apple Silicon). Bei Rosetta oder abweichenden Setups kann `SCREENSHOT_SIMULATOR_RID` explizit gesetzt werden. Die Simulator-Bridges und das Widget werden für Intel und Apple Silicon gebaut; Geräte-/Store-Artefakte bleiben ARM64. Alle Store-Motive zeigen die App selbst.

Voraussetzung für den Bild-Renderer:

```bash
python3 -m venv /tmp/finanz-screenshot-venv
/tmp/finanz-screenshot-venv/bin/pip install -r scripts/requirements-screenshots.txt
export SCREENSHOT_PYTHON=/tmp/finanz-screenshot-venv/bin/python
```

Falls `python3` bereits Pillow enthält, ist kein zusätzliches venv nötig. Die lokalisierten Überschriften, Zusatztexte und Pro-Hinweise stehen in `fastlane/screenshot-storyboard.json`.

```bash
bundle exec fastlane screenshots
bundle exec fastlane screenshots_mac
# Nur neu komponieren, ohne erneute App-Aufnahme:
bundle exec fastlane render_store_screenshots
bundle exec fastlane render_store_screenshots_mac
```

Rohdaten bleiben in `fastlane/screenshots/` bzw. `fastlane/screenshots-mac/`. Die Upload-Lanes rendern vor dem Upload erneut und verwenden ausschließlich `fastlane/screenshots-store/` bzw. `fastlane/screenshots-mac-store/`. Text-only-Lanes verändern keine Bilder. Alle vier Verzeichnisse sind gitignored. README-Skripte greifen weiterhin auf Rohdaten zu.

Store-Reihenfolge: Dashboard, Konten, CSV-Vorschau (Pro), Transaktionen, wiederkehrende Zahlungen, Sparziele (mehrere mit Pro), ausgefüllte Schnellerfassung. Die Rohaufnahmen von Kategorien/Einstellungen bleiben für README und interne Prüfung erhalten, gelangen aber nicht in das Store-Set.

Die CSV-Vorschau verwendet im isolierten **Debug-/Nicht-Store-Demomodus** einen fiktiven CSV-Stream und durchläuft die echte Import-Pipeline. Es wird nichts importiert. Die Daten des normalen App-Profils bleiben unberührt. Mac-Aufnahmen warten auf eine Bereitschaftsmarkierung nach Seitennavigation/Datenladen und wählen das größte normale App-Fenster, keine Tooltips.

Lokale Prüfungen:

```bash
python3 scripts/test_render_store_screenshots.py
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj
```

Der Renderer bewahrt die Pixelmaße und das Seitenverhältnis der Rohbilder, fügt Überschriften hinzu und verändert die App-Inhalte nicht. Fehlende Motive/Sprachen oder inkonsistente Bildgrößen brechen vor dem Ersetzen vorhandener Store-Bilder ab. Vor einem Upload die gerenderten Sets visuell prüfen.

Der Renderer erstellt außerdem eine `index.html` im jeweiligen Store-Ausgabeverzeichnis als lokale Bildgalerie. Die iOS-Aufnahme öffnet Sparziele in einer eigenen Demo-Szene und prüft die Höhe der vollständig geladenen Karte, damit die iPad-Aufnahme nicht während einer unvollständigen Listenanordnung entsteht.
