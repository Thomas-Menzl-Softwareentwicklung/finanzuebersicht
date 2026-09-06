#!/bin/bash
# Build libCloudKitSyncBridge.a (Swift CKSyncEngine → C ABI) for device, simulator and Mac Catalyst.
# Output: Native/lib/Release-{iphoneos|iphonesimulator|maccatalyst}/libCloudKitSyncBridge.a
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
  echo "error: full Xcode not found (CloudKit/CKSyncEngine needs the iOS + macOS SDKs)" >&2
  exit 1
fi

SRC="$SCRIPT_DIR/CloudKitSyncBridge.swift"
OUT_ROOT="$SCRIPT_DIR/lib"
mkdir -p "$OUT_ROOT"

build_one() {
  local sdk="$1"
  local triple="$2"
  local dest_name="$3"
  shift 3
  local extra_args=("$@")

  local dest_dir="$OUT_ROOT/$dest_name"
  mkdir -p "$dest_dir"
  local sdk_path
  sdk_path="$(xcrun --sdk "$sdk" --show-sdk-path)"
  echo "Building CloudKitSyncBridge for $dest_name ($triple)..."
  xcrun -sdk "$sdk" swiftc -parse-as-library -emit-library -static \
    -swift-version 5 \
    -O \
    -target "$triple" \
    -sdk "$sdk_path" \
    ${extra_args[@]+"${extra_args[@]}"} \
    -o "$dest_dir/libCloudKitSyncBridge.a" \
    "$SRC" \
    -framework CloudKit
  echo "Staged $dest_dir/libCloudKitSyncBridge.a"
}

# Match MAUI SupportedOSPlatformVersion 15.0 so the static lib links into the app;
# CKSyncEngine itself is behind @available(iOS 17, macOS 14, *) in the Swift source.
build_one iphoneos arm64-apple-ios15.0 Release-iphoneos
build_one iphonesimulator arm64-apple-ios15.0-simulator Release-iphonesimulator

# Mac Catalyst: iOS triples with the -macabi environment, compiled against the macOS SDK's
# iOSSupport overlay (that is where the Catalyst flavour of CloudKit lives).
# Fat .a so Release Store packages and Intel Debug (maccatalyst-x64) can both link.
MACOSX_SDK_PATH="$(xcrun --sdk macosx --show-sdk-path)"
MACABI_SWIFT_ARGS=(
  -I "$MACOSX_SDK_PATH/System/iOSSupport/usr/lib/swift"
  -F "$MACOSX_SDK_PATH/System/iOSSupport/System/Library/Frameworks"
  -L "$MACOSX_SDK_PATH/System/iOSSupport/usr/lib/swift"
)
build_one macosx arm64-apple-ios15.0-macabi Release-maccatalyst-arm64 "${MACABI_SWIFT_ARGS[@]}"
build_one macosx x86_64-apple-ios15.0-macabi Release-maccatalyst-x64 "${MACABI_SWIFT_ARGS[@]}"

FAT_DIR="$OUT_ROOT/Release-maccatalyst"
mkdir -p "$FAT_DIR"
lipo -create \
  "$OUT_ROOT/Release-maccatalyst-arm64/libCloudKitSyncBridge.a" \
  "$OUT_ROOT/Release-maccatalyst-x64/libCloudKitSyncBridge.a" \
  -output "$FAT_DIR/libCloudKitSyncBridge.a"
echo "Staged fat $FAT_DIR/libCloudKitSyncBridge.a ($(lipo -info "$FAT_DIR/libCloudKitSyncBridge.a"))"

echo "Done. MAUI links via NativeReference (ForceLoad) + P/Invoke __Internal."
