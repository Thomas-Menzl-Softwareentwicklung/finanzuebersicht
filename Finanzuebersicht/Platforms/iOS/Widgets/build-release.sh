#!/bin/bash
# Build the QuickExpenseWidget extension for device and simulator.
# Run from Platforms/iOS/Widgets/ (or via MSBuild BuildWidgetExtension).
#
# Prerequisites: Xcode 16+, xcodegen (brew install xcodegen)
# Output: staged under ../../WidgetExtensions/ (project root, outside Platforms/).
#
# Version env (must match the MAUI host — NBGV):
#   MARKETING_VERSION       → CFBundleShortVersionString (e.g. 1.19)
#   CURRENT_PROJECT_VERSION → CFBundleVersion (e.g. 1.19.46)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"
REPO_ROOT="$(cd ../../../../ && pwd)"

resolve_developer_dir() {
  if [[ -n "${DEVELOPER_DIR:-}" && -x "${DEVELOPER_DIR}/usr/bin/xcodebuild" ]]; then
    return 0
  fi
  local candidate
  for candidate in \
    "/Applications/Xcode.app/Contents/Developer" \
    "/Applications/Xcode-beta.app/Contents/Developer" \
    "$HOME/Downloads/Xcode.app/Contents/Developer" \
    "$HOME/Downloads/Xcode-beta.app/Contents/Developer" \
    "$HOME/Downloads/Xcode-beta 2.app/Contents/Developer"
  do
    if [[ -x "$candidate/usr/bin/xcodebuild" ]]; then
      export DEVELOPER_DIR="$candidate"
      echo "Using DEVELOPER_DIR=$DEVELOPER_DIR"
      return 0
    fi
  done
  return 1
}

resolve_versions() {
  if [[ -n "${MARKETING_VERSION:-}" && -n "${CURRENT_PROJECT_VERSION:-}" ]]; then
    return 0
  fi
  # Fallback: version.json marketing line + leave build as "1" only if unset (MSBuild should pass both)
  if [[ -z "${MARKETING_VERSION:-}" && -f "$REPO_ROOT/version.json" ]]; then
    MARKETING_VERSION=$(python3 -c "import json; print(json.load(open('$REPO_ROOT/version.json'))['version'])")
  fi
  MARKETING_VERSION="${MARKETING_VERSION:-1.19}"
  CURRENT_PROJECT_VERSION="${CURRENT_PROJECT_VERSION:-$MARKETING_VERSION}"
}

if ! command -v xcodegen >/dev/null 2>&1; then
  echo "error: xcodegen not found. Install with: brew install xcodegen" >&2
  exit 1
fi

if ! resolve_developer_dir; then
  echo "error: full Xcode not found (Command Line Tools alone are not enough)." >&2
  echo "Install Xcode, then: sudo xcode-select -s /Applications/Xcode.app/Contents/Developer" >&2
  echo "Or set DEVELOPER_DIR to …/Xcode*.app/Contents/Developer" >&2
  exit 1
fi

if ! xcodebuild -version >/dev/null 2>&1; then
  echo "error: xcodebuild failed under DEVELOPER_DIR=$DEVELOPER_DIR" >&2
  exit 1
fi

resolve_versions
echo "Widget versions: MARKETING_VERSION=$MARKETING_VERSION CURRENT_PROJECT_VERSION=$CURRENT_PROJECT_VERSION"

if [[ ! -d QuickExpenseWidget.xcodeproj ]]; then
  echo "Generating QuickExpenseWidget.xcodeproj from project.yml..."
  xcodegen generate
fi

STAGE_ROOT="$(cd ../../.. && pwd)/WidgetExtensions"
rm -rf build
mkdir -p "$STAGE_ROOT"

for SDK in iphoneos iphonesimulator; do
  echo "Building QuickExpenseWidget for $SDK..."
  xcodebuild -quiet \
    -project QuickExpenseWidget.xcodeproj \
    -target QuickExpenseWidget \
    -configuration Release \
    -sdk "$SDK" \
    -arch arm64 \
    MARKETING_VERSION="$MARKETING_VERSION" \
    CURRENT_PROJECT_VERSION="$CURRENT_PROJECT_VERSION" \
    CODE_SIGN_IDENTITY="-" \
    CODE_SIGNING_REQUIRED=NO \
    CODE_SIGNING_ALLOWED=NO \
    BUILD_DIR=build \
    build

  DEST="$STAGE_ROOT/Release-$SDK"
  mkdir -p "$DEST"
  rm -rf "$DEST/QuickExpenseWidget.appex"
  cp -R "build/Release-$SDK/QuickExpenseWidget.appex" "$DEST/"
  echo "Staged $DEST/QuickExpenseWidget.appex"
done

echo "Done. MAUI embed uses Finanzuebersicht/WidgetExtensions via AdditionalAppExtensions (Name=QuickExpenseWidget)."
