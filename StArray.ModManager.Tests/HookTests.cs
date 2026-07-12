using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Hooks;
using StArray.ModManager.Runtime;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Tests;

// ═══════════════════════════════════════════════════════
// test_native.dll 解析器 —— 确保 [DllImport("test_native")]
// 能找到 GCC 编译输出的 DLL
// ═══════════════════════════════════════════════════════
internal static class NativeResolver
{
    private static bool _registered;

    [ModuleInitializer]
    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        NativeLibrary.SetDllImportResolver(typeof(NativeResolver).Assembly, Resolve);
    }

    private static IntPtr Resolve(string name, Assembly asm, DllImportSearchPath? path)
    {
        // test_native.dll 就在输出目录根下（由 BuildNativeTestDll MSBuild 目标产出）
        if (name != "test_native") return IntPtr.Zero;

        var dllPath = Path.Combine(AppContext.BaseDirectory, "test_native.dll");
        if (File.Exists(dllPath) && NativeLibrary.TryLoad(dllPath, out var h))
            return h;
        return IntPtr.Zero;
    }
}

// ═══════════════════════════════════════════════════════
// P/Invoke 声明 —— 对应 native/test_native.c 的导出函数
// ═══════════════════════════════════════════════════════
public static class NativeFunc
{
    // ── __cdecl ─────────────────────────────────────

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetMagicNumber();

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Add(int a, int b);

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StringLength(string str);

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetCallCount();

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern void IncrementCallCount();

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ResetCallCount();

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetPi();

    // ── __stdcall（x64 上二进制同 Cdecl，但属性不同） ──

    [DllImport("test_native", CallingConvention = CallingConvention.StdCall)]
    public static extern uint S_GetMagicNumber();

    [DllImport("test_native", CallingConvention = CallingConvention.StdCall)]
    public static extern int S_Add(int a, int b);

    // ── __fastcall ─────────────────────────────────

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint F_GetMagicNumber();

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern int F_Add(int a, int b);

    // ── __thiscall ─────────────────────────────────

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint T_GetMagicNumber();

    [DllImport("test_native", CallingConvention = CallingConvention.Cdecl)]
    public static extern int T_Add(int a, int b);
}

// ═══════════════════════════════════════════════════════
// Hook 定义 —— 使用 NativeHook 拦截 test_native 函数
// ═══════════════════════════════════════════════════════
public static partial class TestHooks
{
    // ── __cdecl Hook ────────────────────────────────

    [NativeHook("test_native", "GetMagicNumber", Convention = CallingConvention.Cdecl)]
    public static uint HookGetMagicNumber()
    {
        return 0x42;
    }

    [NativeHook("test_native", "Add", Convention = CallingConvention.Cdecl)]
    public static int HookAdd(int a, int b)
    {
        return a + b + 1;
    }

    public static int NativeCallCount;

    [NativeHook("test_native", "IncrementCallCount", Convention = CallingConvention.Cdecl)]
    public static void HookIncrementCallCount()
    {
        NativeCallCount++;
        HookIncrementCallCountOriginal();
    }

    // ── __stdcall Hook ─────────────────────────────

    [NativeHook("test_native", "S_GetMagicNumber", Convention = CallingConvention.StdCall)]
    public static uint HookS_GetMagicNumber()
    {
        return 0x1234;
    }

    [NativeHook("test_native", "S_Add", Convention = CallingConvention.StdCall)]
    public static int HookS_Add(int a, int b)
    {
        return HookS_AddOriginal(a, b) * 10;
    }

    // ── __fastcall Hook ───────────────────────────

    [NativeHook("test_native", "F_GetMagicNumber", Convention = CallingConvention.FastCall)]
    public static uint HookF_GetMagicNumber()
    {
        return 0x5678;
    }

    [NativeHook("test_native", "F_Add", Convention = CallingConvention.FastCall)]
    public static int HookF_Add(int a, int b)
    {
        return HookF_AddOriginal(a, b) + 1000;
    }

    // ── __thiscall Hook ───────────────────────────

    [NativeHook("test_native", "T_GetMagicNumber", Convention = CallingConvention.ThisCall)]
    public static uint HookT_GetMagicNumber()
    {
        return 0x9ABC;
    }

