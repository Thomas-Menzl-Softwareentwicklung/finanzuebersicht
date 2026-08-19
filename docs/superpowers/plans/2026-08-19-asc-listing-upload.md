# ASC Listing Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Commit DE/EN App Store listing metadata and add fastlane lanes that upload texts (iOS + Mac) plus iPhone/iPad screenshots to App Store Connect without submitting a binary or review.

**Architecture:** Same pattern as SimpleTD: files under `fastlane/metadata/`, `upload_to_app_store` with `api_key_path` `~/.appstoreconnect/api_key.json`. iOS lane sends metadata + screenshots; Mac lane sends the same metadata with `skip_screenshots: true`.

**Tech Stack:** fastlane `deliver` / `upload_to_app_store`, Ruby Bundler, metadata `.txt` files, a small Python length-check script.

## Global Constraints

- Local Mac only (no CI upload).
- Bundle id: `de.thomasmenzl.finanzuebersicht`.
- App version in deliver: `1.20`.
- Locales: `de-DE`, `en-US`.
- Do **not** add `name.txt` (do not overwrite ASC display name).
- Do **not** mention CloudKit/Sync in listing copy.
- Do **not** submit for review, upload IPA, localize IAPs, capture Mac screenshots, or use frameit.
- API key stays at `~/.appstoreconnect/api_key.json` (never commit `.p8` or the JSON).
- Copy must match the spec verbatim: `docs/superpowers/specs/2026-08-19-asc-listing-upload-design.md`.
- Screenshot PNGs stay as snapshot produced them (gitignored under `fastlane/screenshots/`).

## File map

| File | Role |
|------|------|
| `fastlane/metadata/copyright.txt` | Copyright line |
| `fastlane/metadata/primary_category.txt` | `FINANCE` |
| `fastlane/metadata/de-DE/*.txt` | German listing fields |
| `fastlane/metadata/en-US/*.txt` | English listing fields |
| `scripts/check-asc-metadata.py` | Character-limit check |
| `fastlane/Fastfile` | `upload_listing`, `upload_listing_mac`, `upload_listing_all` |
| `docs/APP_STORE.md` | How to run the lanes |

---

### Task 1: Metadata files + length check

**Files:**
- Create: `fastlane/metadata/copyright.txt`
- Create: `fastlane/metadata/primary_category.txt`
- Create: `fastlane/metadata/de-DE/subtitle.txt`
- Create: `fastlane/metadata/de-DE/promotional_text.txt`
- Create: `fastlane/metadata/de-DE/description.txt`
- Create: `fastlane/metadata/de-DE/keywords.txt`
- Create: `fastlane/metadata/de-DE/release_notes.txt`
- Create: `fastlane/metadata/de-DE/support_url.txt`
- Create: `fastlane/metadata/de-DE/privacy_url.txt`
- Create: `fastlane/metadata/de-DE/marketing_url.txt`
- Create: `fastlane/metadata/en-US/subtitle.txt`
- Create: `fastlane/metadata/en-US/promotional_text.txt`
- Create: `fastlane/metadata/en-US/description.txt`
- Create: `fastlane/metadata/en-US/keywords.txt`
- Create: `fastlane/metadata/en-US/release_notes.txt`
- Create: `fastlane/metadata/en-US/support_url.txt`
- Create: `fastlane/metadata/en-US/privacy_url.txt`
- Create: `fastlane/metadata/en-US/marketing_url.txt`
- Create: `scripts/check-asc-metadata.py`

**Interfaces:**
- Consumes: approved copy from the spec (verbatim)
- Produces: deliver-compatible metadata tree; `python3 scripts/check-asc-metadata.py` exits 0 when limits hold

- [ ] **Step 1: Write the length-check script first**

```python
#!/usr/bin/env python3
"""Fail if App Store metadata exceeds ASC field limits or name.txt exists."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1] / "fastlane" / "metadata"
LIMITS = {
    "subtitle.txt": 30,
    "promotional_text.txt": 170,
    "keywords.txt": 100,
    "description.txt": 4000,
    "release_notes.txt": 4000,
}

def main() -> int:
    errors: list[str] = []
    name = ROOT / "name.txt"
    if name.exists():
        errors.append(f"do not ship {name} (would overwrite ASC display name)")
    for locale in ("de-DE", "en-US"):
        folder = ROOT / locale
        if not folder.is_dir():
            errors.append(f"missing {folder}")
            continue
        for filename, limit in LIMITS.items():
            path = folder / filename
            if not path.is_file():
                errors.append(f"missing {path}")
                continue
            text = path.read_text(encoding="utf-8").strip()
            if len(text) > limit:
                errors.append(f"{path}: {len(text)} chars > {limit}")
    if errors:
        print("\n".join(errors), file=sys.stderr)
        return 1
    print("ASC metadata length checks passed")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
```

