#!/usr/bin/env bash
# Publishes Plumb for Apple Silicon, wraps it in dist/Plumb.app with its icon and Info.plist, and zips it
# as dist/Plumb-<version>-macos-arm64.zip for a release.
# IfcConvert must be fetched first (tools/fetch-ifcconvert.sh macos-arm64). The Unity viewer is
# included when viewer-build/ exists (tools/build-viewer.sh).
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
bundle="$root/dist/Plumb.app"
publish="$(mktemp -d "${TMPDIR:-/tmp}/plumb-publish.XXXXXX")"
trap 'rm -rf "$publish"' EXIT

dotnet publish "$root/src/Plumb.App" -c Release -r osx-arm64 --self-contained -o "$publish"

rm -rf "$bundle"
mkdir -p "$bundle/Contents/MacOS" "$bundle/Contents/Resources"
cp -R "$publish"/. "$bundle/Contents/MacOS/"
cp "$root/packaging/macos/Info.plist" "$bundle/Contents/"
cp "$root/packaging/macos/Plumb.icns" "$bundle/Contents/Resources/"
plutil -lint "$bundle/Contents/Info.plist" >/dev/null

# An ad-hoc signature for the whole bundle: without one, macOS reports a downloaded app as damaged
# instead of asking whether to open an app from an unidentified developer.
codesign --force --deep --sign - "$bundle"
codesign --verify --deep --strict "$bundle"

version="$(/usr/libexec/PlistBuddy -c "Print :CFBundleShortVersionString" "$bundle/Contents/Info.plist")"
archive="$root/dist/Plumb-${version}-macos-arm64.zip"
rm -f "$archive"
ditto -c -k --keepParent "$bundle" "$archive"

echo "Plumb.app and $(basename "$archive") ready in dist/"
