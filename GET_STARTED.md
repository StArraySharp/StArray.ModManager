# Getting Started

## Build

```bash
cd Android
./gradlew :library:assembleRelease
cd ../StArray.ModManager
dotnet build -c Release
```

Output:
- `Android/library/build/outputs/aar/library-release.aar`
- `StArray.ModManager/bin/Release/net10.0/StArray.ModManager.dll`

## Deploy to Device

```
/sdcard/ModManager/manager/
  StArray.ModManager.dll        built managed mod
  *.dll                          deps (ImGui.NET, OpenTK, etc.)

/sdcard/ModManager/runtime/
  *.dll                          .NET runtime assemblies
```

Runtime DLLs: [runtime-references release](https://github.com/StArraySharp/StArray.ModManager/releases/tag/0)

## Inject into APK

```bash
apktool d target.apk -o target_src
```

Copy `.so` files to `target_src/lib/arm64-v8a/`:
- From AAR: `libmodmanager.so`, `libmonodroid.so`, `libcimgui.so`
- CoreCLR: `libcoreclr.so`, `libclrjit.so`, `libSystem.Native.so`, etc.

Add to `UnityPlayerActivity.smali` onCreate:

```smali
invoke-static {}, Lstarray/android/modmanager/ModManager;->launch()V
```

Add `smali/starray/android/modmanager/ModManager.smali` and `ModManager$1.smali` from the AAR.

```bash
apktool b target_src -o repacked.apk
uber-apk-signer --apks repacked.apk
```
