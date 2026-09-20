#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS_DIR="$ROOT_DIR/Artifacts"
ADDIN_ARTIFACTS_DIR="$ARTIFACTS_DIR/addin"
PUBLISH_DIR="$ROOT_DIR/XLS-Memory-Lib/bin/$CONFIGURATION/net10.0-windows/publish"
REQUIRED_FILES=("XLS-Memory-Lib.xll" "native/x64/e_sqlite3.dll" "native/x64/duckdb.dll")

mkdir -p "$ARTIFACTS_DIR"

dotnet restore "$ROOT_DIR/MSBuild/MemoryDb-Lib.sln"
dotnet pack "$ROOT_DIR/Memory-Db/Memory-Db.csproj" -c "$CONFIGURATION" -o "$ARTIFACTS_DIR"
dotnet build "$ROOT_DIR/XLS-Memory-Lib/XLS-Memory-Lib.csproj" -c "$CONFIGURATION"

for file in "${REQUIRED_FILES[@]}"; do
    if [[ ! -s "$PUBLISH_DIR/$file" ]]; then
        echo "Required add-in publish file is missing or empty: $PUBLISH_DIR/$file" >&2
        exit 1
    fi
done

# Preserve the packed add-in's complete layout, including native/x64.
mkdir -p "$ADDIN_ARTIFACTS_DIR"
cp -R "$PUBLISH_DIR/." "$ADDIN_ARTIFACTS_DIR/"
for file in "${REQUIRED_FILES[@]}"; do
    cmp "$PUBLISH_DIR/$file" "$ADDIN_ARTIFACTS_DIR/$file"
done

echo "NuGet package output: $ARTIFACTS_DIR"
echo "Excel-DNA add-in output (including native DLLs): $ADDIN_ARTIFACTS_DIR"
