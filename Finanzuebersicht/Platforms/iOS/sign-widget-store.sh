#!/bin/bash
# Embed Store provisioning profile into QuickExpenseWidget.appex and sign with Apple Distribution.
# Usage: sign-widget-store.sh /path/to/QuickExpenseWidget.appex
set -euo pipefail

APPEX="${1:?appex path required}"
TEAM_ID="${DEVELOPMENT_TEAM:-XY663DU933}"
BUNDLE_ID="de.thomasmenzl.finanzuebersicht.QuickExpenseWidget"
PROFILE_NAME="iOS Team Store Provisioning Profile: ${BUNDLE_ID}"
ENTITLEMENTS_SRC="$(cd "$(dirname "$0")" && pwd)/Entitlements.WidgetExtension.plist"
SIGN_IDENTITY="${CODESIGN_IDENTITY:-Apple Distribution: Thomas Menzl (${TEAM_ID})}"

if [[ ! -d "$APPEX" ]]; then
  echo "error: appex not found: $APPEX" >&2
  exit 1
fi

find_profile() {
  local dir
  for dir in \
    "$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles" \
    "$HOME/Library/MobileDevice/Provisioning Profiles"
  do
    [[ -d "$dir" ]] || continue
    local f tmp name
    for f in "$dir"/*.mobileprovision; do
      [[ -f "$f" ]] || continue
      tmp=$(mktemp)
      if security cms -D -i "$f" >"$tmp" 2>/dev/null; then
        name=$(plutil -extract Name raw -o - "$tmp" 2>/dev/null || true)
        if [[ "$name" == "$PROFILE_NAME" ]]; then
          rm -f "$tmp"
          echo "$f"
          return 0
        fi
      fi
      rm -f "$tmp"
    done
  done
  return 1
}

PROFILE=$(find_profile) || {
  echo "error: Store provisioning profile not found: $PROFILE_NAME" >&2
  exit 1
}

echo "Using profile: $PROFILE"
cp "$PROFILE" "$APPEX/embedded.mobileprovision"

# Keep CFBundle* in sync with the containing MAUI app (ASC 90473).
MARKETING_VERSION="${MARKETING_VERSION:-}"
CURRENT_PROJECT_VERSION="${CURRENT_PROJECT_VERSION:-}"
INFO_PLIST="$APPEX/Info.plist"
if [[ -n "$MARKETING_VERSION" && -n "$CURRENT_PROJECT_VERSION" && -f "$INFO_PLIST" ]]; then
  /usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $MARKETING_VERSION" "$INFO_PLIST"
  /usr/libexec/PlistBuddy -c "Set :CFBundleVersion $CURRENT_PROJECT_VERSION" "$INFO_PLIST"
  echo "Synced appex versions: short=$MARKETING_VERSION build=$CURRENT_PROJECT_VERSION"
fi

ENT_TMP=$(mktemp -t widget-entitlements.XXXXXX.plist)
cp "$ENTITLEMENTS_SRC" "$ENT_TMP"

# Ensure required keys are present (idempotent)
/usr/libexec/PlistBuddy -c "Delete :application-identifier" "$ENT_TMP" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Add :application-identifier string ${TEAM_ID}.${BUNDLE_ID}" "$ENT_TMP"
/usr/libexec/PlistBuddy -c "Delete :com.apple.developer.team-identifier" "$ENT_TMP" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Add :com.apple.developer.team-identifier string ${TEAM_ID}" "$ENT_TMP"

codesign --force --sign "$SIGN_IDENTITY" \
  --entitlements "$ENT_TMP" \
  --timestamp=none \
  "$APPEX"

rm -f "$ENT_TMP"
echo "Signed $APPEX with $SIGN_IDENTITY"
codesign -d --entitlements :- "$APPEX" 2>/dev/null | plutil -p - || true