    [NativeHook("test_native", "T_Add", Convention = CallingConvention.ThisCall)]
    public static int HookT_Add(int a, int b)
    {
        return HookT_AddOriginal(a, b) * 100;
    }
}

// ═══════════════════════════════════════════════════════
// 基础测试 —— 验证 P/Invoke 工作正常
// ═══════════════════════════════════════════════════════
public class PInvokeTests
{
    [Test]
    public void GetMagicNumber_ReturnsExpected()
    {
        var val = NativeFunc.GetMagicNumber();
        Assert.That(val, Is.EqualTo(0xDEADBEEF));
    }

    [Test]
    public void Add_ReturnsSum()
    {
        Assert.That(NativeFunc.Add(3, 5), Is.EqualTo(8));
        Assert.That(NativeFunc.Add(-1, 1), Is.EqualTo(0));
    }

    [Test]
    public void StringLength_Works()
    {
        Assert.That(NativeFunc.StringLength("Hello"), Is.EqualTo(5));
        Assert.That(NativeFunc.StringLength(""), Is.EqualTo(0));
    }

    [Test]
    public void CallCount_Lifecycle()
    {
        NativeFunc.ResetCallCount();
        Assert.That(NativeFunc.GetCallCount(), Is.EqualTo(0));

        NativeFunc.IncrementCallCount();
        NativeFunc.IncrementCallCount();
        NativeFunc.IncrementCallCount();
        Assert.That(NativeFunc.GetCallCount(), Is.EqualTo(3));

        NativeFunc.ResetCallCount();
        Assert.That(NativeFunc.GetCallCount(), Is.EqualTo(0));
    }
}

// ═══════════════════════════════════════════════════════
// Hook 测试 —— 验证 NativeHook 对 CDECL 函数生效
// ═══════════════════════════════════════════════════════
public class HookTests
{
    [SetUp]
    public void Setup()
    {
        // 清理上一轮 hook、计数
        TestHooks.NativeCallCount = 0;
        TestHooks.UninstallHooks();
        HookHelper.Instance = null;
    }

    // ─── 安装/卸载流程 ─────────────────────────────

    [Test]
    public void Install_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => TestHooks.InstallHooks());
    }

    [Test]
    public void Uninstall_DoesNotThrow()
    {
        TestHooks.InstallHooks();
        Assert.DoesNotThrow(() => TestHooks.UninstallHooks());
    }

    [Test]
    public void DoubleInstall_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            TestHooks.InstallHooks();
            TestHooks.InstallHooks();
            TestHooks.UninstallHooks();
        });
    }

    [Test]
    public void Uninstall_ClearsOriginals()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        TestHooks.UninstallHooks();

        // _orig == null → Original() 返回 default
        Assert.That(TestHooks.HookGetMagicNumberOriginal(), Is.EqualTo(0u));
    }

    // ─── GetMagicNumber Hook 效果 ──────────────────

    [Test]
    public void Hook_GetMagicNumber_Intercepts()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        // P/Invoke 走 Hook → 返回 0x42
        Assert.That(NativeFunc.GetMagicNumber(), Is.EqualTo(0x42));

        // Original 走原生 → 返回 0xDEADBEEF
        Assert.That(TestHooks.HookGetMagicNumberOriginal(), Is.EqualTo(0xDEADBEEF));
    }

    [Test]
    public void Hook_GetMagicNumber_Unhooked_ReturnsNormal()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        Assert.That(NativeFunc.GetMagicNumber(), Is.EqualTo(0x42));

        TestHooks.UninstallHooks();

        // 卸载后恢复原始值
        Assert.That(NativeFunc.GetMagicNumber(), Is.EqualTo(0xDEADBEEF));
    }

    // ─── Add Hook — 验证参数传递 ───────────────────

    [Test]
    public void Hook_Add_ModifiesResult()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        // HookAdd = AddOriginal(a,b) + 1
        // 正常 Add(2,3)=5, Hook 后=6
        Assert.That(NativeFunc.Add(2, 3), Is.EqualTo(6));
        Assert.That(NativeFunc.Add(0, 0), Is.EqualTo(1)); // 0+0+1=1

        TestHooks.UninstallHooks();
    }

    [Test]
    public void Hook_Add_OriginalStillCorrect()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        // Hook → 6
        Assert.That(NativeFunc.Add(2, 3), Is.EqualTo(6));
        // Original → 5
        Assert.That(TestHooks.HookAddOriginal(2, 3), Is.EqualTo(5));

        TestHooks.UninstallHooks();
    }

    // ─── IncrementCallCount — 验证副作用 ───────────

    [Test]
    public void Hook_IncrementCallCount_TracksManaged()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        NativeFunc.ResetCallCount();

        Assert.That(TestHooks.NativeCallCount, Is.EqualTo(0));

        NativeFunc.IncrementCallCount();
        Assert.That(TestHooks.NativeCallCount, Is.EqualTo(1));
        Assert.That(NativeFunc.GetCallCount(), Is.EqualTo(1)); // 原生也被调用

        NativeFunc.IncrementCallCount();
        NativeFunc.IncrementCallCount();
        Assert.That(TestHooks.NativeCallCount, Is.EqualTo(3));
        Assert.That(NativeFunc.GetCallCount(), Is.EqualTo(3));

        TestHooks.UninstallHooks();
    }

    // ─── 多 Hook 协同 ─────────────────────────────

    [Test]
    public void Hook_MultipleIndependent()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        Assert.That(NativeFunc.GetMagicNumber(), Is.EqualTo(0x42));
        Assert.That(NativeFunc.Add(10, 20), Is.EqualTo(31)); // 10+20+1

        TestHooks.UninstallHooks();
    }

    // ─── 无 HookHelper 降级 ───────────────────────

    [Test]
    public void Original_ReturnsDefault_WhenNeverInstalled()
    {
        HookHelper.Instance = null;
        TestHooks.UninstallHooks(); // 确保 _orig 被清空
        Assert.That(TestHooks.HookGetMagicNumberOriginal(), Is.EqualTo(0u));
    }
}

