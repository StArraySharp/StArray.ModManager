# StArray.ModManager

Android IL2CPP Unity mod manager with CoreCLR runtime embedding and ImGui overlay UI.

## How It Works

1. **Native injection** via `libmodmanager.so` loaded into Unity process
2. **Dobby hook** on `eglSwapBuffers` and Android input events
3. **CoreCLR embedded** at runtime, launching .NET managed code from JNI
4. **UnityResolve** reflection engine traverses IL2CPP/Mono managed types
5. **ImGui overlay** rendered via EGL + OpenGL ES with touch and keyboard IME

## Project Structure

```
StArray.ModManager/              C# core library (.NET 10)
  Behaviours/                    游戏生命周期行为
    BehaviourManager.cs          行为调度器
    GameBehaviour.cs             行为基类
  Il2Cpp/                        IL2CPP 运行时反射实现
    Domain.cs                    IL2CPP 域
    Il2CppFunctions.cs           IL2CPP 原生函数导入
    Il2CppReflection.cs          Assembly/Class/Method/Field 反射
  Inspector/                     ImGui 自动检查器 + 设置特性
    ModInspector.cs              检查器入口
    ModInspector.Build.cs        配置面板构建
    ModInspector.Draw.cs         字段绘制
    ModSettingAttributes.cs      设置特性（[ModSettingLabel], [ModSettingRange] 等）
  Manager/                       核心逻辑
    Benchmark.cs                 栈式嵌套计时
    Logger.cs                    统一日志
    ModEntry.cs                  Mod 条目数据模型
    ModManagerConfig.cs          全局配置 JSON 持久化（STJ 源生成）
    ModManagerJsonContext.cs      STJ 序列化上下文
    ModManagerUI.cs              ImGui 主面板 + 设置窗口
    ModManagerUI.Panels.cs       面板子模块
  Mono/                          Mono 运行时反射实现
    Mono.cs                      Mono 域
    MonoDomain.cs                Mono 域操作（Assembly 队列、Flush 等）
    MonoFunctions.cs             Mono 原生函数导入
    MonoReflection.cs            Assembly/Class/Method/Field 反射
  Resources/                     嵌入资源
    fa-solid-900.ttf             FontAwesome 7 图标字体
    msyh.ttf                     微软雅黑字体
    L10n.cs / Localization.*     本地化字符串
  Runtime/                       Mod 框架接口
    HookAttributes.cs            [UnmanagedHook] 特性
    HookHelper.cs                Hook 注册工具
    IHook.cs                     Hook 抽象
    IModPlugin.cs                Mod 入口（OnLoad/OnUnload/OnBackgroundGUI/OnForegroundGUI）
    IModSettings.cs / IModSettingCustomDraw.cs  设置接口
    ModLoader.cs                 Mod 扫描/加载/卸载/状态管理
  RuntimeAbstractions/           运行时抽象层（屏蔽 IL2CPP/Mono 差异）
    IAppDomain.cs                域抽象
    IRuntimeAssembly.cs          程序集抽象
    IRuntimeClass.cs             类抽象
    IRuntimeField.cs / IRuntimeMethod.cs  字段/方法抽象
    IUnmanagedObject.cs          UnmanagedObject 基类 + Wrap<T>()
    RuntimeArray.cs              强类型数组访问
    RuntimeBox.cs                装箱/拆箱
    RuntimeManager.cs            运行时选择（IL2CPP vs Mono）
    RuntimeObject.cs             对象字段/方法/属性通用操作
    RuntimeString.cs             string 读写
    UnmanagedMemberAttribute.cs  字段/方法声明特性
    GraphicsDevice.cs            GPU 设备抽象
  TestStubs/                     测试 Stub
    Transform.cs                 Unity Transform 测试 Stub
  UI/                            ImGui 工具
    FontAwesome7.cs              FA7 图标常量
    IImGuiRenderer.cs            渲染器接口

Android/library/                 原生库 (libmodmanager.so)
  src/main/cpp/core/             Dobby hook / CoreCLR 嵌入 / JNI helper / UnityResolve
  src/main/java/
    ModManager.java              CoreCLR 启动器
    ModManagerUpdater.java       OTA 自动更新（CompletableFuture + AlertDialog）
    ModManagerUtils.java         IME KeyboardView + InputConnection
```