Make executable: `chmod +x scripts/check-asc-metadata.py`

- [ ] **Step 2: Run the script (expect FAIL — files missing)**

Run: `python3 scripts/check-asc-metadata.py`

Expected: non-zero exit, messages like `missing .../fastlane/metadata/de-DE`

- [ ] **Step 3: Write metadata files (UTF-8, trailing newline, no BOM)**

`fastlane/metadata/copyright.txt`:

```
2026 Thomas Menzl
```

`fastlane/metadata/primary_category.txt`:

```
FINANCE
```

`fastlane/metadata/de-DE/subtitle.txt`:

```
Finanzen lokal. Klar. Privat
```

`fastlane/metadata/de-DE/promotional_text.txt`:

```
Persönliche Finanzen auf dem Gerät: Dashboard, Konten, Daueraufträge und Sparziele. Kein Abo für Kernfunktionen, keine Werbung, kein Tracking.
```

`fastlane/metadata/de-DE/keywords.txt`:

```
Finanzen,Budget,Ausgaben,Sparziele,Dauerauftrag,Konto,CSV,Privat,offline
```

`fastlane/metadata/de-DE/support_url.txt` and `fastlane/metadata/de-DE/marketing_url.txt` and the same two under `en-US/`:

```
https://finanzuebersicht.thomasmenzl.de/
```

`fastlane/metadata/de-DE/privacy_url.txt` and `fastlane/metadata/en-US/privacy_url.txt`:

```
https://finanzuebersicht.thomasmenzl.de/privacy.html
```

`fastlane/metadata/de-DE/description.txt`:

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

`fastlane/metadata/de-DE/release_notes.txt`:

```
Anlegen über Modal-Sheets für Transaktionen, Umbuchungen, Kategorien, Konten, Daueraufträge und Sparziele. Überarbeitete Transaktionsliste mit Monats-Chips und Tageskarten. Schnellerfassung in der App; auf dem iPhone zusätzlich als Widget (Pro). Beträge akzeptieren Komma und Punkt.
```

`fastlane/metadata/en-US/subtitle.txt`:

```
Local finance. Clear. Private.
```

`fastlane/metadata/en-US/promotional_text.txt`:

```
Personal finance on your device: dashboard, accounts, recurring payments, and savings goals. No subscription for core features. No ads. No tracking.
```

`fastlane/metadata/en-US/keywords.txt`:

```
finance,budget,expense,savings,recurring,account,csv,private,offline
```

`fastlane/metadata/en-US/description.txt`:

```
Finanzübersicht keeps income, expenses, and recurring payments locally on your device. Data is stored as JSON on the device — no account, no ads, no tracking.

DASHBOARD AND ACCOUNTS
Month and year overview, accounts with balances, transfers, budgets, and a 30-day cash-flow preview. Categories with icon, color, and monthly budget.

TRANSACTIONS
Create, search, filter, duplicate, and save as templates. CSV import (DKB) with preview, auto-categorization, and duplicate detection. Quick capture in the app; on iPhone optionally as a Home Screen widget (Pro).

RECURRING PAYMENTS AND SAVINGS GOALS
Recurring transactions with reminders. Savings goals with progress and forecast. Backup and restore including schema migration.

FREE AND PRO
Free covers everyday use: unlimited transactions, a few accounts, recurring payments, and savings goals. Pro is a one-time purchase for unlimited accounts, CSV import, widget presets, and other power features. Core features do not require a subscription.

iPhone, iPad, and Mac. German and English. Dark Mode. VoiceOver.
```

`fastlane/metadata/en-US/release_notes.txt`:

```
Create via modal sheets for transactions, transfers, categories, accounts, recurring payments, and savings goals. Redesigned transactions list with month chips and day cards. Quick capture in the app; on iPhone also as a widget (Pro). Amounts accept comma and period.
```

Do not create `fastlane/metadata/name.txt`.

- [ ] **Step 4: Re-run length check (expect PASS)**

Run: `python3 scripts/check-asc-metadata.py`

Expected: `ASC metadata length checks passed` and exit 0.

