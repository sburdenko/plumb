#!/usr/bin/env bash
# Sample models are not committed: the source repository publishes no license.
set -euo pipefail

cd "$(dirname "$0")"

source_commit="7e6927982d7e57b29409bb631ef93d6770ef0665"
url="https://raw.githubusercontent.com/youshengCode/IfcSampleFiles/${source_commit}/Ifc2x3_Duplex_Architecture.ifc"
expected_sha256="b347a2c8aa8fff6db896a4417a9c50c22ac0ccd7c5cfc22b99b8d29336c606ed"

curl -fsSL -o Duplex.ifc.download "$url"
if command -v sha256sum >/dev/null; then
    actual_sha256="$(sha256sum Duplex.ifc.download | cut -d' ' -f1)"
else
    actual_sha256="$(shasum -a 256 Duplex.ifc.download | cut -d' ' -f1)"
fi
if [[ "$actual_sha256" != "$expected_sha256" ]]; then
    rm -f Duplex.ifc.download
    echo "Checksum mismatch for Duplex.ifc: got $actual_sha256" >&2
    exit 1
fi
mv Duplex.ifc.download Duplex.ifc
echo "samples/Duplex.ifc ready"