// ═══════════════════════════════════════════════════════
// __stdcall 调用约定测试
// ═══════════════════════════════════════════════════════
public class StdCallTests
{
    [SetUp]
    public void Cleanup() { HookHelper.Instance = null; TestHooks.UninstallHooks(); }

    [Test]
    public void PInvoke_Normal()
    {
        Assert.That(NativeFunc.S_GetMagicNumber(), Is.EqualTo(0x600D));
        Assert.That(NativeFunc.S_Add(7, 8), Is.EqualTo(15));
    }

    [Test]
    public void Hook_Intercepts()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        Assert.That(NativeFunc.S_GetMagicNumber(), Is.EqualTo(0x1234));
        Assert.That(NativeFunc.S_Add(7, 8), Is.EqualTo(150)); // 15 * 10

        TestHooks.UninstallHooks();
    }

    [Test]
    public void Hook_Original()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        Assert.That(TestHooks.HookS_AddOriginal(7, 8), Is.EqualTo(15));

        TestHooks.UninstallHooks();
    }

    [Test]
    public void Hook_Unhook_ReturnsNormal()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        Assert.That(NativeFunc.S_GetMagicNumber(), Is.EqualTo(0x1234));

        TestHooks.UninstallHooks();
        Assert.That(NativeFunc.S_GetMagicNumber(), Is.EqualTo(0x600D));
    }
}

// ═══════════════════════════════════════════════════════
// __fastcall 调用约定测试
// ═══════════════════════════════════════════════════════
public class FastCallTests
{
    [SetUp]
    public void Cleanup() { HookHelper.Instance = null; TestHooks.UninstallHooks(); }

    [Test]
    public void PInvoke_Normal()
    {
        Assert.That(NativeFunc.F_GetMagicNumber(), Is.EqualTo(0xCAFE));
        Assert.That(NativeFunc.F_Add(5, 6), Is.EqualTo(11));
    }

    [Test]
    public void Hook_Intercepts()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        Assert.That(NativeFunc.F_GetMagicNumber(), Is.EqualTo(0x5678));
        Assert.That(NativeFunc.F_Add(5, 6), Is.EqualTo(1011)); // 11 + 1000