- [ ] **Step 5: Commit**

```bash
git add fastlane/metadata scripts/check-asc-metadata.py
git commit -m "feat(store): add DE/EN App Store listing metadata"
```

---

### Task 2: fastlane upload lanes

**Files:**
- Modify: `fastlane/Fastfile` (append helpers + three lanes; keep existing `screenshots` lane)

**Interfaces:**
- Consumes: metadata from Task 1; screenshots at `fastlane/screenshots/*/*.png`; API key `~/.appstoreconnect/api_key.json`
- Produces: `upload_listing`, `upload_listing_mac`, `upload_listing_all`

- [ ] **Step 1: Add shared constants and guards at the top of Fastfile (after existing REPO_ROOT)**

Insert after `REPO_ROOT = File.expand_path("..", __dir__)`:

```ruby
ASC_API_KEY_PATH = File.expand_path("~/.appstoreconnect/api_key.json")
SCREENSHOTS_PATH = File.join(REPO_ROOT, "fastlane/screenshots")
METADATA_PATH = File.join(REPO_ROOT, "fastlane/metadata")
ASC_APP_IDENTIFIER = "de.thomasmenzl.finanzuebersicht"
ASC_APP_VERSION = "1.20"

def require_asc_api_key!
  unless File.exist?(ASC_API_KEY_PATH)
    UI.user_error!(
      "App Store Connect API key missing at #{ASC_API_KEY_PATH}. " \
      "See docs/APP_STORE.md."
    )
  end
end

def require_screenshots!
  pngs = Dir.glob(File.join(SCREENSHOTS_PATH, "*", "*.png"))
  unless pngs.any?
    UI.user_error!(
      "No screenshots in #{SCREENSHOTS_PATH}. " \
      "Run: bundle exec fastlane screenshots"
    )
  end
end

def listing_upload_options(platform:, skip_screenshots:)
  {
    api_key_path: ASC_API_KEY_PATH,
    app_identifier: ASC_APP_IDENTIFIER,
    platform: platform,
    app_version: ASC_APP_VERSION,
    metadata_path: METADATA_PATH,
    screenshots_path: SCREENSHOTS_PATH,
    skip_binary_upload: true,
    skip_metadata: false,
    skip_screenshots: skip_screenshots,
    overwrite_screenshots: !skip_screenshots,
    force: true,
    submit_for_review: false,
    run_precheck_before_submit: false,
    precheck_include_in_app_purchases: false
  }
end
```

- [ ] **Step 2: Add lanes inside `platform :ios do` after the existing `screenshots` lane**

```ruby
  desc "Upload listing texts and iOS screenshots to App Store Connect (no binary, no review)"
  lane :upload_listing do
    require_asc_api_key!
    require_screenshots!
    upload_to_app_store(listing_upload_options(platform: "ios", skip_screenshots: false))
  end

  desc "Upload listing texts to the Mac App Store listing (no screenshots, no binary, no review)"
  lane :upload_listing_mac do
    require_asc_api_key!
    upload_to_app_store(listing_upload_options(platform: "osx", skip_screenshots: true))
  end

  desc "Upload iOS listing+screenshots then Mac listing texts"
  lane :upload_listing_all do
    upload_listing
    upload_listing_mac
  end
```

If Mac is missing in ASC, `upload_listing_mac` / `upload_listing_all` will fail at deliver — that is expected; do not add screenshot fakes.

- [ ] **Step 3: Smoke the guards without uploading**

Move the API key aside only if you must; otherwise:

```bash
test -f ~/.appstoreconnect/api_key.json && echo KEY_OK
python3 -c "from pathlib import Path; p=Path('fastlane/screenshots'); print(len(list(p.glob('*/*.png'))) if p.exists() else 0)"
```

Expected: `KEY_OK` on this Mac (same key as SimpleTD). PNG count ≥ 1 if a screenshot run already happened; 0 is OK until `fastlane screenshots` is run before the real upload.

Do **not** run `bundle exec fastlane upload_listing` in this task (network/ASC write). That is Task 4 / operator.

- [ ] **Step 4: Commit**

```bash
git add fastlane/Fastfile
git commit -m "feat(store): add fastlane lanes to upload ASC listing"
```

---

### Task 3: Document in APP_STORE.md

**Files:**
- Modify: `docs/APP_STORE.md`

**Interfaces:**
- Consumes: lane names from Task 2
- Produces: operator instructions matching SimpleTD’s `docs/app-store-connect.md` style

