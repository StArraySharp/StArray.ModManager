using StArray.ModManager.Java;
using StArray.ModManager.PInvoke;

namespace StArray.ModManager.Manager;

/// <summary>
/// Android IME — 整量同步方案
/// Java getInputTextChars() → C jchar buffer → C# Marshal.Copy → string
/// </summary>
public static class AndroidIME
{
    private static bool _init;
    private static nint _showMethod, _hideMethod, _setTextMethod;
    private static string _lastText = "";

    public static bool IsVisible { get; private set; }

    static AndroidIME()
    {
        try
        {
            var utils = new JavaClass("starray.android.modmanager.ModManagerUtils");
            _showMethod    = utils.GetStaticMethodID("showSoftInput", "()V");
            _hideMethod    = utils.GetStaticMethodID("hideSoftInput", "()V");
            _setTextMethod = utils.GetStaticMethodID("setInputText", "(Ljava/lang/String;)V");
            _init = _showMethod != 0 && _hideMethod != 0 && _setTextMethod != 0;
            Log(_init ? "IME ready" : "IME init failed");
        }
        catch (Exception ex) { LogErr($"Init: {ex}"); }
    }

    /// <summary>将 ImGui 现有文本同步到 EditText，Show 之前调用</summary>
    public static void SetText(string text)
    {
        if (!_init) return;
        _lastText = text ?? "";
        var utils = new JavaClass("starray.android.modmanager.ModManagerUtils");
        var jstr = JniHelperNative.NewString(text ?? "");
        utils.CallStaticVoidMethod1(_setTextMethod, jstr);
        JniHelperNative.DeleteLocalRef(jstr);
        utils.Dispose();
    }

    public static void Show()
    {
        if (!_init) return;
        Log("Show");
        var utils = new JavaClass("starray.android.modmanager.ModManagerUtils");
        utils.CallStaticVoidMethod0(_showMethod);
        utils.Dispose();
        IsVisible = true;
    }

    public static void Hide()
    {
        if (!_init || !IsVisible) return;
        Log("Hide");
        IsVisible = false;
        var utils = new JavaClass("starray.android.modmanager.ModManagerUtils");
        utils.CallStaticVoidMethod0(_hideMethod);
        utils.Dispose();
    }

    private static void Log(string msg) => AndroidLog.Info(nameof(AndroidIME), msg);
    private static void LogErr(string msg) => AndroidLog.Error(nameof(AndroidIME), $"Failed: {msg}");
}