        TestHooks.UninstallHooks();
    }

    [Test]
    public void Hook_Original()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        Assert.That(TestHooks.HookF_AddOriginal(5, 6), Is.EqualTo(11));
        TestHooks.UninstallHooks();
    }

    [Test]
    public void Hook_Unhook_ReturnsNormal()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        Assert.That(NativeFunc.F_Add(10, 20), Is.EqualTo(1030));
        TestHooks.UninstallHooks();
        Assert.That(NativeFunc.F_Add(10, 20), Is.EqualTo(30));
    }
}

// ═══════════════════════════════════════════════════════
// __thiscall 调用约定测试
// ═══════════════════════════════════════════════════════
public class ThisCallTests
{
    [SetUp]
    public void Cleanup() { HookHelper.Instance = null; TestHooks.UninstallHooks(); }

    [Test]
    public void PInvoke_Normal()
    {
        Assert.That(NativeFunc.T_GetMagicNumber(), Is.EqualTo(0xFACE));
        Assert.That(NativeFunc.T_Add(3, 7), Is.EqualTo(10));
    }

    [Test]
    public void Hook_Intercepts()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        Assert.That(NativeFunc.T_GetMagicNumber(), Is.EqualTo(0x9ABC));
        Assert.That(NativeFunc.T_Add(3, 7), Is.EqualTo(1000)); // 10 * 100

        TestHooks.UninstallHooks();
    }

    [Test]
    public void Hook_Original()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        Assert.That(TestHooks.HookT_AddOriginal(3, 7), Is.EqualTo(10));
        TestHooks.UninstallHooks();
    }
}

// ═══════════════════════════════════════════════════════
// 跨约定协同测试
// ═══════════════════════════════════════════════════════
public class MixedConventionTests
{
    [SetUp]
    public void Cleanup() { HookHelper.Instance = null; TestHooks.UninstallHooks(); }

    [Test]
    public void AllConventions_HookTogether()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        // __cdecl
        Assert.That(NativeFunc.GetMagicNumber(), Is.EqualTo(0x42));
        // __stdcall
        Assert.That(NativeFunc.S_GetMagicNumber(), Is.EqualTo(0x1234));
        // __fastcall
        Assert.That(NativeFunc.F_GetMagicNumber(), Is.EqualTo(0x5678));
        // __thiscall
        Assert.That(NativeFunc.T_GetMagicNumber(), Is.EqualTo(0x9ABC));

        TestHooks.UninstallHooks();
    }

    [Test]
    public void AllConventions_OriginalAfterUnhook()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();
        TestHooks.UninstallHooks();

        Assert.That(NativeFunc.GetMagicNumber(), Is.EqualTo(0xDEADBEEF));
        Assert.That(NativeFunc.S_GetMagicNumber(), Is.EqualTo(0x600D));
        Assert.That(NativeFunc.F_GetMagicNumber(), Is.EqualTo(0xCAFE));
        Assert.That(NativeFunc.T_GetMagicNumber(), Is.EqualTo(0xFACE));
    }
}

// ═══════════════════════════════════════════════════════
// 诊断 — 排查 Add Hook 安装失败原因
// ═══════════════════════════════════════════════════════
public class DiagnosticTests
{
    [Test]
    public void Diagnose_AddHookStatus()
    {
        HookHelper.Instance = new MinHook();
        TestHooks.InstallHooks();

        // 直接调 Original —— 检查 _orig 是否非空
        var origResult = TestHooks.HookAddOriginal(2, 3);
        TestContext.WriteLine($"HookAddOriginal(2,3) = {origResult}");

        // 直接调 P/Invoke —— 看是否被 Hook 拦截
        var pinvokeResult = NativeFunc.Add(2, 3);
        TestContext.WriteLine($"NativeFunc.Add(2,3) = {pinvokeResult}");

        // 检查 GetMagicNumber（正常工作的对比）
        var magicOrig = TestHooks.HookGetMagicNumberOriginal();
        var magicPInvoke = NativeFunc.GetMagicNumber();
        TestContext.WriteLine($"GetMagicNumber: Original=0x{magicOrig:X} P/Invoke=0x{magicPInvoke:X}");

        
        TestHooks.UninstallHooks();
        HookHelper.Instance = null;
    }
}
