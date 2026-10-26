#!/usr/bin/env bash
# Capture Mac Catalyst App Store screenshots (16:10) into fastlane/screenshots-mac/.
# Uses Debug app + --screenshot-demo, opens isolated real demo scenes, captures the app window.
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
// Ignore tooltips and transient controls; the first layer-0 window may be tiny.
let windows = info.filter { w in
  guard w[kCGWindowOwnerName as String] as? String == "Finanzübersicht",
        w[kCGWindowLayer as String] as? Int == 0,
        let bounds = w[kCGWindowBounds as String] as? [String: Any],
        let width = bounds["Width"] as? Double,
        let height = bounds["Height"] as? Double else { return false }
  return width >= 600 && height >= 400
}.sorted { lhs, rhs in
  let a = lhs[kCGWindowBounds as String] as! [String: Any]
  let b = rhs[kCGWindowBounds as String] as! [String: Any]
  return (a["Width"] as! Double) * (a["Height"] as! Double) >
         (b["Width"] as! Double) * (b["Height"] as! Double)
}
if let w = windows.first, let id = w[kCGWindowNumber as String] as? Int {
  print(id)
  exit(0)
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
  local lang="$1" scene="$2"
  local marker="${HOME}/Library/Application Support/Finanzuebersicht/screenshot-demo/capture-ready-${lang}-${scene}"
  quit_app
  rm -f "$marker"
  open "$APP" --args --screenshot-demo --screenshot-scene "$scene" -AppleLanguages "(${lang})"
  local i
  for i in $(seq 1 120); do
    if [[ -f "$marker" ]] && window_id >/dev/null 2>&1; then
      sleep 0.5
      return 0
    fi
    sleep 0.5
  done
  echo "Timed out waiting for screenshot scene ${lang}/${scene}; check crash.log" >&2
  return 1
}

capture_locale() {
  local lang="$1"
  local dir="${OUT}/${lang}"
  mkdir -p "$dir"
  # Stage a complete run before replacing previous raw screenshots.
  local staging
  staging="$(mktemp -d "${OUT}/.capture-${lang}.XXXXXX")"
  local pair scene name
  for pair in \
    "dashboard:01-dashboard" \
    "transactions:02-transactions" \
    "quick-expense:03-quick-expense" \
    "recurring:04-recurring" \
    "management:05-management" \
    "savings:06-savings" \
    "settings:07-settings" \
    "accounts:08-accounts" \
    "import:09-import"; do
    scene="${pair%%:*}"
    name="${pair#*:}"
    launch_demo "$lang" "$scene"
    capture_shot "${staging}/${name}.png"
  done
  cp "${staging}/"*.png "$dir/"
  rm -r "$staging"
  quit_app
}

mkdir -p "$OUT"
capture_locale "de-DE"
capture_locale "en-US"

echo "Done. Raw PNGs under ${OUT}/{de-DE,en-US}/"
echo "Render store images: bundle exec fastlane render_store_screenshots_mac"
echo "README: ./scripts/copy-readme-screenshots-mac.sh"
