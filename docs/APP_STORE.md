# App Store Vorbereitung (iPhone / iPad) + TestFlight

Legal-/Support-Seiten: separates Repo
[`finanzuebersicht-site`](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht-site)
→ `https://finanzuebersicht.thomasmenzl.de/`.

Monetarisierung: [`MONETIZATION.md`](MONETIZATION.md).

## Status

| Thema | Status |
|-------|--------|
| Bundle ID `com.thomasmenzl.finanzuebersicht` | vorhanden |
| iPhone + iPad | vorhanden |
| Privacy Manifest + Export Compliance | gesetzt |
| iOS Release-Entitlements (ohne `get-task-allow`) | gesetzt |
| Support / Privacy Site | eigenes Repo `finanzuebersicht-site` |
| License-Gates Free/Pro/Sync | vorhanden |
| StoreKit (Pro kaufen / Restore) | vorhanden (Store-Build, iOS/Mac Catalyst) |
| Sync-IAP Verkauf | **später** (CloudKit #243) |
| App Store Connect App + Zertifikate | **manuell** |
| TestFlight IPA Upload | **manuell auf dem Mac** |
| Store-Screenshots | **noch offen** |

## Product IDs (App Store Connect)

| Produkt | Typ | Product ID |
|---------|-----|------------|
| Finanzübersicht Pro | Non-Consumable | `com.thomasmenzl.finanzuebersicht.pro` |
| Finanzübersicht Sync | Auto-Renewable (1 Jahr) | `com.thomasmenzl.finanzuebersicht.sync.yearly` |

Sync in der UI noch nicht verkaufen (`IsCloudSyncImplemented = false`). Product trotzdem in ASC anlegen, sobald Sync kommt — oder erst bei #243.

## 1. Apple Developer + App Store Connect

1. App ID `com.thomasmenzl.finanzuebersicht` (Capabilities: In-App Purchase; iCloud erst für Sync).
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
- Stub-Toggles in Einstellungen bleiben für Dev, bis ASC-Produkte live sind.

## Feature-Gates

Siehe `MONETIZATION.md`. Kurz: Direct = immer Pro, kein Sync. Store = Free-Limits + Pro-IAP; Sync-Abo später ohne Pro-Pflicht.
