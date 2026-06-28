# Getting Started / 快速开始

## 1. Inject into APK / 注入 APK

Download `library-release.aar` from [Releases](https://github.com/StArraySharp/StArray.ModManager/releases).  
从 [Releases](https://github.com/StArraySharp/StArray.ModManager/releases) 下载 `library-release.aar`。

⚠️ **Add INTERNET permission in `AndroidManifest.xml`** (required for OTA updates & mod networking)  
⚠️ **在 `AndroidManifest.xml` 添加网络权限**（OTA 更新和 Mod 联网需要）：

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

```bash
unzip library-release.aar -d aar_content
apktool d target.apk -o target_src

# Copy .so → lib/
cp aar_content/jni/arm64-v8a/*.so target_src/lib/arm64-v8a/

# Copy CoreCLR runtime DLLs → assets/
cp aar_content/assets/runtime/*.dll target_src/assets/

# Decompile classes.jar → smali
java -jar baksmali.jar d aar_content/classes.jar -o target_src/smali/starray/

# Add to UnityPlayerActivity.onCreate:
#   invoke-static {}, Lstarray/android/modmanager/ModManager;->launch()V
#
# ⚠️ launch() defaults to /sdcard/ModManager/. Paths are hardcoded in
#   ModManager.smali. Modify smali constants if your layout differs,
#   or use ModManagerUpdater (configurable paths + OTA auto-update).
#
# ⚠️ launch() 默认加载 /sdcard/ModManager/。路径硬编码在 ModManager.smali，
#   如需自定义请修改 smali 内常量，或使用 ModManagerUpdater。

# Repack & sign
apktool b target_src -o repacked.apk
uber-apk-signer --apks repacked.apk
```

## 2. Deploy Manager / 部署管理器

Place manager DLL and dependencies in `/sdcard/ModManager/manager/`.  
将管理器 DLL 及依赖放入 `/sdcard/ModManager/manager/`。

| File | From / 来源 |
|---|---|
| `StArray.ModManager.dll` | [Releases](https://github.com/StArraySharp/StArray.ModManager/releases) → `modmanager.zip` |
| `ImGui.NET.dll` | Same dir as StArray.ModManager build output |
| `OpenTK.Graphics.dll` | 同上 |

Final layout / 最终目录：

```
/sdcard/ModManager/
├── manager/
│   ├── StArray.ModManager.dll
│   ├── ImGui.NET.dll
│   ├── OpenTK.Graphics.dll
│   └── modmanager_config.json    （auto-generated / 自动生成）
└── mods/
    └── {YourModName}/
        ├── {YourModName}.dll
        └── ...
```

## 3. Write a Mod / 写一个 Mod

Reference `StArray.ModManager.dll` → implement `IModPlugin` → build DLL → drop into `mods/{ModName}/`.  
引用 `StArray.ModManager.dll` → 实现 `IModPlugin` → 编译 DLL → 放入 `mods/{ModName}/`。

```csharp
using StArray.ModManager.Runtime;
using System.Numerics;
using ImGuiNET;

public class HelloMod : IModPlugin
{
    public string Id => "com.example.hello";
    public string Name => "Hello";
    public string Version => "1.0.0";
    public string Author => "you";
    public string Description => "demo";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();

    public void OnLoad() { }
    public void OnUnload() { }

    // Draw HUD overlay on game screen / 在游戏画面上叠加 HUD
    public void OnForegroundGUI(ImDrawListPtr drawList)
    {
        drawList.AddText(new Vector2(10, 10), 0xFFFFFFFF, $"FPS: {ImGui.GetIO().Framerate:F0}");
    }
}
```

### Optional interfaces / 可选接口

```csharp
interface IModSettings { void OnGui(); }                    // Custom settings tab / 自定义设置页
interface IModSettingCustomDraw { void DrawInspector(); }   // Custom inspector / 自定义检查器
```

### Field attributes / 设置特性

```csharp
[ModSettingLabel("Health Multiplier / 血量倍率")]
[ModSettingRange(0.1f, 10f)]
public float HealthMultiplier = 1f;

[ModSettingLabelSide(ModInspector.LabelSide.Left)]
[ModSettingJson(Lines = 10)]
public string JsonConfig = "{}";
```

### Overlay drawing / Overlay 绘制

```csharp
void OnBackgroundGUI(ImDrawListPtr d) { }  // Behind ImGui windows / 窗口下方 (watermark / 水印)
void OnForegroundGUI(ImDrawListPtr d) { }  // Above ImGui windows / 窗口上方 (HUD)
```
