#!/usr/bin/env bash
# Capture Mac Catalyst App Store screenshots (16:10) into fastlane/screenshots-mac/.
# Uses Debug app + --screenshot-demo, navigates via ⌘1–⌘5 / ⌘,, captures the app window.
#
# Prerequisite (or pass --build):
#   dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst -c Debug
#
# Output (gitignored):
#   fastlane/screenshots-mac/<locale>/<shot>.png
# ASC accepts 1280×800 or Retina 2560×1600 (this script keeps the native capture size).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP="${HOME}/Applications/Finanzübersicht.app"
OUT="${ROOT}/fastlane/screenshots-mac"
BUILD=0

for arg in "$@"; do
  case "$arg" in
    --build) BUILD=1 ;;
    -h|--help)
      echo "Usage: $0 [--build]"
      exit 0
      ;;
  esac
done

if [[ "$BUILD" -eq 1 ]] || [[ ! -d "$APP" ]]; then
  echo "Building Mac Catalyst Debug → ~/Applications…"
  (cd "$ROOT" && dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst -c Debug --nologo)
fi

if [[ ! -d "$APP" ]]; then
  echo "App not found at ${APP}" >&2
  exit 1
fi

quit_app() {
  osascript -e 'tell application "Finanzübersicht" to quit' 2>/dev/null || true
  sleep 1
}

# CGWindowID of the front Finanzübersicht window (for screencapture -l).
window_id() {
  /usr/bin/swift -e '
import Cocoa
let opts: CGWindowListOption = [.optionOnScreenOnly, .excludeDesktopElements]
guard let info = CGWindowListCopyWindowInfo(opts, kCGNullWindowID) as? [[String: Any]] else { exit(1) }
for w in info {
  let owner = w[kCGWindowOwnerName as String] as? String ?? ""
  let layer = w[kCGWindowLayer as String] as? Int ?? -1
  if owner == "Finanzübersicht", layer == 0, let id = w[kCGWindowNumber as String] as? Int {
    print(id)
    exit(0)
  }
}
exit(2)
'
}

activate_app() {
  osascript <<'EOF'
tell application "Finanzübersicht" to activate
tell application "System Events"
  tell process "Finanzübersicht"
    set frontmost to true
  end tell
end tell
EOF
}

key_cmd() {
  local key="$1"
  osascript -e "tell application \"System Events\" to keystroke \"${key}\" using command down"
}

capture_shot() {
  local dest="$1"
  activate_app
  sleep 0.4
  local wid
  wid="$(window_id)" || {
    echo "Could not resolve CGWindowID for Finanzübersicht" >&2
    return 1
  }
  mkdir -p "$(dirname "$dest")"
  # -x silent, -l window id, -o no shadow
  screencapture -x -o -l"$wid" "$dest"
  # ASC Mac sizes: 1280×800, 1440×900, 2560×1600, 2880×1800 — normalize Retina 16:10.
  sips -z 1600 2560 "$dest" >/dev/null
  echo "  → $(basename "$dest") ($(sips -g pixelWidth -g pixelHeight "$dest" 2>/dev/null | awk '/pixel/{printf $2" "}'))"
}

launch_demo() {
  local lang="$1"
  quit_app
  # AppleLanguages + screenshot demo; window size locked in App.xaml.cs
  open "$APP" --args --screenshot-demo -AppleLanguages "(${lang})"
  # Wait until the window exists and seed has settled
  local i
  for i in $(seq 1 40); do
    if window_id >/dev/null 2>&1; then
      sleep 3
      return 0
    fi
    sleep 0.5
  done
  echo "Timed out waiting for Finanzübersicht window (${lang})" >&2
  return 1
}

capture_locale() {
  local lang="$1"
  local dir="${OUT}/${lang}"
  rm -rf "$dir"
  mkdir -p "$dir"

  echo "=== ${lang} ==="
  launch_demo "$lang"
  activate_app
  sleep 1

  # ⌘1 Dashboard
  key_cmd "1"
  sleep 1.5
  capture_shot "${dir}/01-dashboard.png"

  # ⌘2 Transactions
  key_cmd "2"
  sleep 1.5
  capture_shot "${dir}/02-transactions.png"

  # ⌘3 Recurring
  key_cmd "3"
  sleep 1.5
  capture_shot "${dir}/04-recurring.png"

  # ⌘4 Management
  key_cmd "4"
  sleep 1.5
  capture_shot "${dir}/05-management.png"

  # ⌘5 Savings
  key_cmd "5"
  sleep 1.5
  capture_shot "${dir}/06-savings.png"

  # ⌘, Settings
  key_cmd ","
  sleep 1.5
  capture_shot "${dir}/07-settings.png"

  # Quick expense: back to dashboard then Aktionen›Schnell via ⌘N (Dashboard registers N)
  key_cmd "1"
  sleep 1.2
  key_cmd "n"
  sleep 1.5
  capture_shot "${dir}/03-quick-expense.png"
  # Dismiss sheet (Esc)
  osascript -e 'tell application "System Events" to key code 53'
  sleep 0.5

  quit_app
}

mkdir -p "$OUT"
capture_locale "de-DE"
capture_locale "en-US"

echo
echo "Done. PNGs under ${OUT}/{de-DE,en-US}/"
echo "Upload: bundle exec fastlane upload_listing_mac"
echo "README: ./scripts/copy-readme-screenshots-mac.sh"
