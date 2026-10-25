#!/usr/bin/env bash
# Build MapHeim and produce a Thunderstore/Gale-ready zip in dist/.
#   ./build.sh                      # uses libs/Managed (or $VALHEIM_MANAGED)
#   VALHEIM_MANAGED=/path/to/valheim_Data/Managed ./build.sh
set -euo pipefail
cd "$(dirname "$0")"

DOTNET="${DOTNET:-$(command -v dotnet || echo "$HOME/.dotnet/dotnet")}"
VERSION="$(python3 -c 'import json;print(json.load(open("package/manifest.json"))["version_number"])')"

"$DOTNET" build src/MapHeim.csproj -c Release -nologo -v quiet

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
cp package/manifest.json package/README.md package/CHANGELOG.md package/icon.png "$STAGE/"
mkdir -p "$STAGE/plugins"
cp src/bin/Release/netstandard2.1/MapHeim.dll "$STAGE/plugins/"
if [ -d package/sounds ]; then cp -R package/sounds "$STAGE/plugins/sounds"; fi

mkdir -p dist
OUT="$PWD/dist/MapHeim-$VERSION.zip"
rm -f "$OUT"
(cd "$STAGE" && zip -qr "$OUT" .)
echo "Built $OUT"
