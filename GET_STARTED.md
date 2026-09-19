# Getting Started / 快速开始

## Windows / Windows 注入

通过 [Corehold](https://github.com/StArraySharp/Corehold) 的 `winmm.dll` 代理劫持自动加载 CoreCLR。

### 构建

前置条件：

- .NET 10 SDK
- Visual Studio：**使用 C++ 的桌面开发** 工作负载（MSVC + Windows SDK）
- **适用于 Windows 的 C++ CMake 工具**（CMake + Ninja，需在 PATH）
- 开启 Windows **开发者模式**，克隆后还原 cimgui 符号链接（详见
  [README · Build](README.md#build)），否则 CMake 找不到 cimgui 源码：

```bash
git config core.symlinks true
rm StArray.ModManager.Windows/Native/cimgui Android/library/src/main/cpp/libs/cimgui
git checkout -- StArray.ModManager.Windows/Native/cimgui Android/library/src/main/cpp/libs/cimgui
```

然后：

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
`Managed.Entry` 调用 MinHook 初始化 → kiero 检测 D3D9/11/12 →
MinHook 钩 `Present` → C# 回调渲染 ImGui → 按 `INSERT` 打开菜单

## Android (IL2CPP Unity) / Android 注入

从 [Releases](https://github.com/StArraySharp/StArray.ModManager/releases) 下载 `library-release.aar`。

⚠️ **在 `AndroidManifest.xml` 添加网络权限**（OTA 更新和 Mod 联网需要）：

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

AAR 内直接包含 CoreCLR 运行时 `.so`（arm64-v8a），无需额外下载。

```bash
unzip library-release.aar -d aar_content
apktool d target.apk -o target_src

# 复制 .so → lib/（含 CoreCLR 运行时）
cp aar_content/jni/arm64-v8a/*.so target_src/lib/arm64-v8a/

# 反编译 classes.jar → smali
java -jar baksmali.jar d aar_content/classes.jar -o target_src/smali/starray/

# 在 UnityPlayerActivity.onCreate 添加：
#   invoke-static {}, Lstarray/android/modmanager/ModManager;->launch()V
#
# launch() 默认加载 /sdcard/ModManager/。路径硬编码在 ModManager.smali，
# 如需自定义请修改 smali 内常量，或使用 ModManagerUpdater。

# 回编译 & 签名
apktool b target_src -o repacked.apk
uber-apk-signer --apks repacked.apk
```

## 部署管理器

将管理器 DLL 及依赖放入 `/sdcard/ModManager/manager/`。

| File | 来源 |
|---|---|
| `StArray.ModManager.dll` | [Releases](https://github.com/StArraySharp/StArray.ModManager/releases) → `modmanager.zip` |
| `ImGui.NET.dll` | 同上 |
| `OpenTK.Graphics.dll` | 同上 |

最终目录：

```
/sdcard/ModManager/
├── manager/
│   ├── StArray.ModManager.dll
│   ├── ImGui.NET.dll
│   ├── OpenTK.Graphics.dll
│   └── modmanager_config.json    （自动生成）
└── mods/
    └── {YourModName}/
        ├── {YourModName}.dll
        └── ...
```

## Hook 系统

提供两套独立 Hook 属性，分别适用于不同场景。

### [NativeHook] — 原生 so 函数 Hook

Hook `.so` 共享库中的导出函数（如 `libinput.so`、`libunity.so`），支持三种模式：

#### 1. 符号导出 Hook

按 `.dynsym` 导出的符号名 hook 原生函数：

```csharp
[NativeHook("libinput.so", "_ZN7android13InputConsumer14consumeSamplesEPNS_26InputEventFactoryInterfaceERNS0_5BatchEmPjPPNS_10InputEventE")]
static bool OnConsumeSamples(void* thiz, void* factory, IntPtr batch, ulong count, uint* outSeq, void** outEvent)
{
    return OnConsumeSamplesOriginal(thiz, factory, batch, count, outSeq, outEvent);
}
```

#### 2. RVA Hook

已知函数在 so 内的 RVA 偏移时使用（绕过符号混淆）：

```csharp
[NativeHook("libunity.so", 0x123456)]
static void OnSomeFunc() { }
```

#### 3. 解析器方法 Hook

引用同类内的一个 `static nint MethodName()` 方法，由该方法返回目标地址（支持特征码搜索、ELF 符号扫描等自定义解析逻辑）：

```csharp
[NativeHook(nameof(ResolveTarget))]
static void OnSomeFunc()
{
    // ...
}

static nint ResolveTarget()
{
    NativeFuncResolver resolver = new("/system/lib64/libinput.so");
    long rva = resolver.FindRva("_ZN7android13InputConsumer21initializeMotionEventEPNS_11MotionEventEPKNS_12InputMessageE");
    resolver.Load();
    return resolver.GetFuncPtr(rva);
}
```

跨类引用可用 `"Namespace.Type.Method"` 格式，`::` 自动转换为 `.`。

### 源码生成

`[NativeHook]` 通过 Roslyn 源码生成器在编译时自动生成 partial 类，包含：

| 生成成员 | 说明 |
|---|---|
| `InstallHooks()` | 安装所有 hook |
| `UninstallHooks()` | 卸载所有 hook |
| `MethodNameOriginal` | 调用原函数的委托 |

生成的 partial class 保留原始类的访问修饰符（`public`/`internal`）。

### 使用 NativeHook

```csharp
public static partial class MyHooks
{
    [NativeHook("libinput.so", "_ZN7android13InputConsumer14consumeSamplesEPNS_26InputEventFactoryInterfaceERNS0_5BatchEmPjPPNS_10InputEventE")]
    static long OnConsumeSamples(void* thiz, void* factory, IntPtr batch, ulong count, uint* outSeq, void** outEvent)
    {
        // 调用原函数
        var result = OnConsumeSamplesOriginal(thiz, factory, batch, count, outSeq, outEvent);
        // 自定义逻辑
        return result;
    }
}

// 安装：
MyHooks.InstallHooks();
// 卸载：
MyHooks.UninstallHooks();
```

### NativeFuncResolver

ELF 文件解析工具，用于查找符号 RVA、特征码扫描、计算最终函数指针：

| 方法 | 说明 |
|---|---|
| `FindRva(string symbol, byte?[]? pattern)` | 符号名 → RVA，失败则回退特征码 |
| `FindSymbolRva(string)` | 在 `.dynsym` + `.dynstr` 中精确匹配 |
| `FindRva(byte?[] pattern)` | 在 `.text` 段特征码搜索 |
| `ParseHexPattern(string)` | 转换 `"48 89 ?? ?? ff"` 至匹配模式 |
| `Resolve(string, byte?[]?)` | 加载库 + 查 RVA → 返回函数指针 |
| `Load()` | 加载 so 到进程（优先已加载基址） |
| `GetFuncPtr(long rva)` | 返回 `base + rva` 函数指针 |

特征码使用 `??` 表示通配符：

```csharp
var resolver = new NativeFuncResolver("/system/lib64/libinput.so");
byte?[] sig = NativeFuncResolver.ParseHexPattern(
    "e8 0f 19 fc ?? ?? ?? ?? fc 6f 02 a9 ?? ?? ?? ??"
);
long rva = resolver.FindRva("symbolName", sig);
resolver.Load();
nint funcPtr = resolver.GetFuncPtr(rva);
```

## 写 Mod（跨平台）

### 引用 DLL

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

    public void OnForegroundGUI(ImDrawListPtr drawList)
    {
        drawList.AddText(new Vector2(10, 10), 0xFFFFFFFF, $"FPS: {ImGui.GetIO().Framerate:F0}");
    }
}
```

### 可设置项的 Mod

```csharp
public class ConfigurableMod : IModPlugin, IModSettings
{
    public string Id => "com.example.configurable";

    [ModSettingLabel("Show HUD")]
    [ModSettingLabelSide(ModInspector.LabelSide.Right)]
    public bool ShowHud = true;

    [ModSettingLabel("Scale")]
    [ModSettingRange(0.5f, 3f)]
    public float Scale = 1f;

    public void OnGui() => ModInspector.Draw(this);
}
```

### [UnmanagedHook] — IL2CPP / Mono 方法 Hook

Hook Unity 托管方法（IL2CPP 或 Mono 运行时），通过程序集名、类名、方法名定位：

```csharp
// 3 参数：Il2Cpp 风格（无命名空间）
[UnmanagedHook("Assembly-CSharp.dll", "scrController", "TogglePauseGame")]
static bool TogglePauseGame(nint thiz)
{
    var ret = TogglePauseGameOriginal(thiz);
    Logger.Warn("MyMod", "Game paused");
    return ret;
}

// 4 参数：Mono 风格（带命名空间）
[UnmanagedHook("Assembly-CSharp.dll", "GameLogic", "PlayerController", "TakeDamage")]
static void TakeDamage(nint thiz, int amount)
{
    TakeDamageOriginal(thiz, Math.Min(amount, 10)); // 减伤
}
```

### 定义 Stub DLL — 源码生成器

将 Stub 定义在单独的类库项目中，编译时 `UnmanagedStubGenerator` 自动生成方法实现，输出可直接分发的 Stub DLL。

```csharp
using StArray.ModManager.RuntimeAbstractions;

// [UnmanagedType(assembly, namespace, class)] 标记目标游戏类
[UnmanagedType("Assembly-CSharp.dll", "", "scrController")]
public partial class ScrControllerStub
{
    // partial 方法 + [UnmanagedMember] → 生成器自动实现运行时反射调用
    [UnmanagedMember]
    public partial int CurrentSeqID();

    [UnmanagedMember]
    public partial nint PlayerOne();

    [UnmanagedMember]
    public partial void TogglePauseGame();
}
```

编译后，Stub DLL 可直接被 Mod 项目引用使用：

```csharp
// Mod 项目引用 Stub DLL，无需自己写运行时反射
var controller = new ScrControllerStub(ptr);
int seqId = controller.CurrentSeqID();
controller.TogglePauseGame();
```

### 使用 UnmanagedStub + UnmanagedHook

参考 [LevelDebugger](LevelDebugger/)，分三步：

1. **定义 Stub** — `partial class` + `[UnmanagedType]` + `[UnmanagedMember]`，生成器自动产出实现
2. **声明 Hook** — 用 `[UnmanagedHook]` 标记替换方法
3. **注册设置** — 用 `[ModSettingLabel]` 特性声明可开关项

```csharp
// Stub DLL 项目 — 声明游戏类接口
[UnmanagedType("Assembly-CSharp.dll", "", "scrController")]
public partial class scrController
{
    [UnmanagedMember] public partial int CurrentSeqID();
    [UnmanagedMember] public partial nint PlayerOne();
    [UnmanagedMember] public partial void TogglePauseGame();
}

// Mod 项目 — 引用 Stub DLL + Hook
public class MyMod : IModPlugin
{
    [UnmanagedHook("Assembly-CSharp.dll", "scrController", "TogglePauseGame")]
    static bool TogglePauseGame(nint thiz)
    {
        var ret = TogglePauseGameOriginal(thiz);
        var ctrl = new scrController(thiz);
        Logger.Warn("MyMod", $"Paused at seq {ctrl.CurrentSeqID()}");
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

### 发布

Mod 部署到 `mods/{ModId}/` 目录，ModManager 自动识别：

```
GameFolder/
└── Corehold/
    └── mods/
        └── {YourModId}/
            ├── {YourModId}.dll
            └── 依赖.dll
```

Android 同样部署到 `/sdcard/ModManager/mods/{YourModId}/`。

### 可选接口

```csharp
interface IModSettings { void OnGui(); }                    // 自定义设置页
interface IModSettingCustomDraw { void DrawInspector(); }   // 自定义检查器
```

### 设置特性

```csharp
[ModSettingLabel("Health Multiplier")]
[ModSettingRange(0.1f, 10f)]
public float HealthMultiplier = 1f;

[ModSettingLabelSide(ModInspector.LabelSide.Left)]
[ModSettingJson(Lines = 10)]
public string JsonConfig = "{}";
```

### Overlay 绘制

```csharp
void OnBackgroundGUI(ImDrawListPtr d) { }  // 窗口下方（水印）
void OnForegroundGUI(ImDrawListPtr d) { }  // 窗口上方（HUD）
```
