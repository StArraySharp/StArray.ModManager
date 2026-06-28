#!/bin/bash
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
echo "=== Build Release ==="
echo "Root: $ROOT"

# 1. Build native library (AAR)
echo ""
echo "--- [1/3] Android AAR ---"
cd "$ROOT/Android"
./gradlew :library:assembleRelease -q
AAR_SRC="$ROOT/Android/library/build/outputs/aar/library-release.aar"
AAR_DST="$ROOT/release/library-release.aar"
mkdir -p "$ROOT/release"
cp "$AAR_SRC" "$AAR_DST"
echo "  → $AAR_DST"

# 2. Build .NET mod manager
echo ""
echo "--- [2/3] .NET ModManager ---"
cd "$ROOT/StArray.ModManager"
dotnet build -c Release
DLL_DIR="$ROOT/StArray.ModManager/bin/Release/net10.0"
echo "  DLLs in: $DLL_DIR"
ls -1 "$DLL_DIR"/*.dll | while read f; do echo "    $(basename "$f")"; done

# 3. Package manager tar (exclude runtimes/)
echo ""
echo "--- [3/3] modmanager.tar ---"
TAR_DST="$ROOT/release/modmanager.tar"
rm -f "$TAR_DST"
cd "$DLL_DIR"
tar cf "$TAR_DST" --exclude='runtimes' *.dll
gzip -f "$TAR_DST"
echo "  → ${TAR_DST}.gz ($(du -h "${TAR_DST}.gz" | cut -f1))"

echo ""
echo "=== Done ==="
echo "  AAR:    $AAR_DST"
echo "  TAR:    $TAR_DST.gz"
