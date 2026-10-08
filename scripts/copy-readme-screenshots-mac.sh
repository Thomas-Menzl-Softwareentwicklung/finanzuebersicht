#!/usr/bin/env bash
# Copy German Mac demo screenshots into docs/screenshots/ (README filenames).
# Prerequisite: ./scripts/capture-mac-screenshots.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="${ROOT}/fastlane/screenshots-mac/de-DE"
DEST="${ROOT}/docs/screenshots"

if [[ ! -d "$SRC" ]]; then
  echo "Keine Mac-Screenshots unter ${SRC}." >&2
  echo "Zuerst: ./scripts/capture-mac-screenshots.sh [--build]" >&2
  exit 1
fi

mkdir -p "$DEST"

declare -a PAIRS=(
  "01-dashboard.png:dashboard-monat.png"
  "02-transactions.png:transaktionen.png"
  "04-recurring.png:dauerauftraege.png"
  "05-management.png:verwaltung-kategorien.png"
  "06-savings.png:sparziele.png"
  "07-settings.png:einstellungen.png"
)

copied=0
for pair in "${PAIRS[@]}"; do
  src_name="${pair%%:*}"
  dest_name="${pair##*:}"
  src_file="${SRC}/${src_name}"
  if [[ ! -f "$src_file" ]]; then
    echo "Übersprungen (fehlt): ${src_name}" >&2
    continue
  fi
  cp "$src_file" "${DEST}/${dest_name}"
  echo "  ${src_name} → ${dest_name}"
  copied=$((copied + 1))
done

if [[ "$copied" -eq 0 ]]; then
  echo "Keine PNGs kopiert." >&2
  exit 1
fi

echo "Kopiert ${copied} README-Screenshot(s) aus ${SRC}"
