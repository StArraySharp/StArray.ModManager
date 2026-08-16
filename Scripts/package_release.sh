#!/bin/bash
# 一键打包 release zip 包（version.json 的 platforms[].manager 对应产物）：
#   release/modmanager-windows.zip  — Windows SMM（manager/ 布局，供 Setup 热更新）
#   release/modmanager-android.zip  — Android SMM 托管程序集
# 用法: bash Scripts/package_release.sh
#   环境变量 ZIP=... 可覆盖 zip 可执行文件位置
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
REL="$ROOT/release"
mkdir -p "$REL"

ZIP="${ZIP:-zip}"

# 打包依赖的 SMM dll（Windows / Android 两个入口所需的公共与平台程序集）
WIN_DLLS=(StArray.ModManager.dll StArray.ModManager.Windows.dll StArray.ModManager.Windows.Native.dll ImGui.NET.dll)
AND_DLLS=(StArray.ModManager.dll StArray.ModManager.Android.dll ImGui.NET.dll OpenTK.Core.dll OpenTK.Graphics.dll OpenTK.Mathematics.dll)

echo "=== Package Release ==="
echo "Root: $ROOT"
echo "Zip:  $($ZIP --version 2>/dev/null | head -1 || echo "$ZIP")"

# ─────────────────────────────────────────────
# 0. 构建（Release）
# ─────────────────────────────────────────────
echo ""
echo "--- [0/3] Build (Release) ---"
(cd "$ROOT" && dotnet build StArray.ModManager.Windows/StArray.ModManager.Windows.csproj -c Release -p:BuildNativeDll=false)
(cd "$ROOT" && dotnet build StArray.ModManager.Android/StArray.ModManager.Android.csproj -c Release)

WIN_BIN="$ROOT/StArray.ModManager.Windows/bin/Release/net10.0"
AND_BIN="$ROOT/StArray.ModManager.Android/bin/Release/net10.0"

# ─────────────────────────────────────────────
# 1. Windows 包（manager/ 布局，HotUpdater 直接解压到 manager/）
# ─────────────────────────────────────────────
echo ""
echo "--- [1/3] modmanager-windows.zip ---"

STAGE_WIN="$(mktemp -d)"
trap 'rm -rf "$STAGE_WIN" "$STAGE_AND"' EXIT
for dll in "${WIN_DLLS[@]}"; do cp "$WIN_BIN/$dll" "$STAGE_WIN/"; done
[ -d "$WIN_BIN/en" ] && cp -r "$WIN_BIN/en" "$STAGE_WIN/en"

WIN_ZIP="$REL/modmanager-windows.zip"
rm -f "$WIN_ZIP"
( cd "$STAGE_WIN" && "$ZIP" -q -r -9 "$WIN_ZIP" . )
echo "  → $WIN_ZIP ($(du -h "$WIN_ZIP" | cut -f1))"
"$ZIP" -sf "$WIN_ZIP" 2>/dev/null | tail -n +2 | head -8 || unzip -l "$WIN_ZIP" | tail -n +4 | head -8

# ─────────────────────────────────────────────
# 2. Android 包（托管 dll，无 AAR）
# ─────────────────────────────────────────────
echo ""
echo "--- [2/3] modmanager-android.zip ---"

STAGE_AND="$(mktemp -d)"
for dll in "${AND_DLLS[@]}"; do cp "$AND_BIN/$dll" "$STAGE_AND/"; done
[ -d "$AND_BIN/en" ] && cp -r "$AND_BIN/en" "$STAGE_AND/en"

AND_ZIP="$REL/modmanager-android.zip"
rm -f "$AND_ZIP"
( cd "$STAGE_AND" && "$ZIP" -q -r -9 "$AND_ZIP" . )
echo "  → $AND_ZIP ($(du -h "$AND_ZIP" | cut -f1))"

# ─────────────────────────────────────────────
# 3. 输出 sha256（供 version.json 填写）
# ─────────────────────────────────────────────
echo ""
echo "=== SHA-256 ==="
(cd "$REL" && sha256sum modmanager-windows.zip modmanager-android.zip)

echo ""
echo "=== Done ==="