## Features

- **EGL SwapBuffers hook** with Dobby — renders ImGui overlay every frame
- **Touch & key input** via InputConsumer hooks + cimgui Android backend
- **IME support** (Chinese/Japanese) via custom KeyboardView + InputConnection bridge
- **Mod system** — scan/load/unload with dependency resolution + auto-enable on restart
- **Mod overlay API** — `OnBackgroundGUI` / `OnForegroundGUI` for direct game-screen drawing
- **Auto-update** — OTA version check + download + SHA-256 verification + restart
- **Config persistence** — STJ source-gen JSON, AOT-compatible
- **File logging** — dual-write to logcat + `manager.log`
- **GL debug panel** — caps toggles, blend/depth func selectors, GL state queries
- **FontAwesome 7** icon support via embedded resource
- **IImGuiRenderer** interface — swap between EGL, Vulkan backends
- **CoreCLR args** — pass `string[]` from Java to managed `Entry(int, IntPtr)`

## API Reference

### Mod Entry Point

| Interface | Method | Description |
|-----------|--------|-------------|
| `IModPlugin` | `OnLoad()` | Called when mod is loaded |
| | `OnUnload()` | Called when mod is unloaded |
| | `OnBackgroundGUI(ImDrawListPtr)` | Draw behind ImGui windows (watermark) |
| | `OnForegroundGUI(ImDrawListPtr)` | Draw above ImGui windows (HUD) |
| `IModSettings` | `OnGui()` | Draw custom settings tab in ModManager |

### Hooks

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[UnmanagedHook("dll", "class", "method")]` | `static bool/nvoid Method(nint thiz, ...)` | Hooks a game method, replaces it with your implementation. Define `MethodOriginal` for calling the original. |

### Stubs (Typed Game Object Wrappers)

| Base Class | Description |
|------------|-------------|
| `UnmanagedObject` | Wraps `nint` pointer, provides `Obj` field for runtime access. Subclass to type-safe game object access. |
| `Wrap<T>(nint ptr)` | Returns a typed stub wrapping the given pointer. |

### Runtime Reflection

| Class | Key Methods | Description |
|-------|-------------|-------------|
| `RuntimeObject` | `GetField<T>(string)` / `SetField(string, T)` | Read/write instance fields |
| | `Invoke<T>(string, params object[])` | Call instance methods |
| | `GetProperty<T>(string)` / `SetProperty(string, T)` | Read/write properties |
| `RuntimeArray<T>` | `new RuntimeArray<T>(nint ptr)` | Strongly-typed array wrapper |
| | `.Length` / `[index]` | Array length and element access |
| | `.DataPtr` | Direct pointer to array elements |
| `RuntimeManager` | `GetDomain()` / `IsIl2Cpp` | Detect current runtime and get domain |
| `IAppDomain` | `OpenAssembly(string)` | Open a loaded assembly |
| `IRuntimeAssembly` | `GetClass(string ns, string name)` | Get a class by namespace + name |
| `IRuntimeClass` | `GetMethod(string, int)` / `GetField(string)` | Get method/field for invocation |
| `IRuntimeMethod` | `Invoke(nint thisPtr, params object[])` / `InvokeStatic()` | Call a method |
| `IRuntimeField` | `GetValue<T>(nint)` / `SetValue(nint, T)` | Read/write a field |

### Settings Attributes

| Attribute | Parameters | Description |
|-----------|------------|-------------|
| `[ModSettingLabel("text")]` | `string` | Display label for a setting field |
| `[ModSettingLabelSide(ModInspector.LabelSide)]` | `Left` / `Right` | Label position |
| `[ModSettingRange(min, max)]` | `float, float` | Clamp numeric values to range |
| `[ModSettingJson(Lines = N)]` | `int` | Multi-line JSON editor |

### Inspector

| Method | Description |
|--------|-------------|
| `ModInspector.Draw(object)` | Auto-generate UI for all `[ModSettingLabel]` fields on the given object |

### Utilities

| Class | Description |
|-------|-------------|
| `Logger` | `Info(string)`, `Warn(string)`, `Error(string)` — unified logging |
| `HookHelper` | `Instance` — set to your hook implementation (`MinHook` etc.) |
| `BehaviourManager` | Schedule actions on Unity lifecycle events (Update, LateUpdate, etc.) |

## Third-Party Libraries

| Library | License | Used In |
|---------|---------|---------|
| [Dear ImGui](https://github.com/ocornut/imgui) | MIT | UI rendering |
| [cimgui](https://github.com/cimgui/cimgui) | MIT | ImGui C API bindings |
| [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) | MIT | C# ImGui bindings |
| [kiero2](https://github.com/kirchesz/kiero2) | MIT | Graphics API detection (D3D9/11/12/GL/VK) |
| [MinHook](https://github.com/TsudaKageyu/minhook) | BSD-2 | API hooking |
| [Corehold](https://github.com/StArraySharp/Corehold) | MIT | winmm proxy DLL + CoreCLR hosting |
| [Dobby](https://github.com/jmpews/Dobby) | Apache-2.0 | Android inline hook |
| [CoreCLR](https://github.com/dotnet/runtime) | MIT | .NET runtime |
| [FontAwesome 7](https://fontawesome.com) | OFL/SIL | Icon font |

## Build

Requires .NET 10 SDK + MinGW (gcc/cmake on PATH).

```bash
# Windows (native + C#, one command)
dotnet build StArray.ModManager.Windows -c Release

