#!/usr/bin/env bash
# Downloads the IfcConvert build for this machine into tools/ifcconvert/<platform>/.
# The binaries are not committed: they are large and versioned upstream.
set -euo pipefail

cd "$(dirname "$0")"

version="0.9.0"
release_url="https://github.com/IfcOpenShell/IfcOpenShell/releases/download/ifcconvert-${version}"

detect_platform() {
    case "$(uname -s)-$(uname -m)" in
        Darwin-arm64) echo "macos-arm64" ;;
        Linux-x86_64) echo "linux-x64" ;;
        MINGW*-x86_64 | MSYS*-x86_64 | CYGWIN*-x86_64) echo "win-x64" ;;
        *) echo "Unsupported platform: $(uname -s) $(uname -m)" >&2; exit 1 ;;
    esac
}

platform="${1:-$(detect_platform)}"
case "$platform" in
    macos-arm64) asset="macosm164"; sha256="083bf3cd2fffad6cebbbacd1d530f13f7e36af6d0601e1924c9a566893439f07" ;;
    linux-x64)   asset="linux64";   sha256="084f9ceb636b1c7594e223a81049e085d076e122fc8d7214016f85381e394071" ;;
    win-x64)     asset="win64";     sha256="eab154be69904254b84647da200ed1f760d69dd1b8e5ca6cc5dbf8c839b764e3" ;;
    *) echo "Unknown platform: $platform" >&2; exit 1 ;;
esac

target="ifcconvert/${platform}"
marker="${target}/.version"
if [[ -f "$marker" && "$(cat "$marker")" == "$version" ]]; then
    echo "IfcConvert ${version} for ${platform} is already in tools/${target}"
    exit 0
fi

archive="ifcconvert-${version}-${asset}.zip"
curl -fsSL -o "$archive" "${release_url}/${archive}"

if command -v sha256sum >/dev/null; then
    actual="$(sha256sum "$archive" | cut -d' ' -f1)"
else
    actual="$(shasum -a 256 "$archive" | cut -d' ' -f1)"
fi
if [[ "$actual" != "$sha256" ]]; then
    rm -f "$archive"
    echo "Checksum mismatch for ${archive}: got ${actual}" >&2
    exit 1
fi

rm -rf "$target"
mkdir -p "$target"
if command -v unzip >/dev/null; then
    unzip -q "$archive" -d "$target"
else
    python3 -m zipfile -e "$archive" "$target" 2>/dev/null || python -m zipfile -e "$archive" "$target"
    chmod +x "${target}/IfcConvert" 2>/dev/null || true
fi
rm -f "$archive"
echo "$version" > "$marker"
echo "IfcConvert ${version} for ${platform} ready in tools/${target}"
