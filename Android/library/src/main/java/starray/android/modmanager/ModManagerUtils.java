package starray.android.modmanager;

import android.app.Activity;
import android.content.Context;
import android.os.Environment;
import android.os.Handler;
import android.os.Looper;
import android.text.Editable;
import android.text.InputType;
import android.text.TextWatcher;
import android.view.KeyEvent;
import android.view.View;
import android.util.Log;
import android.view.ViewGroup;
import android.view.inputmethod.InputConnection;
import android.view.inputmethod.InputMethodManager;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.BaseInputConnection;
import android.widget.Toast;
import android.widget.EditText;
import android.widget.FrameLayout;

import java.io.File;

public class ModManagerUtils {

    private static final Activity unityActivity = getUnityActivity();
    private static EditText sHiddenEditText;
    private static boolean sEditTextAdded;

    public static Activity getUnityActivity() {
        try {
            Class<?> clazz = Class.forName("com.unity3d.player.UnityPlayer");
            return (Activity) clazz.getField("currentActivity").get(null);
        } catch (Exception e) {
            Log.e("ModManagerUtils", "getUnityActivity", e);
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
            }
        });

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

    public static native void nativeSetData(String key, int[] data);

    public static native int[] nativeGetData(String key);

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


    private static KeyboardView keyboardView;

    public static void showKeyboard(boolean show) {
        if (unityActivity == null)
            return;

        new Handler(Looper.getMainLooper()).post(() -> {
            InputMethodManager imm = (InputMethodManager)
                    unityActivity.getSystemService(Context.INPUT_METHOD_SERVICE);

            if (imm == null)
                return;

            if (show) {
                if (keyboardView == null) {
                    keyboardView = new KeyboardView(unityActivity);
                    unityActivity.addContentView(keyboardView,
                            new ViewGroup.LayoutParams(1, 1));
                }

                final KeyboardView kv = keyboardView;
                kv.post(() -> {
                    kv.requestFocus();
                    imm.showSoftInput(kv, InputMethodManager.SHOW_FORCED);
                });
            } else {
                if (keyboardView != null) {
                    imm.hideSoftInputFromWindow(keyboardView.getWindowToken(), 0);
                    keyboardView.clearFocus();
                }
            }
        });
    }

    public static native void nativeSendChar(int unicode);
    public static native void nativeSendKey(int keyCode);

    static class KeyboardView extends View {

        public KeyboardView(Context ctx) {
            super(ctx);
            setFocusable(true);
            setFocusableInTouchMode(true);
        }

        @Override
        public boolean onCheckIsTextEditor() {
            return true;
        }

        @Override
        public InputConnection onCreateInputConnection(EditorInfo outAttrs) {
            outAttrs.inputType = android.text.InputType.TYPE_CLASS_TEXT |
                    android.text.InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS;
            outAttrs.imeOptions = EditorInfo.IME_ACTION_SEND |
                    EditorInfo.IME_FLAG_NO_EXTRACT_UI |
                    EditorInfo.IME_FLAG_NO_FULLSCREEN;
            outAttrs.initialSelStart = 0;
            outAttrs.initialSelEnd = 0;

            return new BaseInputConnection(this, false) {

                @Override
                public boolean commitText(CharSequence text, int newCursorPosition) {
                    for (int i = 0; i < text.length(); i++) {
                        char c = text.charAt(i);
                        if (Character.isHighSurrogate(c) && i + 1 < text.length()
                                && Character.isLowSurrogate(text.charAt(i + 1))) {
                            nativeSendChar(Character.toCodePoint(c, text.charAt(i + 1)));
                            i++;
                        } else {
                            nativeSendChar((int) c);
                        }
                    }
                    return true;
                }

                @Override
                public boolean deleteSurroundingText(int beforeLength, int afterLength) {
                    for (int i = 0; i < beforeLength; i++)
                        nativeSendKey(KeyEvent.KEYCODE_DEL);
                    for (int i = 0; i < afterLength; i++)
                        nativeSendKey(KeyEvent.KEYCODE_FORWARD_DEL);
                    return true;
                }

                @Override
                public boolean sendKeyEvent(KeyEvent event) {
                    if (event.getAction() == KeyEvent.ACTION_DOWN) {
                        int code = event.getKeyCode();
                        if (code == KeyEvent.KEYCODE_DEL ||
                                code == KeyEvent.KEYCODE_ENTER ||
                                code == KeyEvent.KEYCODE_DPAD_LEFT ||
                                code == KeyEvent.KEYCODE_DPAD_RIGHT) {
                            nativeSendKey(code);
                            return true;
                        }
                        int uni = event.getUnicodeChar(event.getMetaState());
                        if (uni != 0) {
                            nativeSendChar(uni);
                            return true;
                        }
                    }
                    return super.sendKeyEvent(event);
                }
            };
        }
    }
}
