using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;

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
        if (!IsInitialized) return;
        try
        {
            InstallHooks();
            // IME 字符回调：Java nativeSendChar → C → 此回调 → ImGui
            NativeFunctions.SetOnAcceptCharCallback(codepoint =>
            {
                ImGui.GetIO().AddInputCharacter(codepoint);
            });

            // IME 特殊键回调：Java nativeSendKey → C → 此回调 → ImGui
            NativeFunctions.SetOnAcceptKeyCallback(keyCode =>
            {
                var io = ImGui.GetIO();
                switch (keyCode)
                {
                    case 67:
                        io.AddKeyEvent(ImGuiKey.Backspace, true);
                        io.AddKeyEvent(ImGuiKey.Backspace, false);
                        break; // KEYCODE_DEL
                    case 112:
                        io.AddKeyEvent(ImGuiKey.Delete, true);
                        io.AddKeyEvent(ImGuiKey.Delete, false);
                        break; // KEYCODE_FORWARD_DEL
                    case 66:
                        io.AddKeyEvent(ImGuiKey.Enter, true);
                        io.AddKeyEvent(ImGuiKey.Enter, false);
                        break; // KEYCODE_ENTER
                    case 21:
                        io.AddKeyEvent(ImGuiKey.LeftArrow, true);
                        io.AddKeyEvent(ImGuiKey.LeftArrow, false);
                        break; // KEYCODE_DPAD_LEFT
                    case 22:
                        io.AddKeyEvent(ImGuiKey.RightArrow, true);
                        io.AddKeyEvent(ImGuiKey.RightArrow, false);
                        break; // KEYCODE_DPAD_RIGHT
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiInputHandler), ex.ToString());
        }
        IsInitialized = true;
    }

    /*
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
    }*/
    
    [NativeHook("GetInitializeMotionEventAddress")]
    public unsafe static bool OnInitializeMotionEvent(void* @event, void* message)
    {
        var result = OnInitializeMotionEventOriginal(@event, message);
        var x = AndroidInput.AMotionEvent_getX(new(@event), 0);
        var y = AndroidInput.AMotionEvent_getY(new(@event), 0);
        ImGuiImplAndroid.HandleInputEvent(new IntPtr(@event));
        return result;
    }

    private static nint GetInitializeMotionEventAddress()
    {
        NativeFuncResolver resolver = new("/system/lib64/libinput.so");
        string sigHex = "e8 0f 19 fc fd 7b 01 a9 fc 6f 02 a9 fa 67 03 a9 " +
                        "f8 5f 04 a9 f6 57 05 a9 f4 4f 06 a9 " +
                        "fd 43 00 91 ?? ?? ?? ?? " +           // add x29, sp + sub sp (栈帧大小可变)
                        "58 d0 3b d5 " +                       // mrs x24, tpidr_el0
                        "?? ?? ?? ?? " +                       // ldr x8, [x24, #off]
                        "?? ?? ?? ?? " +                       // stur x8, [x29, #off]
                        "39 0c 40 b9 " +                       // ldr w25, [x1, #0xc]
                        "?? ?? ?? ?? " +                       // cbz w25
                        "37 f3 7d d3";                         // lsl x23, x25, #3

        var addr = resolver.Resolve("_ZN7android13InputConsumer21initializeMotionEventEPNS_11MotionEventEPKNS_12InputMessageE",
            NativeFuncResolver.ParseHexPattern(sigHex));
        Logger.Error(nameof(ImGuiInputHandler), $"GetInitializeMotionEventAddress: {addr}");
        return addr;
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
