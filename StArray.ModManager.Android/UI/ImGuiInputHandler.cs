using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.UI;

/// <summary>ImGui input handler / 输入处理器 — touch/key hooks + IME control</summary>
public static partial class ImGuiInputHandler
{
    /// <summary>ImGui 上下文就绪后由渲染器设置</summary>
    public static bool IsInitialized { get; set; }
    

    private static bool s_wantTextInputLast;

    /// <summary>
    /// 安装触摸事件和按键事件 Hook
    /// </summary>
    public static void InstallInputHooks()
    {
        InstallHooks();
        // IME 字符回调：Java nativeSendChar → C → 此回调 → ImGui
        NativeFunctions.SetOnAcceptCharCallback(codepoint =>
        {
            if (!IsInitialized) return;
            ImGui.GetIO().AddInputCharacter(codepoint);
        });

        // IME 特殊键回调：Java nativeSendKey → C → 此回调 → ImGui
        NativeFunctions.SetOnAcceptKeyCallback(keyCode =>
        {
            if (!IsInitialized) return;
            var io = ImGui.GetIO();
            switch (keyCode)
            {
                case 67:  io.AddKeyEvent(ImGuiKey.Backspace, true);  io.AddKeyEvent(ImGuiKey.Backspace, false); break;   // KEYCODE_DEL
                case 112: io.AddKeyEvent(ImGuiKey.Delete, true);     io.AddKeyEvent(ImGuiKey.Delete, false);    break;   // KEYCODE_FORWARD_DEL
                case 66:  io.AddKeyEvent(ImGuiKey.Enter, true);      io.AddKeyEvent(ImGuiKey.Enter, false);     break;   // KEYCODE_ENTER
                case 21:  io.AddKeyEvent(ImGuiKey.LeftArrow, true);  io.AddKeyEvent(ImGuiKey.LeftArrow, false); break;   // KEYCODE_DPAD_LEFT
                case 22:  io.AddKeyEvent(ImGuiKey.RightArrow, true); io.AddKeyEvent(ImGuiKey.RightArrow, false); break;  // KEYCODE_DPAD_RIGHT
            }
        });
    }

    /// <summary>触摸事件 Hook 回调</summary>
    [NativeHook("libinput.so","_ZN7android13InputConsumer14consumeSamplesEPNS_26InputEventFactoryInterfaceERNS0_5BatchEmPjPPNS_10InputEventE")]
    public unsafe static long OnConsumeSamples(void* thiz,void* factory, IntPtr batch,
        ulong count, uint* outSeq, void** outEvent)
    {
        var result = OnConsumeSamplesOriginal(thiz,factory, batch, count, outSeq, outEvent);
        if (IsInitialized && *outEvent != null) ImGuiImplAndroid.HandleInputEvent(new IntPtr(*outEvent));
        return result;
    }
    
    [NativeHook("libinput.so","_ZN7android13InputConsumer7consumeEPNS_26InputEventFactoryInterfaceEblPjPPNS_10InputEventE")]
    public unsafe static long OnConsume(void* thiz, void* factory, bool consumeBatches, ulong frameTime, uint* outSeq, void** outEvent)
    {
        var result = OnConsumeOriginal(thiz, factory, consumeBatches, frameTime, outSeq, outEvent);
        if (IsInitialized && *outEvent != null) ImGuiImplAndroid.HandleInputEvent(new IntPtr(*outEvent));
        return result;
    }

    private static JavaClass? s_utilsClass;
    private static nint s_showKeyboardMethod;

    /// <summary>根据 ImGui 文本输入状态切换软键盘</summary>
    public static void UpdateIme()
    {
        if (!IsInitialized) return;
        bool want = ImGui.GetIO().WantTextInput;
        if (want == s_wantTextInputLast) return;
        s_wantTextInputLast = want;

        // 懒加载缓存 Java 类引用
        if (s_utilsClass == null)
        {
            s_utilsClass = new JavaClass("starray.android.modmanager.ModManagerUtils");
            s_showKeyboardMethod = s_utilsClass.GetStaticMethodID("showKeyboard", "(Z)V");
        }

        s_utilsClass.CallStaticVoidMethod1(s_showKeyboardMethod, want ? 1 : 0);
        Logger.Info(nameof(ImGuiInputHandler), $"IME {(want ? "Show" : "Hide")}");
    }
}