# Android
cd Android && ./gradlew :library:assembleRelease
```

## Windows Architecture

The native DLL (`StArray.ModManager.Windows.Native.dll`) uses
[kiero2](https://github.com/kirchesz/kiero2) for graphics API detection and
[MinHook](https://github.com/TsudaKageyu/minhook) for `Present` hooking.

**Per-backend hook files** (`Native/`):

| File | Backend | Init | Render |
|------|---------|------|--------|
| `hook_d3d12.cpp` | D3D12 | Descriptor heaps, command list, RTV per-frame contexts | Resource barriers + OMSetRenderTargets |
| `hook_d3d11.cpp` | D3D11 | Device + Context + persistent backbuffer RTV | OMSetRenderTargets + RenderDrawData |
| `hook_d3d9.cpp`  | D3D9  | SwapChain QI → Device | Viewport/scissor save-restore |
| `main.cpp`       | GL/VK | Win32 init only (C# handles backend) | C# render callback only |

**Unified dispatch** (`main.cpp` `hkPresent`):
- All backends share `DisplaySize` (from swapchain desc), `ImGui_ImplWin32_NewFrame`, WndProc hook
- `DEBUG_LOG` conditional compilation (`NDEBUG` → compiled out in Release)
- Window handle from `SwapChain::GetDesc().OutputWindow` (kiero approach)

```mermaid
flowchart LR
    CSharp[C# ImGuiRenderer] -->|P/Invoke Init| Native[Native DLL]
    Native -->|kiero2 detect| API[D3D9/11/12/GL/VK]
    API -->|MinHook| Present[hkPresent]
    Present -->|dispatch| D3D11[hook_d3d11]
    Present -->|dispatch| D3D12[hook_d3d12]
    Present -->|dispatch| D3D9[hook_d3d9]
    D3D11 & D3D12 & D3D9 -->|callback| CSharp
    CSharp -->|ImGui.NET| cimgui[cimgui.dll]
```

## Target

- Windows x64 (D3D9/11/12) — DLL injection or CoreCLR hosting
- Android arm64-v8a (OpenGL ES) — IL2CPP Unity games (API 26+)
- CoreCLR .NET 10 runtime

## Getting Started

```bash
git clone --recurse-submodules https://github.com/StArraySharp/StArray.ModManager.git
dotnet build StArray.ModManager.Windows -c Release
```

See [GET_STARTED.md](GET_STARTED.md) for injection and build instructions.

Runtime assemblies can be downloaded from the [runtime-references release](https://github.com/StArraySharp/StArray.ModManager/releases/tag/0).

---

Most code generated by AI.
