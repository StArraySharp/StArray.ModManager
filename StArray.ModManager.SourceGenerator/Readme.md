# StArray.ModManager.SourceGenerator

Roslyn 增量 Source Generator 项目，用于自动生成原生函数 Hook / il2cpp Hook 的基础设施代码。

## 特性

- **`[NativeHook]`** — 标记一个静态方法为原生函数 Hook 替换方法。  
  调用约定默认 **StdCall**（Win32 API 标准），可通过 `Convention` 属性改为 `Cdecl`。
- **`[Il2CppHook]`** — 标记一个静态方法为 il2cpp 方法 Hook 替换方法。  
  调用约定固定为 **Cdecl**，**不可配置**（il2cpp 运行时只使用 Cdecl）。

## 用法

### 1）引用 Source Generator

在需要使用的 `.csproj` 中添加 Analyzer 引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\StArray.ModManager.SourceGenerator\StArray.ModManager.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

> 目前已在 `StArray.ModManager` 和 `StArray.ModManager.Windows` 项目中加入。

### 2）**NativeHook** — Hook 原生函数

```csharp
using StArray.ModManager.Hooks;

public static partial class MyHooks
{
    [NativeHook("user32.dll", "MessageBoxW")]
    public static int MyMessageBoxW(nint hWnd, nint text, nint caption, uint type)
    {
        // 调用原始函数
        return MyMessageBoxWOriginal(hWnd, text, caption, type);
    }
}
```

生成器会自动生成：
- 匹配函数签名的委托类型 `_MyMessageBoxWDelegate`
- `_MyMessageBoxW_origPtr` / `_MyMessageBoxW_orig` 字段
- `InstallHooks()` / `UninstallHooks()` 批量管理方法
- `MyMessageBoxWOriginal(...)` 原函数调用方法

### 3）**Il2CppHook** — Hook il2cpp 方法

```csharp
[Il2CppHook("Assembly-CSharp", "PlayerController", "Update")]
public static void OnPlayerUpdate(nint instance)
{
    Console.WriteLine("Player.Update called!");
    OnPlayerUpdateOriginal(instance);  // 调用原始
}
```

可选命名参数 `ParameterCount` 用于区分方法重载（按参数个数）：

```csharp
[Il2CppHook("Assembly-CSharp", "PlayerController", "Update", ParameterCount = 1)]
```

也可以使用 `ParameterTypeNames` 按参数类型名精确区分重载：

```csharp
[Il2CppHook("Assembly-CSharp", "PlayerController", "SetHealth", ParameterTypeNames = new[] { "System.Single" })]
```

### 4）运行时初始化

```csharp
// Windows：设置 MinHook 实现
StArray.ModManager.Runtime.HookHelper.Instance = new StArray.ModManager.Native.MinHook();

// 安装所有 Hook
MyHooks.InstallHooks();

// 卸载
MyHooks.UninstallHooks();
```

## 项目结构

- `HookGenerator.cs` — 增量 Source Generator 核心
- `Properties/launchSettings.json` — 调试配置
- `StArray.ModManager.SourceGenerator.csproj` — 项目文件（netstandard2.0）
