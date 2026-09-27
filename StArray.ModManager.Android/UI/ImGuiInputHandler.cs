using ImGuiNET;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Manager;
using StArray.ModManager.Native;

namespace StArray.ModManager.Android.UI;

/// <summary>ImGui input handler / 输入处理器 — Java bridge input + IME control</summary>
public static partial class ImGuiInputHandler
{
    /// <summary>ImGui 上下文就绪后由渲染器设置</summary>
    public static bool IsInitialized { get; set; }
    

    private static bool s_wantTextInputLast;
    private static int s_javaPrimaryPointerId = -1;

    /// <summary>
    /// 注册 Java Activity 输入桥接回调。宿主 APK 必须包含 Activity 转发补丁。
    /// </summary>
    public static void InstallInputHooks()
    {
        if (!IsInitialized) return;
        try
        {
            if (!AndroidJavaInputBridge.RegisterCallback())
                Logger.Error(nameof(ImGuiInputHandler),
                    "Could not register the Java Activity input bridge callback.");

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

    internal static void DispatchJavaInput(AndroidInputEventInfo input)
    {
        if (!IsInitialized)
            return;

        var io = ImGui.GetIO();
        if (input.Kind == AndroidInputEventKind.Key)
        {
            DispatchJavaKeyEvent(io, input);
            return;
        }

        ReadOnlySpan<AndroidInputPointerInfo> pointers = input.Pointers.Span;
        if (input.Kind != AndroidInputEventKind.Motion || pointers.Length == 0)
            return;

        int pointerCount = pointers.Length;
        int actionIndex = Math.Clamp(input.ActionIndex, 0, pointerCount - 1);
        int selectedIndex = actionIndex;
        if (s_javaPrimaryPointerId >= 0)
        {
            for (int index = 0; index < pointerCount; index++)
            {
                if (pointers[index].Id == s_javaPrimaryPointerId)
                {
                    selectedIndex = index;
                    break;
                }
            }
        }

        AndroidInputPointerInfo pointer = pointers[selectedIndex];
        float scaleX = input.ViewportWidth > 0
            ? io.DisplaySize.X / input.ViewportWidth
            : 1.0f;
        float scaleY = input.ViewportHeight > 0
            ? io.DisplaySize.Y / input.ViewportHeight
            : 1.0f;
        io.AddMousePosEvent(pointer.X * scaleX, pointer.Y * scaleY);

        if (!input.IsGenericMotion)
        {
            DispatchJavaTouchAction(io, input, pointers, pointerCount, actionIndex);
            return;
        }

        UpdateJavaMouseButtons(io, input.ButtonState);
        if (input.HorizontalScroll != 0.0f || input.VerticalScroll != 0.0f)
            io.AddMouseWheelEvent(input.HorizontalScroll, input.VerticalScroll);
    }

    private static void DispatchJavaTouchAction(
        ImGuiIOPtr io,
        AndroidInputEventInfo input,
        ReadOnlySpan<AndroidInputPointerInfo> pointers,
        int pointerCount,
        int actionIndex)
    {
        AndroidInput.MotionAction action = (AndroidInput.MotionAction)input.Action;
        int actionPointerId = pointers[actionIndex].Id;
        switch (action)
        {
            case AndroidInput.MotionAction.Down:
            case AndroidInput.MotionAction.PointerDown:
                if (s_javaPrimaryPointerId < 0 || action == AndroidInput.MotionAction.Down)
                    s_javaPrimaryPointerId = actionPointerId;
                if (actionPointerId == s_javaPrimaryPointerId)
                    io.AddMouseButtonEvent(0, true);
                break;

            case AndroidInput.MotionAction.PointerUp:
                if (actionPointerId == s_javaPrimaryPointerId)
                {
                    int replacementIndex = -1;
                    for (int index = 0; index < pointerCount; index++)
                    {
                        if (index != actionIndex)
                        {
                            replacementIndex = index;
                            break;
                        }
                    }

                    if (replacementIndex >= 0)
                    {
                        s_javaPrimaryPointerId = pointers[replacementIndex].Id;
                        float scaleX = input.ViewportWidth > 0
                            ? io.DisplaySize.X / input.ViewportWidth
                            : 1.0f;
                        float scaleY = input.ViewportHeight > 0
                            ? io.DisplaySize.Y / input.ViewportHeight
                            : 1.0f;
                        io.AddMousePosEvent(
                            pointers[replacementIndex].X * scaleX,
                            pointers[replacementIndex].Y * scaleY);
                    }
                    else
                    {
                        s_javaPrimaryPointerId = -1;
                        io.AddMouseButtonEvent(0, false);
                    }
                }
                break;

            case AndroidInput.MotionAction.Up:
            case AndroidInput.MotionAction.Cancel:
                s_javaPrimaryPointerId = -1;
                io.AddMouseButtonEvent(0, false);
                break;
        }
    }

    private static void DispatchJavaKeyEvent(
        ImGuiIOPtr io,
        AndroidInputEventInfo input)
    {
        const int KeyActionDown = 0;
        const int KeyActionUp = 1;
        bool isDown = input.Action == KeyActionDown;
        if (input.Action is KeyActionDown or KeyActionUp)
        {
            ImGuiKey key = MapAndroidKeyCode(input.KeyCode);
            if (key != ImGuiKey.None)
                io.AddKeyEvent(key, isDown);
        }

        int meta = input.MetaState;
        io.AddKeyEvent((ImGuiKey)4096, (meta & 0x1000) != 0);  // Ctrl
        io.AddKeyEvent((ImGuiKey)8192, (meta & 0x0001) != 0);  // Shift
        io.AddKeyEvent((ImGuiKey)16384, (meta & 0x0002) != 0); // Alt
        io.AddKeyEvent((ImGuiKey)32768, (meta & 0x10000) != 0); // Meta/Super

        if (isDown && !s_wantTextInputLast && input.UnicodeCodePoint > 0
            && !char.IsControl((char)input.UnicodeCodePoint))
        {
            io.AddInputCharacter((uint)input.UnicodeCodePoint);
        }
    }

    private static ImGuiKey MapAndroidKeyCode(int keyCode)
    {
        if (keyCode is >= 29 and <= 54)
            return (ImGuiKey)(546 + keyCode - 29); // A-Z
        if (keyCode is >= 7 and <= 16)
            return (ImGuiKey)(536 + keyCode - 7); // 0-9
        if (keyCode is >= 131 and <= 142)
            return (ImGuiKey)(572 + keyCode - 131); // F1-F12
        if (keyCode is >= 144 and <= 153)
            return (ImGuiKey)(612 + keyCode - 144); // Keypad 0-9

        return keyCode switch
        {
            61 => (ImGuiKey)512,  // Tab
            21 => (ImGuiKey)513,  // Left
            22 => (ImGuiKey)514,  // Right
            19 => (ImGuiKey)515,  // Up
            20 => (ImGuiKey)516,  // Down
            92 => (ImGuiKey)517,  // PageUp
            93 => (ImGuiKey)518,  // PageDown
            122 => (ImGuiKey)519, // Home
            123 => (ImGuiKey)520, // End
            124 => (ImGuiKey)521, // Insert
            112 => ImGuiKey.Delete,
            67 => ImGuiKey.Backspace,
            62 => (ImGuiKey)524,  // Space
            66 => ImGuiKey.Enter,
            111 => (ImGuiKey)526, // Escape
            113 => (ImGuiKey)527, // Left Ctrl
            59 => (ImGuiKey)528,  // Left Shift
            57 => (ImGuiKey)529,  // Left Alt
            117 => (ImGuiKey)530, // Left Meta
            114 => (ImGuiKey)531, // Right Ctrl
            60 => (ImGuiKey)532,  // Right Shift
            58 => (ImGuiKey)533,  // Right Alt
            118 => (ImGuiKey)534, // Right Meta
            82 => (ImGuiKey)535,  // Menu
            55 => (ImGuiKey)597,  // Comma
            69 => (ImGuiKey)598,  // Minus
            56 => (ImGuiKey)599,  // Period
            76 => (ImGuiKey)600,  // Slash
            74 => (ImGuiKey)601,  // Semicolon
            70 => (ImGuiKey)602,  // Equal
            71 => (ImGuiKey)603,  // Left bracket
            73 => (ImGuiKey)604,  // Backslash
            72 => (ImGuiKey)605,  // Right bracket
            68 => (ImGuiKey)606,  // Grave
            115 => (ImGuiKey)607, // Caps lock
            116 => (ImGuiKey)608, // Scroll lock
            143 => (ImGuiKey)609, // Num lock
            120 => (ImGuiKey)610, // Print screen
            121 => (ImGuiKey)611, // Pause
            154 => (ImGuiKey)623, // Keypad divide
            155 => (ImGuiKey)624, // Keypad multiply
            156 => (ImGuiKey)625, // Keypad subtract
            157 => (ImGuiKey)626, // Keypad add
            160 => (ImGuiKey)627, // Keypad enter
            161 => (ImGuiKey)628, // Keypad equal
            4 => (ImGuiKey)629,   // App back
            125 => (ImGuiKey)630, // App forward
            75 => (ImGuiKey)596,  // Apostrophe
            158 => (ImGuiKey)622, // Keypad decimal
            _ => ImGuiKey.None,
        };
    }

    private static void UpdateJavaMouseButtons(ImGuiIOPtr io, int buttonState)
    {
        io.AddMouseButtonEvent(0, (buttonState & 0x01) != 0); // primary
        io.AddMouseButtonEvent(1, (buttonState & 0x02) != 0); // secondary
        io.AddMouseButtonEvent(2, (buttonState & 0x04) != 0); // tertiary
        io.AddMouseButtonEvent(3, (buttonState & 0x08) != 0); // back
        io.AddMouseButtonEvent(4, (buttonState & 0x10) != 0); // forward
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
