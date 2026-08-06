#!/bin/bash
# Build libWidgetKitBridge.a (Swift → C ABI) for device + simulator.
# Output: Native/lib/Release-{iphoneos|iphonesimulator}/libWidgetKitBridge.a
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

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

if ! resolve_developer_dir; then
  echo "error: full Xcode not found" >&2
  exit 1
fi

SRC="$SCRIPT_DIR/WidgetKitBridge.swift"
OUT_ROOT="$SCRIPT_DIR/lib"
mkdir -p "$OUT_ROOT"

build_one() {
  local sdk="$1"
  local triple="$2"
  local dest_dir="$OUT_ROOT/Release-$sdk"
  mkdir -p "$dest_dir"
  local sdk_path
  sdk_path="$(xcrun --sdk "$sdk" --show-sdk-path)"
  echo "Building WidgetKitBridge for $sdk ($triple)..."
  xcrun -sdk "$sdk" swiftc -parse-as-library -emit-library -static \
    -target "$triple" \
    -sdk "$sdk_path" \
    -o "$dest_dir/libWidgetKitBridge.a" \
    "$SRC" \
    -framework WidgetKit
  echo "Staged $dest_dir/libWidgetKitBridge.a"
}

# Match MAUI SupportedOSPlatformVersion 15.0
build_one iphoneos arm64-apple-ios15.0
build_one iphonesimulator arm64-apple-ios15.0-simulator

echo "Done. MAUI links via NativeReference (ForceLoad) + P/Invoke __Internal."
