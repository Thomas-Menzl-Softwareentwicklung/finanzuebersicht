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
| iOS Release-Entitlements (ohne `get-task-allow`) | gesetzt (inkl. App Group für Quick-Expense-Widget) |
| Quick Expense Widget (Pro); In-App Schnell Free | ✅ In-App alle Targets; WidgetKit-`.appex` eingebettet — `Platforms/iOS/Widgets/README.md` |
| Support / Privacy Site | eigenes Repo `finanzuebersicht-site` |
| License-Gates Free/Pro/Sync | vorhanden |
| StoreKit (Pro kaufen / Restore) | vorhanden (Store-Build, iOS/Mac Catalyst) |
| License-Stub-UI (Dev-Toggles) | nur Debug; Release ignoriert Stub-Entitlements |
| Sync-IAP Verkauf | **später** (CloudKit #243) |
| App Store Connect App + Zertifikate | **manuell** |
| TestFlight IPA Upload | **manuell auf dem Mac** |
| Store-Screenshots | Automatisierung lokal (`fastlane snapshot`) — siehe [Screenshot-Automatisierung](#screenshot-automatisierung) |

## Product IDs (App Store Connect)

| Produkt | Typ | Product ID |
|---------|-----|------------|
| Finanzübersicht Pro | Non-Consumable | `de.thomasmenzl.finanzuebersicht.pro` |
| Finanzübersicht Sync | Auto-Renewable (1 Jahr) | `de.thomasmenzl.finanzuebersicht.sync.yearly` |

Sync in der UI noch nicht verkaufen (`IsCloudSyncImplemented = false`). Product trotzdem in ASC anlegen, sobald Sync kommt — oder erst bei #243.

## 1. Apple Developer + App Store Connect

1. App ID `de.thomasmenzl.finanzuebersicht` (Capabilities: In-App Purchase; **App Groups** `group.de.thomasmenzl.finanzuebersicht` für Quick-Expense-Widget; iCloud erst für Sync).
2. Zertifikate: **Apple Development** + **Apple Distribution**.
3. Profiles: Development + **App Store**.
4. ASC: iOS-App anlegen (gleiche Bundle-ID).
5. ASC → Monetization → In-App Purchases:
   - Pro (Non-Consumable), Preis z. B. 5,99 €
   - optional Sync (Auto-Renewable Yearly) für später
6. Sandbox-Tester unter Users and Access → Sandbox.

## 2. Legal-URLs

Site-Repo: [finanzuebersicht-site](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht-site)

In App Store Connect:

- **Support URL** → `https://finanzuebersicht.thomasmenzl.de/`
- **Privacy Policy URL** → `https://finanzuebersicht.thomasmenzl.de/privacy.html`

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

### TestFlight-Checkliste

1. IPA/Archive hochladen (Transporter oder Xcode Organizer).
2. In ASC → TestFlight: Export Compliance beantworten (`ITSAppUsesNonExemptEncryption=false` → in der Regel „Nein“).
3. Interne Tester hinzufügen (oder externe Gruppe + Beta-Review).
4. Auf Gerät mit **Sandbox Apple ID** Pro-Kauf testen (Einstellungen → Lizenz → Pro freischalten / Käufe wiederherstellen).
5. Direct/GitHub-Builds bleiben ohne StoreKit-Limits (weiterhin voll lokal).

## 4. Listing (vor öffentlichem Release)

- Beschreibung DE/EN, Keywords, Kategorie Finance
- Screenshots iPhone + iPad
- Review-Notes: lokal, kein Login; IAP Pro optional; Sync noch nicht aktiv

## 5. Technik-Hinweise StoreKit

- Implementierung: StoreKit **1** (wie Microsoft MAUI BillingService-Sample; StoreKit 2 wartet auf bessere .NET-Swift-Interop).
- Code: `Finanzuebersicht/Services/Billing/StoreKitBillingService.cs` (nur `IOS`/`MACCATALYST` + `AppDistribution=Store`).
- Direct-Builds registrieren `UnavailableStoreBillingService`.
- Stub-Toggles nur in **Debug**-Store-Builds (Simulator / lokale Dev). Release, TestFlight und App Store: UI ausgeblendet, Stub-Flags werden ignoriert.
- **Restore:** leeres StoreKit-Owned-Set darf den gecachten Pro-Status nicht löschen.
- **Resume:** License-Refresh vor Widget-Inbox-Drain; ohne Pro bleiben Pending-Items erhalten.

## Feature-Gates

Siehe `MONETIZATION.md`. Kurz: Direct = immer Pro, kein Sync. Store = Free-Limits + Pro-IAP; Sync-Abo später ohne Pro-Pflicht.

## Screenshot-Automatisierung

Lokal App-Store-Screenshots (iPhone + iPad, `de-DE` + `en-US`) und ausgewählte DE-iPhone-Frames fürs README erzeugen. Rohdaten unter `fastlane/screenshots/` sind **gitignored**; kuratierte README-PNGs liegen in `docs/screenshots/`.

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

2. **Screenshots aufnehmen** (fastlane snapshot, 7 Screens × 2 Geräte × 2 Sprachen):

```bash
bundle exec fastlane screenshots
```

PNG-Ausgabe: `fastlane/screenshots/<locale>/<Gerät>/01-dashboard.png` … `07-settings.png`.  
Details zu UITest-Flow und Simulator-Pfaden: `Finanzuebersicht/Platforms/iOS/UITests/README.md`.

3. **README-Bilder aktualisieren** (nur DE-iPhone → bestehende `docs/screenshots/`-Namen):

```bash
./scripts/copy-readme-screenshots.sh
git add docs/screenshots/
```

Das Skript mappt sechs der sieben Aufnahmen (`03-quick-expense` hat noch keinen README-Slot). Legacy-README-Assets ohne Gegenstück (z. B. `dashboard-jahr.png`, Filter/Swipe/Detail) bleiben unverändert, bis passende Flows ergänzt werden.

4. **App Store Connect:** passende Gerätegrößen aus `fastlane/screenshots/` manuell hochladen (kein `frameit` in Wave 1).

Demo-Daten: Launch-Argument `--screenshot-demo` (nur Debug; isolierter Demo-Pfad). Release/Store-Builds sind nicht betroffen.
