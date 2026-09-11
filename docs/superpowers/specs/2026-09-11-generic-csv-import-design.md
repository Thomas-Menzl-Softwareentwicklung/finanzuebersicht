# Generic CSV import (column mapping) — Design

**Date:** 2026-09-11  
**Related issues:** [#360](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/360) (this feature), [#361](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/361) (CAMT, later), [#362](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/362) (Store copy after ship), [#349](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/349) (mail hint, not this slice)  
**Status:** Ready for user review  
**Scope:** In-app CSV import; Direct + Store; Pro-Gate unchanged

## Goal

Unknown bank CSVs become importable without a new release parser. The user maps columns once; the mapping is remembered for the same file pattern. DKB keeps today’s one-step import as a built-in profile, not a second code path.

## Context

- Production import is `DkbCsvParser` via `IStatementParser`. Unknown files surface `Msg_ImportNoParserMatched`.
- Preview, duplicates, auto-categorization, and commit already exist (`AnalyzeCsvImportUseCase` / `CommitCsvImportUseCase`, `ImportPreviewPage`).
- DKB exports have preamble rows, `;`, German dates and amounts, and a signed `Betrag (€)` plus `Umsatztyp`.
- Bank CSVs are poorly documented; samples arrive from users. A parser zoo does not scale.
- Not Open Banking (#245). Not CAMT.053 (#361).

## Decisions (from brainstorming)

| Topic | Choice |
|--------|--------|
| Scope | Full #360 (mapping UI, saved profiles, DKB as profile) |
| Profile match | Silent: same headers + delimiter → preview; mapping only if unknown |
| Amount | One amount column; optional type/sign column (`Eingang`/`Ausgang`, `Soll`/`Haben`, …) |
| Mapped fields | Date + amount required; title **or** purpose required; optional payee/title, purpose, type, IBAN |
| Header row | Auto-detect; user can override in mapping UI |
| Column guess | Pre-fill from header names; user corrects |
| Date/number format | Detect from sample values; shown and overridable in mapping UI |
| Bad profile escape | “Spalten neu zuordnen” on preview; no Settings profile list in v1 |
| Architecture | One pipeline; DKB is a built-in profile; JSON store in data directory |
| Parser zoo | `DkbCsvParser` / `IStatementParser` leave the CSV production path |

## Architecture

```text
File picker (Pro-gated)
        ↓
CsvTableReader  →  CsvTable (headers, rows, delimiter, encoding, header index)
        ↓
ProfileMatcher  →  built-in DKB or user profile (user wins on same fingerprint)
        ↓
     match? ──yes──→ CsvMappingApplier → TransactionDto[] → Analyze → ImportPreview
        │
        no
        ↓
ImportMappingPage (guessed mapping, formats, header override)
        ↓
save user profile → CsvMappingApplier → Analyze → ImportPreview
        ↓
optional “Spalten neu zuordnen” (same CsvTable in session)
```

**Ownership**

- **Core:** `CsvTable`, profile model, fingerprint, header/format heuristics, mapping → `TransactionDto`, sign-column rules, built-in DKB profile.
- **Application:** detect/analyze orchestration; persist profiles via repository; existing analyze/commit stay the DTO→preview→save path.
- **Infrastructure:** `csv-import-profiles.json` in `DataPath` (`DataFileNames`), included in ZIP backup/restore; not CloudKit.
- **Presentation:** coordinator branches to mapping vs preview; mapping ViewModel; extend import session with the table; preview action to remap.

`IStatementParser` is not used for CSV after this change. CAMT (#361) can add a separate XML reader that still emits `TransactionDto` into Analyze/Commit.

## Components

### 1. CsvTableReader

Input: file bytes. Output: `CsvTable` or a domain error (unreadable / not tabular).

- Encoding: UTF-8 BOM → UTF-8; valid UTF-8 → UTF-8; else Windows-1252.
- Delimiter: `;`, `,`, or tab — whichever is most consistent on candidate header lines.
- CSV grammar: quotes, escaped `""`, separators and newlines inside quotes (same behavior as today’s DKB parser).
- Header heuristic: first row with at least three non-empty cells that look like names (mostly non-numeric, or a known token such as Datum/Date/Buchung/Betrag/Amount/Verwendungszweck/IBAN/Umsatz). DKB preamble (`Girokonto`, Kontostand, blank) is skipped.
- Rows after the header are data. Empty trailing rows are ignored.

### 2. CsvImportProfile

| Field | Role |
|--------|------|
| Id | Stable id (built-in DKB is a well-known id) |
| Name | Display only (`DKB`, or a short auto name from first distinctive header / “CSV”) |
| IsBuiltIn | True only for shipped DKB |
| Delimiter | Part of fingerprint |
| Headers | Normalized header list in order; part of fingerprint |
| HeaderRowIndex | Stored so remap opens on the same header |
| Columns | Header **names** for Date, Amount, Title, Purpose, AmountSign, Iban (each nullable except Date and Amount must be set when saved) |
| DateFormat | Detected or user-selected pattern |
| DecimalStyle | Comma or point decimal |

Fingerprint: delimiter + headers trimmed, Unicode case-insensitive, order significant. Extra or missing column = different pattern.

**Match order:** user profiles first, then built-in DKB. Remapping DKB writes a **user** profile with the same fingerprint; the built-in file is never patched.

### 3. CsvMappingApplier

Table + profile → `TransactionDto` list (skip/invalid rows become analyze `Invalid`, not a thrown parse abort).

- **Date** → `Buchungsdatum` (and `Wertstellung` = same date if no separate map — v1 has no Wertstellung field).
- **Amount** parsed with the profile decimal style. Absolute value later in `CreateTransaction` as today.
- **AmountSign** if mapped: income tokens `Eingang`, `Haben`, `Credit`, `Einnahme`, `+`; expense tokens `Ausgang`, `Soll`, `Debit`, `Ausgabe`, `-` (case-insensitive, trim). Match sets the sign of `dto.Betrag`. Unknown token → keep parsed amount sign.
- **No AmountSign column:** sign of parsed amount, same as today.
- **Title column** → `Zahlungsempfaenger`. **Purpose** → `Verwendungszweck`. `CreateTransaction` / `BuildTitle` stay: title = payee, else purpose, else fallback `Buchung {date} {amount}`.
- **IBAN** → `dto.IBAN` only; still not persisted on `Transaction`.
- Unmapped DKB extras (Status, Gläubiger-ID, …) are ignored.

Built-in DKB column names (exact export headers):

- Date: `Buchungsdatum`
- Amount: `Betrag (€)`
- Title: `Zahlungsempfänger*in`
- Purpose: `Verwendungszweck`
- AmountSign: `Umsatztyp`
- Iban: `IBAN`

### 4. Use cases and session

- Coordinator reads the picked file into a session buffer (do not require a second pick for remap).
- New analyze entry: `CsvTable` + profile → existing preview result. Stream-of-parsers path goes away.
- If no profile matches: result `NeedsMapping` (not an error). Coordinator navigates to mapping.
- Mapping save: upsert user profile, then analyze, then preview.
- `IImportSessionStore` holds active `CsvTable`, applied profile (if any), and preview. `Clear` on successful leave / new import.

### 5. Persistence

- File: `csv-import-profiles.json` next to `transactions.json`.
- ZIP backup/restore includes that file (optional in old archives: missing = empty user profiles; DKB built-in remains).
- Not a CloudKit entity.

## Flow

1. Transactions → Import → Pro-Gate → file picker.
2. Reader builds `CsvTable`. Failure → alert, stop.
3. Matcher: user profile or DKB → apply → analyze → preview (no mapping page).
4. No match → mapping page (guessed columns, detected formats, header override). Continue enabled only when Date, Amount, and Title **or** Purpose are set.
5. Save profile → analyze → preview.
6. Preview action “Spalten neu zuordnen” (hidden if no table in session) → mapping with current profile filled → overwrite that user profile (or create shadow if it was built-in) → re-analyze → preview.
7. Commit unchanged (selection, duplicates, uncategorized, CloudKit notify).

Encoding, delimiter, and header index stay internal except header override on the mapping page.

## UI

- New route `ImportMappingPage`, registered like `ImportPreviewPage`. Not a Toolkit popup.
- Mac Catalyst: `SelectionField` + `SelectionPopup` for every dropdown (columns, date format, decimal style, header row). Native `Picker` is not used.
- Mapping page: short table preview (first rows); header-row control; dropdowns for Date, Amount, Title, Purpose, AmountSign, IBAN, date format, decimal style. Required fields marked. Continue disabled until valid.
- Preview: existing page + remap action.
- Strings DE+EN in Resx / `ResourceKeys`. No domain strings from Application.
- Replace import copy that says only DKB is supported or “no parser matched” as the happy-path unknown-file outcome (unknown file → mapping, not that error).

Date format choices (detect + dropdown): `dd.MM.yy`, `dd.MM.yyyy`, `yyyy-MM-dd`, `dd/MM/yyyy`, `d.M.yyyy`. Auto-detect tries these on a sample of the mapped date column; first format that parses a clear majority wins. Do not auto-pick `MM/dd/yyyy`.

Decimal style: comma (`1.234,56` / `45,20`) vs point (`1,234.56` / `45.20`), detected from the amount column sample.

Column-name guesses: normalize header (trim, case-fold, strip `*`, currency in parentheses). Match longest token first. A header matches if the normalized header equals the token, or equals the token plus a short suffix (`in`, `/in`). Do not substring-match `Umsatz` onto `Umsatztyp` or `Typ` onto `Umsatztyp`. DKB does not depend on guessing (built-in profile uses exact headers).

| Target | Header tokens (examples) |
|--------|---------------------------|
| Date | `Buchungsdatum`, `Buchungstag`, `Datum`, `Date` (prefer these over `Wertstellung`/`Valuta`) |
| Amount | `Betrag`, `Amount`, `Umsatz` (not `Kontostand`, not `Umsatztyp`) |
| Title | `Zahlungsempfänger`, `Empfänger`, `Payee`, `Auftraggeber` |
| Purpose | `Verwendungszweck`, `Buchungstext`, `Beschreibung`, `Text`, `Purpose` |
| AmountSign | `Umsatztyp`, `Soll/Haben`, `S/H`, `Debit/Credit`, `Typ` |
| IBAN | `IBAN` |

## Error handling

| Case | Behavior |
|------|----------|
| Unreadable, empty, or not tabular | Alert; no mapping page |
| Header not auto-detected | Mapping page anyway; user sets header row |
| Profile matched, all rows invalid | Preview with Invalid rows / existing empty path; do not fail closed |
| Row date/amount unparsable | `ImportPreviewRowStatus.Invalid` |
| Unknown type-column token | Fall back to amount sign; do not drop the row |
| Incomplete mapping | Continue disabled; no save |
| Profile save failed | Alert; session kept; user can retry |
| Remap without table session | Action hidden |
| Analyze/commit/duplicate/save errors | Unchanged |

## Testing

- Reader: DKB fixture (preamble, `;`, UTF-8); comma CSV; tab; Windows-1252 umlauts; quoted `;` and newlines.
- Header heuristic: DKB finds `Buchungsdatum`; explicit header index override.
- Fingerprint: DKB headers match built-in; extra column does not; trim/case ignored.
- Mapping: required fields; title from payee vs purpose; type column tokens; amount-sign fallback; DE and ISO dates; `1.234,56` vs `1234.56`.
- DKB profile: existing `test_dkb_sample.csv`, multiline, malformed — same committable DTOs as current parser tests (port golden asserts; delete production `DkbCsvParser`).
- Orchestration: known profile → analyze without mapping; unknown → `NeedsMapping`; remap upserts user profile; user profile shadows DKB.
- Coordinator/ViewModel: mapping navigation and remap with fakes, same style as existing import tests.

## Out of scope

- CAMT.053 (#361)
- Open Banking / bank API (#245)
- MT940, PDF, Excel
- Separate Soll and Haben amount columns
- Settings screen to list/delete profiles
- US `MM/dd/yyyy` auto-detect
- Store listing rewrite (#362) — next Store release after this ships
- #349 mail CTA (may sit on mapping later; not required to close #360)

## Success criteria (issue #360)

- Unknown CSV: mapping UI, then the same preview as today.
- DKB export: no manual mapping (built-in profile).
- Mapping persists for the next file with the same fingerprint.
- Direct and Store builds; Pro-Gate unchanged.
- Tests for detection, mapping, and DKB profile.
