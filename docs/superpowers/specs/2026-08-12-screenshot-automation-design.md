# Screenshot automation (fastlane snapshot) — Design

**Date:** 2026-08-12  
**Status:** Approved for planning  
**Scope:** Local Mac only; App Store sets first, README derived from same flows

## Goal

Reproducibly generate **App Store screenshots** (iPhone + iPad, German + English) and use selected frames to refresh **README** images under `docs/screenshots/`.

No CI in wave 1.

## Context

- Product: .NET 10 MAUI (`net10.0-ios` / Mac Catalyst); store listing needs iPhone + iPad shots (`docs/APP_STORE.md`).
- Current README screenshots are **v1.17** and do not match Create-UX / transactions mockup (v1.20).
- No UITest target, no fastlane, no screenshot seed mode in tree today.

## Decisions (from brainstorming)

| Topic | Choice |
|--------|--------|
| Deliverables | C — Store + README (store-first) |
| Where it runs | A — Local Mac only |
| Devices / locales | B — iPhone + iPad, `de-DE` + `en-US` |
| Tooling | fastlane `snapshot` + XCUITest |

## Architecture

```
Demo seed (Debug launch arg)
  → MAUI iOS Simulator build
  → XCUITest navigates tabs / sheets
  → snapshot("name") per screen
  → fastlane/screenshots/{locale}/{device}/…
  → curated copy → docs/screenshots/ (README names)
```

## Components

### 1. Screenshot demo mode

- **Trigger:** Debug-only launch argument, e.g. `--screenshot-demo` (or `SCREENSHOT_DEMO=1` mapped at startup).
- **Behavior:** Replace / seed JSON stores with a **fixed fixture** (accounts, transactions spanning months, recurring, category budgets, savings goal). Deterministic amounts and titles (localized where visible).
- **Safety:** Never enabled in Release / Store distribution builds. No write of demo data into the user’s real DataPath unless isolated (prefer dedicated demo path or wipe-in-memory seed before first UI).

### 2. Automation identifiers

- Add stable `AutomationId` (MAUI) on:
  - Shell tabs (Dashboard, Transactions, Recurring, Management, Savings)
  - Key FABs / sheet hosts
  - A few list anchors if needed for waiting
- Prefer ids over localized accessibility labels for navigation in tests.
- Existing a11y strings remain for VoiceOver; ids are orthogonal.

### 3. XCUITest target

- New Xcode / UI test target: `FinanzuebersichtUITests` (or equivalent MAUI-friendly setup that drives the Simulator app).
- Flows (minimum set for store + README):
  1. Dashboard (month overview)
  2. Transactions list (chips / day cards if visible)
  3. One create sheet (e.g. Schnell or neue Buchung)
  4. Recurring list
  5. Management (categories or accounts)
  6. Savings goals
  7. Settings
- Each screen: wait for idle → `snapshot("kebab-case-name")`.
- Language: follow Simulator locale from snapshot run (app already DE/EN via `LocalizationService` / system culture).

### 4. fastlane

- `fastlane/` with `Snapfile` + lane `screenshots`.
- **Devices (illustrative; pin concrete Simulator names in Snapfile):** one current iPhone (e.g. iPhone 16 Pro) + one iPad (e.g. iPad Pro 13-inch).
- **Languages:** `de-DE`, `en-US`.
- **Output:** `fastlane/screenshots/` (raw ASC material).
- **Command:** `bundle exec fastlane screenshots` (documented in `docs/` + short README note).
- Status bar / clear keychain options as recommended by snapshot docs.

### 5. README bridge

- Script or lane step: copy a **named subset** into `docs/screenshots/` with existing filenames (`dashboard-monat.png`, `transaktionen.png`, …) for DE Mac-marketing look **or** use iPhone frames with a note in README.
- Wave 1 default: map DE iPhone (or iPad) shots → README paths; update README caption to the version that produced them.
- Mac Catalyst screenshots remain optional later (not required for ASC).

## Out of scope (wave 1)

- GitHub Actions / CI
- Dark mode matrix
- Multiple iPhone sizes beyond one primary
- `frameit` marketing frames / device bezels (optional wave 2)
- Windows screenshot automation
- Widget extension screenshots

## Success criteria

1. One local command produces DE + EN screenshots for the configured iPhone and iPad simulators.
2. Navigation is fully automated (no manual Cmd+S).
3. Demo data is stable across runs (same layout density).
4. A documented path updates `docs/screenshots/` for README without hand-picking every ASC file.
5. Release/Store builds cannot accidentally ship with demo seed enabled.

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| MAUI + XCUITest flakiness | Explicit waits on AutomationIds; avoid hard sleeps where possible |
| Locale vs in-app language setting | Prefer Simulator locale; reset language settings key in demo mode |
| Simulator name churn (Xcode betas) | Pin names in Snapfile; document how to retarget |
| Demo data polluting real data | Isolated path or wipe-only when launch arg present |

## Implementation order (high level)

1. Demo seed + launch arg  
2. AutomationIds on shell + key controls  
3. XCUITest target + one happy-path snapshot  
4. fastlane Snapfile + full locale/device matrix  
5. README copy script + docs (`APP_STORE.md` / `GUIDE.md`)  
6. Optional: `frameit` later  

## Open points (resolved at plan time if needed)

- Exact Simulator device strings for the user’s Xcode install  
- Whether `fastlane/screenshots/` is gitignored (recommended: ignore raw; commit only `docs/screenshots/`)  
- Whether create-sheet snapshot is Schnell vs full transaction form  

Default proposals: **gitignore raw fastlane output**; commit curated README PNGs; include **both** list + one create sheet in the minimum set.
