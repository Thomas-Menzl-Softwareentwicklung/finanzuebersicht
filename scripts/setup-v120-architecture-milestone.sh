#!/usr/bin/env bash
# Assigns milestones/labels and updates existing issues for v1.20 architecture backlog.
# Requires a GitHub token with issues:write + (for milestone edit) appropriate repo permissions.
# Usage: ./scripts/setup-v120-architecture-milestone.sh
set -euo pipefail

REPO="${REPO:-tom4711/finanzuebersicht}"
MS_TITLE='v1.20 – Architektur-Fundament'

echo "==> Ensure maui label exists"
gh label create maui --repo "$REPO" \
  --description 'MAUI / multiplatform UI & platform concerns' \
  --color '5319E7' 2>/dev/null || true

echo "==> Update milestone description"
gh api "repos/$REPO/milestones/24" -X PATCH -f title="$MS_TITLE" -f description="$(cat <<'EOF'
Ein thematischer Architektur-Meilenstein vor Feature-Backlog und v2.0 (Mehrwährung, Verschlüsselung). Kein Zwischen-Release nötig — alles in develop abarbeiten.

## Reihenfolge (empfohlen)

1. **Korrektheit & Konstanten** — DataPath (#289), DataFileNames (#290), Doc-Drift (#291); #272 schließen
2. **Legacy raus** — IDataService (#268), CategoryBudget Use Case (#270)
3. **Application-Grenze** — Import (#269), Backup (#292), Repository-Queries (#273)
4. **Presentation** — Dashboard (#267), Transactions (#293), Categories (#294), IAppEvents (#271), Navigation (#295)
5. **Fehler & i18n** — Fehler-Modell (#274)
6. **MAUI-Readiness** — Listen (#296), Nested Scroll (#297), Theme (#298), Storage-UI (#299)
7. **Sync-Prep** — ExternalId/Source (#300) als Grundlage für #243/#245

## Nicht in diesem Milestone

- Transaktionen-Sheet UX → #266 (v1.19)
- CloudKit / Open Banking Features → Milestone 22
- Mehrwährung / Verschlüsselung → v2.0
EOF
)" >/dev/null

assign() {
  local num="$1"; shift
  echo "  assign #$num -> $*"
  gh issue edit "$num" --repo "$REPO" --milestone "$MS_TITLE" "$@" >/dev/null
}

echo "==> Assign new + orphan issues to $MS_TITLE"
assign 268 --add-label architecture --add-label code-quality --add-label priority:high --add-label tech-debt --add-label refactoring
assign 270 --add-label architecture --add-label code-quality --add-label priority:low --add-label refactoring
assign 289 --add-label architecture --add-label bug --add-label priority:high --add-label tech-debt
assign 290 --add-label code-quality --add-label priority:low --add-label tech-debt
assign 291 --add-label documentation --add-label priority:low --add-label chore
assign 292 --add-label architecture --add-label priority:medium --add-label refactoring
assign 293 --add-label architecture --add-label code-quality --add-label priority:medium --add-label refactoring
assign 294 --add-label architecture --add-label code-quality --add-label priority:low --add-label refactoring
assign 295 --add-label architecture --add-label priority:medium --add-label refactoring
assign 296 --add-label maui --add-label enhancement --add-label priority:medium
assign 297 --add-label maui --add-label bug --add-label priority:medium
assign 298 --add-label maui --add-label code-quality --add-label priority:low
assign 299 --add-label maui --add-label enhancement --add-label priority:medium
assign 300 --add-label architecture --add-label priority:low --add-label tech-debt

echo "==> Ensure existing v1.20 issues keep milestone"
for n in 267 269 271 273 274; do
  gh issue edit "$n" --repo "$REPO" --milestone "$MS_TITLE" >/dev/null
done

echo "==> Refine #269 to Import-only (Backup → #292)"
gh issue edit 269 --repo "$REPO" --title 'Architektur: Import in Application-Layer (Use Cases)' --body "$(cat <<'EOF'
## Kontext

Gesplittet: Backup liegt in #292. Konvention: **ViewModel → Use Case → Repository/Port**.

## Problem

CSV-Import läuft über konkreten `ImportService` in Core, direkt in `TransactionsViewModel` / `ImportPreviewViewModel`.

## Aufgabe

1. Port + Use Cases (z. B. Preview/Analyze + Commit)
2. Presentation injiziert keinen konkreten `ImportService` mehr
3. Parser/Categorization-Registration aus `MauiProgram` in Infra/Application-Extensions verschieben, soweit sinnvoll
4. Tests für Analyze + Commit

## Akzeptanzkriterien

- [ ] Kein ViewModel injiziert `ImportService` direkt
- [ ] Use Cases kapseln Orchestrierung; Infrastructure/Core für Parser/IO
- [ ] Bestehende Import-Tests grün; Use-Case-Tests ergänzt

## Nicht im Scope

- Open Banking (#245)
- Backup (#292)

## Meta

- **Milestone:** v1.20 – Architektur-Fundament
- **Siehe auch:** #292, #293, #274
EOF
)" >/dev/null

echo "==> Refine #268 body (LocalDataService dual instances)"
gh issue edit 268 --repo "$REPO" --body "$(cat <<'EOF'
## Kontext

Migration zu `I*Repository` + Use Cases ist abgeschlossen. `IDataService` ist `[Obsolete]` und wird in Produktion nicht mehr injiziert.

## Problem

Toter Legacy-Pfad:

- `MauiProgram` registriert noch `IDataService` → `DataServiceFacade`
- `LocalDataService` implementiert `IDataService`
- `LocalDataService` `new`t private `ReportingService` / `RecurringGenerationService`, obwohl DI dieselben Interfaces registriert
- Test-Mocks implementieren noch `IDataService`

## Aufgabe

1. Shared `InMemory*Repository`-Test-Doubles statt `MockDataService`-Götter
2. `IDataService`-Registrierung und Facade entfernen
3. Private Service-`new`s in `LocalDataService` entfernen; Stores idealerweise direkt als `I*Repository` registrieren
4. `IDataService`-Implementierung von `LocalDataService` streichen (Klasse ggf. nur noch Composite oder löschen)

## Akzeptanzkriterien

- [ ] Kein `IDataService` mehr in DI oder Produktionscode
- [ ] `DataServiceFacade.cs` gelöscht
- [ ] Backup-/Consistency-Tests ohne `IDataService`
- [ ] Alle Tests grün

## Meta

- **Milestone:** v1.20 – Architektur-Fundament
- **Welle:** 1
EOF
)" >/dev/null

echo "==> Refine #267 / #271 / #274 lightly via comment"
gh issue comment 267 --repo "$REPO" --body "$(cat <<'EOF'
### Umsetzungsplan-Ergänzung (v1.20)

Beim Split bitte mitdenken:

- Accounts/Saldo-Presenter: doppelten `GetAccountBalances`-Call pro Load zusammenführen
- Due-Recurring-Presenter (Book/Skip/Shift)
- Ziel: &lt; ~400 LOC oder klare Sub-VM-Delegation
- Reihenfolge: nach Legacy/Import-Wellen sinnvoll, parallel zu #273 möglich
EOF
)" >/dev/null

gh issue comment 271 --repo "$REPO" --body "$(cat <<'EOF'
### Umsetzungsplan-Ergänzung (v1.20)

- Pages vollständig auf `IAppEvents` (keine `App.*Changed`-Subscriptions)
- `CurrencyRefreshRegistry` langfristig durch denselben Kanal ersetzen
- Ergänzend (eigenes Issue #295): Tab-Routes in `Routes`, ID-basierte Detail-Navigation
EOF
)" >/dev/null

gh issue comment 274 --repo "$REPO" --body "$(cat <<'EOF'
### Umsetzungsplan-Ergänzung (v1.20)

Zusätzlich zur Result-/Exception-Konvention:

- Keine user-facing DE-Strings in Application/Infrastructure (z. B. „Unkategorisiert“, Backup-`Details`/`ErrorMessage`)
- Sentinel-IDs / Error-Codes → Lokalisierung in Presentation (`ResourceKeys`)
- Optional `IErrorPresenter` gegen Copy-Paste-`catch` in ViewModels
EOF
)" >/dev/null

echo "==> Close #272 (done via PR #285) and permission probes"
gh issue close 272 --repo "$REPO" --reason completed --comment "$(cat <<'EOF'
Erledigt durch PR #285 (`SettingsKeys`, `NavigationQueryKeys`, `BackupEntityKeys`).

Restarbeiten (Dateinamen / Theme-Frequency-Werte): #290.
EOF
)"

for n in 286 287 288; do
  gh issue close "$n" --repo "$REPO" --reason not_planned --comment 'Permission-Probe des Cloud-Agents — bitte ignorieren.' || true
done

echo "==> Done. Open milestone:"
gh issue list --repo "$REPO" --milestone "$MS_TITLE" --limit 50