- [ ] **Step 1: Update the status table**

Change the row:

```
| Store-Screenshots | Automatisierung lokal (`fastlane snapshot`) — siehe [Screenshot-Automatisierung](#screenshot-automatisierung) |
```

to two rows:

```
| Store-Screenshots | Automatisierung lokal (`fastlane snapshot`) — siehe [Screenshot-Automatisierung](#screenshot-automatisierung) |
| Listing-Texte DE/EN + Screenshot-Upload | API lokal (`bundle exec fastlane upload_listing`) — siehe [Listing hochladen](#listing-hochladen-app-store-connect) |
```

- [ ] **Step 2: Replace section „4. Listing (vor öffentlichem Release)“ with a concrete upload section**

Insert (keep later sections numbered):

```markdown
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

`upload_listing` setzt Version **1.20** falls nötig, lädt DE/EN-Texte und überschreibt iOS-Screenshots. Kein Binary, kein Review.

Mac-Listing (nur Texte, keine Mac-Screenshots in dieser Welle). Die macOS-App muss in ASC existieren, sonst schlägt die Lane fehl:

```bash
bundle exec fastlane upload_listing_mac
```

Beide hintereinander:

```bash
bundle exec fastlane upload_listing_all
```

Vor dem iOS-Upload: PNGs unter `fastlane/screenshots/` (`bundle exec fastlane screenshots`). Deliver mappt nach Pixelgröße, nicht nach Simulator-Namen.

Review-Notes, Age Rating und Privacy Nutrition Labels bleiben manuell in ASC.
```

Remove the old three-bullet „Beschreibung DE/EN…“ stub so it does not contradict the new section.

- [ ] **Step 3: In Screenshot-Automatisierung, replace the last bullet about manual ASC upload**

Old:

```
4. **App Store Connect:** passende Gerätegrößen aus `fastlane/screenshots/` manuell hochladen (kein `frameit` in Wave 1).
```

New:

```
4. **App Store Connect:** `bundle exec fastlane upload_listing` (siehe [Listing hochladen](#listing-hochladen-app-store-connect)). Kein `frameit`.
```

- [ ] **Step 4: Commit**

```bash
git add docs/APP_STORE.md
git commit -m "docs: document ASC listing upload lanes"
```

---

### Task 4: Operator upload (this Mac, not CI)

**Files:** none in git unless deliver reports a screenshot size mismatch (then document in `docs/APP_STORE.md` only).

**Interfaces:**
- Consumes: Tasks 1–3, local API key, local PNGs
- Produces: ASC iOS DE/EN listing + screenshots; Mac DE/EN texts if Mac platform exists

- [ ] **Step 1: Preflight**

```bash
python3 scripts/check-asc-metadata.py
test -f ~/.appstoreconnect/api_key.json
ls fastlane/screenshots/de-DE/*.png fastlane/screenshots/en-US/*.png | wc -l
```

Expected: length check pass; key present; PNG count 28 if the last screenshot matrix is still on disk (2 locales × 2 devices × 7). If 0, run `bundle exec fastlane screenshots` first (needs Xcode + simulators; see existing screenshot docs).

- [ ] **Step 2: Upload iOS**

```bash
bundle exec fastlane upload_listing
```

Expected: deliver succeeds; ASC version 1.20 has DE+EN subtitle/description/keywords and iPhone/iPad screenshots.

If deliver rejects a PNG size, do not invent files — note the error in `docs/APP_STORE.md` and stop for a follow-up.

- [ ] **Step 3: Upload Mac texts**

```bash
bundle exec fastlane upload_listing_mac
```

Expected: success, or a clear ASC error if the Mac app is not created yet. That failure is acceptable; do not create dummy Mac screenshots.

- [ ] **Step 4: Commit only if docs needed a size-mismatch note**

Otherwise no commit. Do not commit screenshots or API keys.

---

## Spec coverage (self-review)

| Spec item | Task |
|-----------|------|
| Metadata DE/EN files, no `name.txt` | 1 |
| Verbatim copy + EN description/release notes | 1 |
| Length check | 1 |
| `upload_listing` iOS + screenshots, flags | 2 |
| `upload_listing_mac` osx skip screenshots | 2 |
| `upload_listing_all` | 2 |
| API key missing / no PNGs errors | 2 |
| APP_STORE.md | 3 |
| Operator upload, no CI | 4 |
| No IPA, review, IAP, Mac shots, frameit | constraints + Task 2/4 |
