using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using ImGuiNET;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.UI;

public enum AndroidImeOwner
{
    None,
    ModManager,
    UnitySettings
}

/// <summary>ImGui input handler / 输入处理器 — touch/key hooks + IME control</summary>
public static class ImGuiInputHandler
{
    /// <summary>ImGui 上下文就绪后由渲染器设置</summary>
    public static bool IsInitialized { get; set; }

    private static InitializeMotionEventDelegate? s_initializeMotionEvent;

    private delegate int InitializeMotionEventDelegate(IntPtr self, IntPtr motionEvent, IntPtr message);

    private static bool s_hooksInstalled;
    private static bool s_imeCallbacksRegistered;
    private static readonly object ImeGate = new();
    private static bool s_wantTextInputLast;
    private static bool s_pendingWantTextInput;
    private static AndroidImeOwner s_imeOwner;
    private static bool s_modManagerObservedReleasedFocus;
    private static long s_pendingWantTextInputSinceMs;
    private static long s_lastImeDispatchMs;
    private static int s_touchLogBudget = 4;
    private static int s_imeDiagnosticBudget = 16;
    private const long ImeStableDelayMs = 80;
    private const long ImeDispatchMinIntervalMs = 200;

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_ui_is_visible")]
    private static extern int OverlayUiIsVisibleNative();

    public static void RegisterImeCallbacks()
    {
        if (s_imeCallbacksRegistered)
            return;
        s_imeCallbacksRegistered = true;

        // IME 字符回调：Java nativeSendChar → C → 此回调 → ImGui
        NativeFunctions.SetOnAcceptCharCallback(codepoint =>
        {
            try
            {
                if (!IsInitialized) return;
                ImGui.GetIO().AddInputCharacter(codepoint);
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(ImGuiInputHandler), $"AcceptChar failed: {ex}");
            }
        });

