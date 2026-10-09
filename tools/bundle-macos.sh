#!/usr/bin/env bash
# Publishes Plumb for Apple Silicon and wraps it in dist/Plumb.app with its icon and Info.plist.
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

echo "Plumb.app ready in dist/"
