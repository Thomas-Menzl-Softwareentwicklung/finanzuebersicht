#!/usr/bin/env bash
# Copy German iPhone snapshot PNGs into docs/screenshots/ using README filenames.
# Prerequisite: bundle exec fastlane screenshots (see docs/APP_STORE.md).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="${ROOT}/fastlane/screenshots/de-DE"
DEST="${ROOT}/docs/screenshots"

if [[ ! -d "$SRC" ]]; then
  echo "Keine fastlane-Ausgabe unter ${SRC}." >&2
  echo "Zuerst: bundle exec fastlane screenshots (siehe docs/APP_STORE.md)" >&2
  exit 1
fi

IPHONE_DIR="$(find "$SRC" -maxdepth 1 -type d -name 'iPhone*' | sort | head -1)"
if [[ -z "$IPHONE_DIR" || ! -d "$IPHONE_DIR" ]]; then
  echo "Kein iPhone-Simulator-Ordner unter ${SRC} gefunden." >&2
  exit 1
fi

mkdir -p "$DEST"

# snapshot name → README filename (only shots with a README counterpart)
declare -a PAIRS=(
  "01-dashboard.png:dashboard-monat.png"
  "02-transactions.png:transaktionen.png"
  "04-recurring.png:dauerauftraege.png"
  "05-management.png:verwaltung-kategorien.png"
  "06-savings.png:sparziele.png"
  "07-settings.png:einstellungen.png"
)

# 03-quick-expense has no README slot yet.
# Legacy README assets without automation counterpart (left unchanged):
#   dashboard-jahr, dashboard-dauerauftrag, transaktionen-filter/swipe/detail,
#   umbuchung, import-vorschau, dauerauftrag-detail, verwaltung-konten,
#   konto-bearbeiten, sparziel-neu, einstellungen-ueber

copied=0
for pair in "${PAIRS[@]}"; do
  src_name="${pair%%:*}"
  dest_name="${pair##*:}"
  src_file="${IPHONE_DIR}/${src_name}"
  if [[ ! -f "$src_file" ]]; then
    echo "Übersprungen (fehlt): ${src_name}" >&2
    continue
  fi
  cp "$src_file" "${DEST}/${dest_name}"
  echo "  ${src_name} → ${dest_name}"
  copied=$((copied + 1))
done

if [[ "$copied" -eq 0 ]]; then
  echo "Keine PNGs kopiert — prüfe ${IPHONE_DIR}" >&2
  exit 1
fi

echo "Kopiert ${copied} README-Screenshot(s) aus ${IPHONE_DIR}"
