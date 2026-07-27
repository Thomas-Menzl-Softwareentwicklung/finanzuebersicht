# App Store Vorbereitung (iPhone / iPad)

Kurz-Checkliste für den ersten Store-/TestFlight-Weg. Legal-Seiten liegen im Repo unter [`site/`](../site/).

## Status (Basics)

| Thema | Status |
|-------|--------|
| Bundle ID `com.thomasmenzl.finanzuebersicht` | vorhanden (csproj) |
| iPhone + iPad (`UIDeviceFamily` 1+2) | vorhanden |
| Privacy Manifest (`PrivacyInfo.xcprivacy`) | vorhanden |
| Export Compliance (`ITSAppUsesNonExemptEncryption=false`) | gesetzt |
| iOS Release-Entitlements (ohne `get-task-allow`) | gesetzt |
| Support / Privacy / Impressum (`site/`) | vorbereitet (Platzhalter ersetzen) |
| App Store Connect App + Zertifikate | **manuell in Apple Portal** |
| Release-Signing / IPA / TestFlight CI | **noch offen** |
| Store-Screenshots iPhone/iPad | **noch offen** |

## 1. Apple Developer Portal

1. App ID `com.thomasmenzl.finanzuebersicht` anlegen (Capabilities erst bei Bedarf: iCloud, App Groups).
2. Zertifikate: **Apple Development** + **Apple Distribution**.
3. Profiles: Development + **App Store**.
4. In App Store Connect neue iOS-App mit derselben Bundle-ID anlegen.

## 2. Legal-URLs (GitHub Pages)

Seiten im öffentlichen Repo: `site/`.

1. Platzhalter ersetzen (`REPLACE_WITH_*`) — siehe [`site/README.md`](../site/README.md).
2. Pages aktivieren: Branch `main` (oder `develop`), Folder **`/site`**.
3. In App Store Connect:
   - **Support URL** → `https://finanzuebersicht.thomasmenzl.de/`
   - **Privacy Policy URL** → `https://finanzuebersicht.thomasmenzl.de/privacy.html`

   Fallback: `https://thomas-menzl-softwareentwicklung.github.io/finanzuebersicht/`

## 3. Lokaler Release-Build (Mac + Xcode)

Nach Anlegen von Team / Distribution Profile in `Finanzuebersicht.csproj` die auskommentierten Release-`Codesign*`-Properties setzen oder per CLI übergeben:

```bash
dotnet publish Finanzuebersicht/Finanzuebersicht.csproj \
  -f net10.0-ios \
  -c Release \
  -p:ArchiveOnBuild=true \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Distribution" \
  -p:CodesignProvision="NAME_DES_APP_STORE_PROFILES"
```

Dann Archive in Xcode Organizer / Transporter nach TestFlight laden.

## 4. Listing (später)

- Beschreibung DE/EN, Keywords, Kategorie (z. B. Finance / Lifestyle)
- Screenshots iPhone + iPad (nicht nur Mac Catalyst)
- Review-Notes: lokale JSON-Daten, kein Login, kein Account
- Splash/Icon final prüfen

## 5. Preisstruktur (Entwurf)

Siehe [`docs/MONETIZATION.md`](MONETIZATION.md):

- **Free** — Alltag lokal, Soft-Limits
- **Pro** — Einmalkauf (Power-Features)
- **Sync** — günstiges Jahresabo (CloudKit), sobald #243 bereit ist

## Lizenz

Als Rechteinhaber darfst du Store-Binaries unter der kommerziellen Spur ([`LICENSE-COMMERCIAL`](../LICENSE-COMMERCIAL)) veröffentlichen. GPL bleibt für den offenen Quellcode.

## 6. Feature-Gates (Implementierungsstand)

- `ILicenseService` + Soft-Limits in Create-Use-Cases
- Default-Build **Direct** (volle lokale Features, kein Sync)
- Store-Build: Free-Limits + Stub-Entitlements in Einstellungen (bis StoreKit)
- CloudKit-Engine: noch nicht (`IsCloudSyncImplemented = false`)

