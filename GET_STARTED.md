# Getting Started / 快速开始

## Windows / Windows 注入

通过 [Corehold](https://github.com/StArraySharp/Corehold) 的 `winmm.dll` 代理劫持自动加载 CoreCLR。

### 构建

```bash
git clone --recurse-submodules https://github.com/StArraySharp/StArray.ModManager.git
dotnet build StArray.ModManager.Windows -c Release
```

### 部署

```bash
# 1. 下载 Corehold 的 winmm.dll + Corehold/ 模板
#    https://github.com/StArraySharp/Corehold/releases

# 2. 放入游戏 .exe 同目录
GameFolder/
├── Game.exe
├── winmm.dll                          # Corehold 代理 DLL
└── Corehold/
    ├── corehold.json
    ├── managed/                        # 放入我们的 DLL
    │   ├── StArray.ModManager.dll
    │   ├── StArray.ModManager.Windows.dll
    │   ├── StArray.ModManager.Windows.Native.dll
    │   ├── cimgui.dll
    │   └── ImGui.NET.dll
    └── runtime/                        # 首次启动自动下载 .NET
```

### corehold.json

```json
{
    "enabled": true,
    "console_enabled": true,
    "runtime_path": "Corehold/runtime/",
    "coreclr_path": "Corehold/runtime/coreclr.dll",
    "target_assembly_path": "Corehold/managed/StArray.ModManager.Windows.dll",
    "entry_point_method": "StArray.ModManager.Windows.Managed.Entry",
    "entrypoint_string_args": ["Corehold/mods"]
}
```

### 运行

启动游戏 → `winmm.dll` 被加载 → Corehold 初始化 CoreCLR →
`Managed.Entry` 调用 `NativeApi.Init` → kiero 检测 D3D9/11/12 →
MinHook 钩 `Present` → C# 回调渲染 ImGui → 按 `INSERT` 打开菜单

## Android (IL2CPP Unity) / Android 注入

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

## 2. Deploy Manager (Android) / 部署管理器

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

## 3. Write a Mod / 写 Mod (跨平台)

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
