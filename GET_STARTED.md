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

### 引用 DLL / Reference DLLs

| DLL | 用途 | 必需 |
|---|---|---|
| `StArray.ModManager.dll` | ModManager 核心 API（`IModPlugin`, `IModSettings`, 运行时抽象, ImGui 等） | ✅ |
| `StArray.ModManager.Windows.dll` | Windows 平台原生依赖（仅部署时，不引用） | ❌ |
| `ImGui.NET.dll` | ImGui 绑定（`ImGuiNET` 命名空间） | 如需 UI |

### .csproj 模板

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="StArray.ModManager">
      <HintPath>path\to\StArray.ModManager.dll</HintPath>
    </Reference>
    <Reference Include="ImGui.NET">
      <HintPath>path\to\ImGui.NET.dll</HintPath>
    </Reference>
  </ItemGroup>

</Project>
```

### 最简单的 Mod

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

### 可设置项的 Mod

实现 `IModSettings` 添加设置页：

```csharp
public class ConfigurableMod : IModPlugin, IModSettings
{
    public string Id => "com.example.configurable";

    // ── 声明设置字段，自动显示在 ModManager 检查器 ──
    [ModSettingLabel("Show HUD")]
    [ModSettingLabelSide(ModInspector.LabelSide.Right)]  // 勾选框，标签在右侧
    public bool ShowHud = true;

    [ModSettingLabel("Scale")]
    [ModSettingRange(0.5f, 3f)]
    public float Scale = 1f;

    public void OnGui() => ModInspector.Draw(this);
}
```

### 使用 UnmanagedHook / UnmanagedStub

参考 [LevelDebugger](LevelDebugger/)，分三步：

1. **定义 Stub** — 用 `UnmanagedObject` 封装游戏类的字段/属性/方法调用
2. **声明 Hook** — 用 `[UnmanagedHook(程序集, 类, 方法)]` 标记替换方法
3. **注册设置** — 用 `[ModSettingLabel]` 特性声明可开关项

```csharp
// Stub — 封装游戏类字段/方法
public sealed class ScrControllerStub(nint ptr) : UnmanagedObject(ptr)
{
    public int CurrentSeqID => Obj.GetField<int>("currentSeqID");
    public nint PlayerOne => Obj.GetField<nint>("playerOne");
}

// Hook
public class MyMod : IModPlugin
{
    [UnmanagedHook("Assembly-CSharp.dll", "scrController", "TogglePauseGame")]
    static bool TogglePauseGame(nint thiz)
    {
        var ret = TogglePauseGameOriginal(thiz);
        Logger.Warn("MyMod", "Game paused");
        return ret;
    }
}
```
### Stub / Hook 详细文档

参见：
- [LevelDebugger 完整示例](LevelDebugger/) — 含 Stub 定义、Hook 声明、HUD 渲染、设置面板
- [RuntimeObject API](StArray.ModManager/RuntimeAbstractions/RuntimeObject.cs) — 字段读写、方法调用、数组访问
- [RuntimeArray API](StArray.ModManager/RuntimeAbstractions/RuntimeArray.cs) — 强类型数组元素读写
- [ModInspector](StArray.ModManager/Inspector/) — 自动绘制设置面板
- [UnmanagedObject](StArray.ModManager/RuntimeAbstractions/IUnmanagedObject.cs) — Stub 基类，提供 `Wrap<T>()` 安全包装

### package / 发布

Mod 部署到 `mods/{ModId}/` 目录，ModManager 自动识别：

```
GameFolder/
└── Corehold/
    └── mods/
        └── {YourModId}/
            ├── {YourModId}.dll
            └── 依赖.dll          # 如有额外依赖
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