        // IME 特殊键回调：Java nativeSendKey → C → 此回调 → ImGui
        NativeFunctions.SetOnAcceptKeyCallback(keyCode =>
        {
            try
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
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(ImGuiInputHandler), $"AcceptKey failed: {ex}");
            }
        });
    }

    /// <summary>
    /// 安装触摸事件和按键事件 Hook
    /// </summary>
    public static void InstallHooks()
    {
        if (s_hooksInstalled)
            return;

        // —— 触摸事件 Hook ——
        string consumerSymbol = "_ZN7android13InputConsumer21initializeMotionEventEPNS_11MotionEventEPKNS_12InputMessageE";
        IntPtr consumerAddr = Dobby.SymbolResolver("libinput.so", consumerSymbol);
        if (consumerAddr == IntPtr.Zero)
        {
            Logger.Warn(nameof(ImGuiInputHandler), "libinput initializeMotionEvent symbol not found");
            return;
        }

        int hookResult = Dobby.Hook(consumerAddr,
            typeof(ImGuiInputHandler).GetMethod(nameof(OnTouchEvent))!.MethodHandle.GetFunctionPointer(),
            out var origin);
        if (hookResult != 0 || origin == IntPtr.Zero)
        {
            Logger.Warn(nameof(ImGuiInputHandler), $"Input hook failed: {hookResult}");
            return;
        }
        s_initializeMotionEvent = Marshal.GetDelegateForFunctionPointer<InitializeMotionEventDelegate>(origin);
        s_hooksInstalled = true;

        Logger.Info(nameof(ImGuiInputHandler), "Input hooks installed");
    }

    /// <summary>触摸事件 Hook 回调</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnTouchEvent(IntPtr self, IntPtr motionEvent, IntPtr message)
    {
        var original = s_initializeMotionEvent;
        int result = 0;

        try
        {
            if (original != null)
                result = original(self, motionEvent, message);
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiInputHandler), $"initializeMotionEvent original failed: {ex}");
        }

        try
        {
            if (IsInitialized && motionEvent != IntPtr.Zero)
            {
                int handled = ImGuiImplAndroid.HandleInputEvent(motionEvent);
                if (s_touchLogBudget > 0)
                {
                    s_touchLogBudget--;
                    Logger.Info(nameof(ImGuiInputHandler),
                        $"Touch forwarded handled={handled} motionEvent=0x{motionEvent:X}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiInputHandler), $"HandleInputEvent failed: {ex}");
        }

        return result;
    }

    private static JavaClass? s_utilsClass;
    private static nint s_showKeyboardMethod;

    public static void SetImeOwner(AndroidImeOwner owner)
    {
        lock (ImeGate)
        {
            if (s_imeOwner == owner)
                return;
            s_imeOwner = owner;
            s_modManagerObservedReleasedFocus = false;
            s_pendingWantTextInput = false;
            s_pendingWantTextInputSinceMs = Environment.TickCount64;
            if (owner != AndroidImeOwner.ModManager)
                DispatchKeyboardLocked(false, force: true);
            else
                s_wantTextInputLast = false;
        }
    }

    /// <summary>根据 ImGui 文本输入状态切换软键盘</summary>
    public static void UpdateIme()
    {
        lock (ImeGate)
            UpdateImeLocked();
    }

    private static void UpdateImeLocked()
    {
        try
        {
            if (!IsInitialized) return;
            bool overlayVisible;
            try
            {
                overlayVisible = OverlayUiIsVisibleNative() != 0;
            }
            catch
            {
                overlayVisible = true;
            }
            bool dearImGuiWant = ImGui.GetIO().WantTextInput;
            if (s_imeOwner == AndroidImeOwner.ModManager && !dearImGuiWant)
                s_modManagerObservedReleasedFocus = true;
            bool want = overlayVisible && dearImGuiWant &&
                        s_imeOwner == AndroidImeOwner.ModManager &&
                        s_modManagerObservedReleasedFocus;
            long now = Environment.TickCount64;

            if (want != s_pendingWantTextInput)
            {
                s_pendingWantTextInput = want;
                s_pendingWantTextInputSinceMs = now;
            }

            if (s_pendingWantTextInput == s_wantTextInputLast)
                return;
            if (now - s_pendingWantTextInputSinceMs < ImeStableDelayMs)
                return;
            if (now - s_lastImeDispatchMs < ImeDispatchMinIntervalMs)
                return;

            bool next = s_pendingWantTextInput;
            if (s_imeDiagnosticBudget > 0)
            {
                s_imeDiagnosticBudget--;
                var loadContext = AssemblyLoadContext.GetLoadContext(typeof(ImGuiInputHandler).Assembly);
                Logger.Info(
                    "PcCompatSettingsDiag",
                    "[DEBUG-settings-surface-v1] ime source=DearImGui " +
                    $"owner={s_imeOwner} released={s_modManagerObservedReleasedFocus} " +
                    $"overlayVisible={overlayVisible} ioWant={dearImGuiWant} " +
                    $"want={want} pending={s_pendingWantTextInput} last={s_wantTextInputLast} " +
                    $"next={next} stableMs={now - s_pendingWantTextInputSinceMs} " +
                    $"dispatchAgeMs={now - s_lastImeDispatchMs} tid={Environment.CurrentManagedThreadId} " +
                    $"alc={loadContext?.Name ?? "default"} mvid={typeof(ImGuiInputHandler).Assembly.ManifestModule.ModuleVersionId}");
            }
            DispatchKeyboardLocked(next, force: false);
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiInputHandler), $"UpdateIme failed: {ex}");
        }
    }

    private static void DispatchKeyboardLocked(bool show, bool force)
    {
        if (!force && show == s_wantTextInputLast)
            return;
        s_utilsClass ??= new JavaClass(
            "com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
        if (s_showKeyboardMethod == 0)
            s_showKeyboardMethod = s_utilsClass.GetStaticMethodID("showKeyboard", "(Z)V");
        if (s_showKeyboardMethod == 0)
        {
            Logger.Warn(nameof(ImGuiInputHandler), "showKeyboard method not found");
            return;
        }
        s_utilsClass.CallStaticVoidMethod1(s_showKeyboardMethod, show);
        s_wantTextInputLast = show;
        s_lastImeDispatchMs = Environment.TickCount64;
        Logger.Info(nameof(ImGuiInputHandler), $"IME owner={s_imeOwner} {(show ? "Show" : "Hide")}");
    }
}
