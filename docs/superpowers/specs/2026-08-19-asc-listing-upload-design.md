# App Store Connect listing upload (fastlane deliver) — Design

**Date:** 2026-08-19  
**Status:** Ready for user review  
**Scope:** Local Mac only; listing texts DE+EN for iOS and Mac; iOS screenshots only

## Goal

Upload **German and English App Store listing copy** plus **existing iPhone/iPad screenshots** to App Store Connect via the same API-key flow as SimpleTD (`upload_to_app_store` / deliver). No IPA, no review submission, no IAP localization, no Mac screenshots in this wave.

## Context

- Product: Finanzübersicht, bundle id `de.thomasmenzl.finanzuebersicht`, version baseline **1.20**.
- iOS screenshots already exist locally under gitignored `fastlane/screenshots/{de-DE,en-US}/` (7 shots × iPhone 17 + iPad Pro 13-inch).
- ASC listing texts are empty. Support/Privacy URLs live on `https://finanzuebersicht.thomasmenzl.de/`.
- SimpleTD already uses `~/.appstoreconnect/api_key.json` and `bundle exec fastlane upload_listing`. Same key applies here (`docs/app-store-connect.md` in SimpleTD).

## Decisions (from brainstorming)

| Topic | Choice |
|--------|--------|
| Tooling | Clone SimpleTD: fastlane `upload_to_app_store`, metadata in repo |
| Locales | `de-DE` + `en-US` |
| iOS | Texts + screenshots (`overwrite_screenshots: true`) |
| Mac | Texts only (`skip_screenshots: true`) |
| Copy | Draft from README / MONETIZATION; reviewed in chat (approved) |
| Out of scope | Binary upload, submit for review, IAP strings, Mac screenshots, frameit |

## Architecture

```
fastlane/metadata/{de-DE,en-US}/*.txt
fastlane/screenshots/{de-DE,en-US}/*.png   (already produced)
        │
        ▼
upload_listing      → platform ios  (metadata + screenshots)
upload_listing_mac  → platform osx  (metadata, skip screenshots)
        │
        ▼
App Store Connect  (API key, no review)
```

## Components

### 1. Metadata files (committed)

Mirror SimpleTD’s layout. Shared across iOS and Mac:

| File | Purpose | Limit |
|------|---------|--------|
| `de-DE/subtitle.txt` / `en-US/subtitle.txt` | Subtitle | 30 |
| `promotional_text.txt` | Promo | 170 |
| `description.txt` | Full description | 4000 |
| `keywords.txt` | Search keywords | 100 |
| `release_notes.txt` | What’s New 1.20 | 4000 |
| `support_url.txt` | Support | URL |
| `privacy_url.txt` | Privacy | URL |
| `marketing_url.txt` | Same as support URL | URL |
| `copyright.txt` | `2026 Thomas Menzl` | — |
| `primary_category.txt` | `FINANCE` | — |

Do **not** ship `name.txt`. Leave the ASC display name as set in the console (SimpleTD lesson: name collisions).

### 2. Approved copy (verbatim)

**DE subtitle:** `Finanzen lokal. Klar. Privat`  
**EN subtitle:** `Local finance. Clear. Private.`

**DE promotional_text:**  
`Persönliche Finanzen auf dem Gerät: Dashboard, Konten, Daueraufträge und Sparziele. Kein Abo für Kernfunktionen, keine Werbung, kein Tracking.`

**EN promotional_text:**  
`Personal finance on your device: dashboard, accounts, recurring payments, and savings goals. No subscription for core features. No ads. No tracking.`

**DE keywords:**  
`Finanzen,Budget,Ausgaben,Sparziele,Dauerauftrag,Konto,CSV,Privat,offline`

**EN keywords:**  
`finance,budget,expense,savings,recurring,account,csv,private,offline`

**DE description (body):**

```
Finanzübersicht verwaltet Einnahmen, Ausgaben und Daueraufträge lokal auf dem Gerät. Daten liegen als JSON bei dir — ohne Account, ohne Werbung, ohne Tracking.

DASHBOARD UND KONTEN
Monats- und Jahresübersicht, Konten mit Salden, Umbuchungen, Budgets und eine 30-Tage-Cashflow-Vorschau. Kategorien mit Icon, Farbe und Monatsbudget.

TRANSAKTIONEN
Anlegen, suchen, filtern, duplizieren und als Vorlage speichern. CSV-Import (DKB) mit Vorschau, Auto-Kategorisierung und Duplikat-Erkennung. Schnellerfassung in der App; auf dem iPhone optional als Home-Screen-Widget (Pro).

DAUERAUFTRÄGE UND SPARZIELE
Wiederkehrende Buchungen mit Erinnerung. Sparziele mit Fortschritt und Prognose. Backup und Restore inklusive Schema-Migration.

FREE UND PRO
Free reicht für den Alltag: Transaktionen unbegrenzt, wenige Konten, Daueraufträge und Sparziele. Pro ist ein Einmalkauf für unbegrenzt Konten, CSV-Import, Widget-Presets und weitere Power-Features. Die Kernfunktionen brauchen kein Abo.

iPhone, iPad und Mac. Deutsch und Englisch. Dark Mode. VoiceOver.
```

