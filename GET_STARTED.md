# Getting Started

Inject StArray.ModManager into an existing Unity IL2CPP Android APK.

## Prerequisites

- JDK 17+
- Android SDK / NDK 27+
- apktool (https://apktool.org)
- uber-apk-signer (https://github.com/patrickfav/uber-apk-signer)
- A Unity IL2CPP arm64-v8a APK

## Build the Loader

```bash
# 1. Build native libraries
cd Android
./gradlew :library:assembleRelease
# output: library/build/outputs/aar/library-release.aar

# 2. Build managed mod
cd StArray.ModManager
dotnet build -c Release
# output: bin/Release/net10.0/ModManager.dll
```

## Inject via Smali

### Step 1: Decompile the target APK

```bash
apktool d target.apk -o target_src
```

### Step 2: Add native libraries

Copy these `.so` files to `target_src/lib/arm64-v8a/`:

From `library-release.aar` (extract with zip):
- `jni/arm64-v8a/libmodmanager.so`
- `jni/arm64-v8a/libmonodroid.so`
- `jni/arm64-v8a/libcimgui.so`

From CoreCLR runtime (obtain from `dotnet/runtime` build or prebuilt package):
- `libcoreclr.so`
- `libclrjit.so`
- `libSystem.Native.so`
- `libSystem.Globalization.Native.so`
- `libSystem.IO.Compression.Native.so`
- `libSystem.Security.Cryptography.Native.Android.so`

### Step 3: Add .NET runtime and mod files

Push to device:

```
/sdcard/ModManager/manager/
  StArray.ModManager.dll   (built managed mod)
  *.dll                     (dependencies: ImGui.NET, OpenTK, etc.)

/sdcard/ModManager/runtime/
  *.dll                     (.NET runtime assemblies: System.*.dll, etc.)
```

> **Note:** The `Runtime/` directory is **not** included in this repo.
> Download the .NET runtime assemblies from the [runtime-references release](https://github.com/StArraySharp/StArray.ModManager/releases/tag/0).
> The native `.so` files (`libcoreclr.so`, `libclrjit.so`, etc.) must be obtained
> from a matching CoreCLR Android build.

### Step 4: Inject smali startup code

Find the Unity player activity smali file (usually `com/unity3d/player/UnityPlayerActivity.smali`
or a subclass). In its `onCreate` method, insert after `invoke-super`:

```smali
invoke-static {}, Lstarray/android/modmanager/ModManager;->launch()V
```

Also add these smali classes to `target_src/smali/`. If you compiled from
source, extract them from the AAR. Otherwise create them manually:

`smali/starray/android/modmanager/ModManager.smali`:

```smali
.class public Lstarray/android/modmanager/ModManager;
.super Ljava/lang/Object;
.source "ModManager.java"

.field private static final TAG:Ljava/lang/String; = "StArray.ModManager"

.method public constructor <init>()V
    .registers 1
    invoke-direct {p0}, Ljava/lang/Object;-><init>()V
    return-void
.end method

.method public static launch()V
    .registers 7

    const-string v0, "/sdcard/ModManager/runtime"
    filled-new-array {v0, v0}, [Ljava/lang/String;
    move-result-object v1
    filled-new-array {v0}, [Ljava/lang/String;
    move-result-object v2

    new-instance v3, Lstarray/android/modmanager/ModManager$1;
    invoke-direct {v3, v0, v1, v2}, Lstarray/android/modmanager/ModManager$1;-><init>(Ljava/lang/String;[Ljava/lang/String;[Ljava/lang/String;)V

    new-instance v4, Ljava/lang/Thread;
    const-string v5, "ModManager-Main"
    invoke-direct {v4, v3, v5}, Ljava/lang/Thread;-><init>(Ljava/lang/Runnable;Ljava/lang/String;)V
    invoke-virtual {v4}, Ljava/lang/Thread;->start()V
    return-void
.end method

.method public dotnetRoot(Ljava/lang/String;)Lstarray/android/modmanager/ModManager;
    .registers 2
    invoke-static {p1}, Lnet/dot/MonoRunner;->dotnetRoot(Ljava/lang/String;)Lnet/dot/MonoRunner;
    return-object p0
.end method

.method public addAssemblyDir(Ljava/lang/String;)Lstarray/android/modmanager/ModManager;
    .registers 2
    invoke-static {p1}, Lnet/dot/MonoRunner;->addAssemblyDir(Ljava/lang/String;)Lnet/dot/MonoRunner;
    return-object p0
.end method

.method public addNativeDir(Ljava/lang/String;)Lstarray/android/modmanager/ModManager;
    .registers 2
    invoke-static {p1}, Lnet/dot/MonoRunner;->addNativeDir(Ljava/lang/String;)Lnet/dot/MonoRunner;
    return-object p0
.end method

.method public start(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)I
    .registers 5
    invoke-static {p1, p2, p3}, Lnet/dot/MonoRunner;->run(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)I
    move-result v0
    return v0
.end method
```

`smali/starray/android/modmanager/ModManager$1.smali`:

```smali
.class Lstarray/android/modmanager/ModManager$1;
.super Ljava/lang/Object;
.implements Ljava/lang/Runnable;
.source "ModManager.java"

.field final synthetic val$assemblyDirs:[Ljava/lang/String;
.field final synthetic val$nativeDirs:[Ljava/lang/String;
.field final synthetic val$runtimeRoot:Ljava/lang/String;

.method constructor <init>(Ljava/lang/String;[Ljava/lang/String;[Ljava/lang/String;)V
    .registers 4
    invoke-direct {p0}, Ljava/lang/Object;-><init>()V
    iput-object p1, p0, Lstarray/android/modmanager/ModManager$1;->val$runtimeRoot:Ljava/lang/String;
    iput-object p2, p0, Lstarray/android/modmanager/ModManager$1;->val$assemblyDirs:[Ljava/lang/String;
    iput-object p3, p0, Lstarray/android/modmanager/ModManager$1;->val$nativeDirs:[Ljava/lang/String;
    return-void
.end method

.method public run()V
    .registers 7
    :try_start
    new-instance v0, Lstarray/android/modmanager/ModManager;
    invoke-direct {v0}, Lstarray/android/modmanager/ModManager;-><init>()V
    iget-object v1, p0, Lstarray/android/modmanager/ModManager$1;->val$runtimeRoot:Ljava/lang/String;
    invoke-virtual {v0, v1}, Lstarray/android/modmanager/ModManager;->dotnetRoot(Ljava/lang/String;)Lstarray/android/modmanager/ModManager;

    iget-object v1, p0, Lstarray/android/modmanager/ModManager$1;->val$assemblyDirs:[Ljava/lang/String;
    array-length v2, v1
    const/4 v3, 0x0
    :loop_asm
    if-lt v3, v2, :loop_nat
    aget-object v4, v1, v3
    invoke-virtual {v0, v4}, Lstarray/android/modmanager/ModManager;->addAssemblyDir(Ljava/lang/String;)Lstarray/android/modmanager/ModManager;
    add-int/lit8 v3, v3, 0x1
    goto :loop_asm

    :loop_nat
    iget-object v1, p0, Lstarray/android/modmanager/ModManager$1;->val$nativeDirs:[Ljava/lang/String;
    array-length v2, v1
    const/4 v3, 0x0
    :loop_n
    if-lt v3, v2, :call
    aget-object v4, v1, v3
    invoke-virtual {v0, v4}, Lstarray/android/modmanager/ModManager;->addNativeDir(Ljava/lang/String;)Lstarray/android/modmanager/ModManager;
    add-int/lit8 v3, v3, 0x1
    goto :loop_n

    :call
    const-string v1, "ModManager.dll"
    const-string v2, "StArray.ModManager.Mono"
    const-string v3, "Entry"
    invoke-virtual {v0, v1, v2, v3}, Lstarray/android/modmanager/ModManager;->start(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)I
    :try_end
    .catch Ljava/lang/Exception; {:try_start .. :try_end} :catch_ex
    return-void

    :catch_ex
    move-exception v0
    const-string v1, "StArray.ModManager"
    const-string v2, "launch failed"
    invoke-static {v1, v2, v0}, Landroid/util/Log;->e(Ljava/lang/String;Ljava/lang/String;Ljava/lang/Throwable;)I
    return-void
.end method
```

Also add `MonoRunner` and `ModManagerUtils` (with their inner classes)
from the AAR.

### Step 5: Repackage and sign

```bash
apktool b target_src -o target_patched.apk
uber-apk-signer --apks target_patched.apk
```

### Step 6: Install and run

```bash
adb install target_patched.apk
adb shell mkdir -p /sdcard/ModManager/runtime
adb push Runtime/runtime/*.dll /sdcard/ModManager/runtime/
adb push StArray.ModManager/bin/Release/net10.0/ModManager.dll /sdcard/ModManager/runtime/
adb logcat -s StArray ImGuiRender JNIHelper
```

## Verify

The ImGui overlay window should appear after the Unity splash screen. Logcat
should show:

```
StArray.MonoRunner: CoreCLR initialized
ImGuiRender: ImGui initialized with official OpenGL3 + Android input backends
```
