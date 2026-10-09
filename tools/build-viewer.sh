#!/usr/bin/env bash
# Builds the Unity viewer into viewer-build/ with the Unity version the project was saved with.
# Set UNITY_EDITOR to the Unity executable to use a different install.
set -euo pipefail

cd "$(dirname "$0")/.."

version="$(sed -n 's/^m_EditorVersion: //p' viewer/ProjectSettings/ProjectVersion.txt)"
case "$(uname -s)" in
    Darwin) default_editor="/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity"; method="BuildMacOS" ;;
    MINGW* | MSYS* | CYGWIN*) default_editor="/c/Program Files/Unity/Hub/Editor/${version}/Editor/Unity.exe"; method="BuildWindows" ;;
    *) echo "Building the viewer is supported on macOS and Windows." >&2; exit 1 ;;
esac
editor="${UNITY_EDITOR:-$default_editor}"

if [[ ! -x "$editor" ]]; then
    echo "Unity ${version} not found at ${editor}. Install it with Unity Hub or set UNITY_EDITOR." >&2
    exit 1
fi

log="$(mktemp -t plumb-viewer-build)"
if ! "$editor" -batchmode -quit -projectPath "$PWD/viewer" \
        -executeMethod "Plumb.Viewer.Editor.BuildViewer.${method}" -logFile "$log"; then
    grep -E "error|Exception" "$log" | tail -20 >&2
    echo "Viewer build failed; full log: ${log}" >&2
    exit 1
fi
rm -f "$log"
echo "Viewer ready in viewer-build/"