**EN description:** same structure, English wording (local JSON, no account/ads/tracking; dashboard/accounts; transactions + DKB CSV; recurring + savings; Free vs one-time Pro; iPhone/iPad/Mac; DE/EN; Dark Mode; VoiceOver). Do not mention CloudKit/Sync.

**DE release_notes (1.20):**

```
Anlegen über Modal-Sheets für Transaktionen, Umbuchungen, Kategorien, Konten, Daueraufträge und Sparziele. Überarbeitete Transaktionsliste mit Monats-Chips und Tageskarten. Schnellerfassung in der App; auf dem iPhone zusätzlich als Widget (Pro). Beträge akzeptieren Komma und Punkt.
```

**EN release_notes:** equivalent of the above.

**URLs:**

- Support: `https://finanzuebersicht.thomasmenzl.de/`
- Privacy: `https://finanzuebersicht.thomasmenzl.de/privacy.html`
- Marketing: `https://finanzuebersicht.thomasmenzl.de/`

### 3. Fastlane lanes

Same API key path as SimpleTD: `~/.appstoreconnect/api_key.json`. Fail fast if missing.

`upload_listing` (iOS):

- `app_identifier: "de.thomasmenzl.finanzuebersicht"`
- `platform: "ios"`
- `app_version: "1.20"` (create version in ASC if deliver supports it / if missing)
- `metadata_path` / `screenshots_path` under repo `fastlane/`
- `skip_binary_upload: true`
- `skip_metadata: false`
- `skip_screenshots: false`
- `overwrite_screenshots: true`
- `force: true`
- `submit_for_review: false`
- `run_precheck_before_submit: false`
- `precheck_include_in_app_purchases: false`
- Require at least one PNG under `fastlane/screenshots/` or error with “run `bundle exec fastlane screenshots` first”

`upload_listing_mac` (macOS):

- Same metadata and version
- `platform: "osx"`
- `skip_screenshots: true`
- If ASC has no Mac platform yet, the lane fails with a clear message (operator must add Mac in ASC); do not invent screenshots.

Optional convenience: `upload_listing_all` calling iOS then Mac.

### 4. Screenshot mapping

Deliver maps PNGs by **pixel size**, not Simulator marketing name. Existing snapshot files (`iPhone 17-0N-*.png`, `iPad Pro 13-inch (M5)-0N-*.png`) stay as produced. Do not rename unless deliver rejects a size; then document the required ASC slot and fix in a follow-up.

No `frameit` in this wave.

### 5. Docs

Extend `docs/APP_STORE.md`:

- API key location (shared with SimpleTD, not in git)
- `bundle exec fastlane upload_listing`
- `bundle exec fastlane upload_listing_mac`
- Prerequisite: screenshots lane already run; Mac app record in ASC for the Mac lane
- Status table: listing upload via API (local)

Do not commit `.p8` or `api_key.json`.

## Error handling

| Failure | Behaviour |
|---------|-----------|
| Missing API key | `UI.user_error!` with path + pointer to APP_STORE.md |
| No screenshot PNGs (iOS lane) | `UI.user_error!` with screenshots command |
| Mac platform missing | Deliver error; document “add macOS app in ASC” |
| Version 1.20 missing | Deliver creates/uses 1.20; if ASC only has another version, operator aligns in console |

## Testing / verification

- Character counts: subtitle ≤ 30, promo ≤ 170, keywords ≤ 100 (assert in a small script or documented check before first upload).
- Dry run: `upload_to_app_store` with `run_precheck` off; first real upload is local and operator-triggered (not CI).
- After upload: spot-check ASC iOS DE/EN listing + screenshot slots; Mac DE/EN text only.

## Out of scope

- TestFlight / IPA (Transporter remains manual)
- Age rating / privacy nutrition labels (still manual in ASC)
- IAP localization for Pro/Sync
- Mac screenshot capture
- Submitting the version for App Review
