#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS_DIR="$ROOT_DIR/Artifacts"
ADDIN_ARTIFACTS_DIR="$ARTIFACTS_DIR/addin"

mkdir -p "$ARTIFACTS_DIR" "$ADDIN_ARTIFACTS_DIR"

dotnet restore "$ROOT_DIR/MSBuild/MemoryDb-Lib.sln"
dotnet pack "$ROOT_DIR/Memory-Db/Memory-Db.csproj" -c "$CONFIGURATION" -o "$ARTIFACTS_DIR"
dotnet build "$ROOT_DIR/XLS-Memory-Lib/XLS-Memory-Lib.csproj" -c "$CONFIGURATION"

find "$ROOT_DIR/XLS-Memory-Lib/bin/$CONFIGURATION" -type f \( -name '*.xll' -o -name '*.dna' \) -exec cp -f {} "$ADDIN_ARTIFACTS_DIR" \;

echo "NuGet package output: $ARTIFACTS_DIR"
echo "Excel-DNA add-in output: $ADDIN_ARTIFACTS_DIR"
