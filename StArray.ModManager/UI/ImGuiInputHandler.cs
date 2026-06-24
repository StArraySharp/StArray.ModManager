using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Java;
using StArray.ModManager.PInvoke;

namespace StArray.ModManager.UI;

/// <summary>
/// ImGui 输入处理器 —— 管理触摸事件、按键事件 Hook 以及 IME 控制
/// </summary>
public static unsafe class ImGuiInputHandler
{
    // ===== P/Invoke delegates =====

    private static InitializeMotionEventDelegate s_initializeMotionEvent;

    private delegate int InitializeMotionEventDelegate(IntPtr self, IntPtr motionEvent, IntPtr message);

    // ===== IME 状态 =====
    private static bool s_wantTextInputLast = false;

    /// <summary>
    /// 安装触摸事件和按键事件 Hook
    /// </summary>
    public static void InstallHooks()
    {
        // —— 触摸事件 Hook ——
        string consumerSymbol = "_ZN7android13InputConsumer21initializeMotionEventEPNS_11MotionEventEPKNS_12InputMessageE";
        IntPtr consumerAddr = Dobby.SymbolResolver("libinput.so", consumerSymbol);
        Dobby.Hook(consumerAddr,
            typeof(ImGuiInputHandler).GetMethod(nameof(OnTouchEvent))!.MethodHandle.GetFunctionPointer(),
            out var origin);
        s_initializeMotionEvent = Marshal.GetDelegateForFunctionPointer<InitializeMotionEventDelegate>(origin);

        // IME 字符回调：Java nativeSendChar → C → 此回调 → ImGui
        Misc.SetOnAcceptCharCallback(codepoint =>
        {
            ImGuiNET.ImGui.GetIO().AddInputCharacter(codepoint);
        });

        // IME 特殊键回调：Java nativeSendKey → C → 此回调 → ImGui
        Misc.SetOnAcceptKeyCallback(keyCode =>
        {
            var io = ImGuiNET.ImGui.GetIO();
            switch (keyCode)
            {
                case 67:  io.AddKeyEvent(ImGuiKey.Backspace, true);  io.AddKeyEvent(ImGuiKey.Backspace, false); break;   // KEYCODE_DEL
                case 112: io.AddKeyEvent(ImGuiKey.Delete, true);     io.AddKeyEvent(ImGuiKey.Delete, false);    break;   // KEYCODE_FORWARD_DEL
                case 66:  io.AddKeyEvent(ImGuiKey.Enter, true);      io.AddKeyEvent(ImGuiKey.Enter, false);     break;   // KEYCODE_ENTER
                case 21:  io.AddKeyEvent(ImGuiKey.LeftArrow, true);  io.AddKeyEvent(ImGuiKey.LeftArrow, false); break;   // KEYCODE_DPAD_LEFT
                case 22:  io.AddKeyEvent(ImGuiKey.RightArrow, true); io.AddKeyEvent(ImGuiKey.RightArrow, false); break;  // KEYCODE_DPAD_RIGHT
            }
        });

        AndroidLog.Info(nameof(ImGuiInputHandler), "Input hooks installed");
    }

    // ===== Hook 回调（UnmanagedCallersOnly） =====

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnTouchEvent(IntPtr self, IntPtr motionEvent, IntPtr message)
    {
        // 先调用原函数初始化 MotionEvent，再传给 ImGui
        int result = s_initializeMotionEvent(self, motionEvent, message);
        ImGuiImplAndroid.HandleInputEvent(self);
        return result;
    }

    // ===== IME 控制（WantTextInput 边沿触发） =====

    private static JavaClass? s_utilsClass;
    private static nint s_showKeyboardMethod;

    public static void UpdateIme()
    {
        bool want = ImGuiNET.ImGui.GetIO().WantTextInput;
        if (want == s_wantTextInputLast) return;
        s_wantTextInputLast = want;

        // 懒加载缓存 Java 类引用
        if (s_utilsClass == null)
        {
            s_utilsClass = new JavaClass("starray.android.modmanager.ModManagerUtils");
            s_showKeyboardMethod = s_utilsClass.GetStaticMethodID("showKeyboard", "(Z)V");
        }

        s_utilsClass.CallStaticVoidMethod1(s_showKeyboardMethod, want ? 1 : 0);
        AndroidLog.Info(nameof(ImGuiInputHandler), $"IME {(want ? "Show" : "Hide")}");
    }

}
