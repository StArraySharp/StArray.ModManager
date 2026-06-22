package starray.android.modloader;

import android.app.Activity;
import android.text.Editable;
import android.text.InputType;
import android.text.TextWatcher;
import android.view.Gravity;
import android.view.ViewGroup;
import android.util.Log;
import android.view.inputmethod.InputMethodManager;
import android.view.inputmethod.EditorInfo;
import android.widget.EditText;
import android.widget.FrameLayout;

public class ModManagerUtils {

    private static final Activity unityActivity = getUnityActivity();
    private static EditText sHiddenEditText;
    private static boolean sEditTextAdded;

    private static Activity getUnityActivity() {
        try {
            Class<?> clazz = Class.forName("com.unity3d.player.UnityPlayer");
            return (Activity) clazz.getField("currentActivity").get(null);
        } catch (Exception e) {
            android.util.Log.e("ModManagerUtils", "getUnityActivity", e);
        }
        return null;
    }

    /** showSoftInput 前调用，把 ImGui 已有文本同步到 EditText */
    public static void setInputText(String text) {
        if (unityActivity == null) return;
        unityActivity.runOnUiThread(() -> {
            ensureHiddenEditText();
            String t = text != null ? text : "";
            sHiddenEditText.setText(t);
            sHiddenEditText.setSelection(t.length());
            Log.i("ModManagerUtils", "setInputText len=" + t.length());
        });
    }

    public static void showSoftInput() {
        if (unityActivity == null) return;
        unityActivity.runOnUiThread(() -> {
            ensureHiddenEditText();
            if (!sHiddenEditText.hasFocus()) {
                sHiddenEditText.requestFocus();
            }
            InputMethodManager imm = (InputMethodManager)
                unityActivity.getSystemService(android.content.Context.INPUT_METHOD_SERVICE);
            imm.showSoftInput(sHiddenEditText, 0);
            Log.i("ModManagerUtils", "showSoftInput focus=" + sHiddenEditText.hasFocus());
        });
    }

    public static void hideSoftInput() {
        if (unityActivity == null) return;
        // 先同步发送最终文本到 C（不在 UI 线程，确保 C# 能立即读到）

        unityActivity.runOnUiThread(() -> {
            if (sHiddenEditText == null) return;
            InputMethodManager imm = (InputMethodManager)
                unityActivity.getSystemService(android.content.Context.INPUT_METHOD_SERVICE);
            imm.hideSoftInputFromWindow(sHiddenEditText.getWindowToken(), 0);
            sHiddenEditText.setText("");
            Log.i("ModManagerUtils", "hideSoftInput");
        });
    }

    private static void ensureHiddenEditText() {
        if (sHiddenEditText != null) return;

        sHiddenEditText = new EditText(unityActivity);
        sHiddenEditText.setShowSoftInputOnFocus(true);
        sHiddenEditText.setInputType(InputType.TYPE_CLASS_TEXT);
        sHiddenEditText.setImeOptions(EditorInfo.IME_ACTION_DONE);
        // 隐藏控件 — 不占用屏幕空间，但仍可获取焦点接收输入法
        sHiddenEditText.setBackgroundColor(0x00000000);
        sHiddenEditText.setTextColor(0x00000000);
        sHiddenEditText.setCursorVisible(false);
        sHiddenEditText.setAlpha(0f);
        sHiddenEditText.setOnEditorActionListener((v, actionId, event) -> {
            if (actionId == EditorInfo.IME_ACTION_DONE) {
                sendTextToNative();
                hideSoftInput();
                return true;
            }
            return false;
        });
        sHiddenEditText.addTextChangedListener(new TextWatcher() {
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {}
            public void onTextChanged(CharSequence s, int start, int before, int count) {}
            public void afterTextChanged(Editable s) {
                // hide 时统一发送，不逐字同步
            }
        });

        // 加入到 UnityPlayer.getFrameLayout() → 1x1 透明无光标 → requestFocus
        try {
            Class<?> upClass = Class.forName("com.unity3d.player.UnityPlayer");
            Object unityPlayer = upClass.getField("currentActivity").get(null);
            java.lang.reflect.Field upField = unityPlayer.getClass().getSuperclass().getDeclaredField("mUnityPlayer");
            upField.setAccessible(true);
            Object player = upField.get(unityPlayer);
            java.lang.reflect.Method getFrameLayout = player.getClass().getMethod("getFrameLayout");
            FrameLayout frameLayout = (FrameLayout) getFrameLayout.invoke(player);

            FrameLayout.LayoutParams lp = new FrameLayout.LayoutParams(1, 1);
            lp.gravity = android.view.Gravity.BOTTOM | android.view.Gravity.LEFT;
            sHiddenEditText.setLayoutParams(lp);
            frameLayout.addView(sHiddenEditText);
            
            sHiddenEditText.requestFocus();
            sEditTextAdded = true;
            Log.i("ModManagerUtils", "EditText added to UnityPlayer.getFrameLayout(), focus=" + sHiddenEditText.hasFocus());
        } catch (Exception e) {
            Log.e("ModManagerUtils", "addView to frameLayout failed", e);
        }
    }

    /** 发送 int[] 到 C 层，按 key 路由 */
    public static native void nativeSetData(String key, int[] data);

    /** 从 C 层获取 int[]，按 key 路由 */
    public static native int[] nativeGetData(String key);

    /** 整量同步 — 把 EditText 当前文本转为 int[] 发送到 C (每字符 = 一个 int) */
    public static void sendTextToNative() {
        if (sHiddenEditText == null) {
            nativeSetData("ime_text", null);
            return;
        }
        String text = sHiddenEditText.getText().toString();
        int[] data = new int[text.length()];
        for (int i = 0; i < text.length(); i++)
            data[i] = text.charAt(i);
        Log.e("ModManagerUtils","send:" + text);
        nativeSetData("ime_text", data);
    }
}
