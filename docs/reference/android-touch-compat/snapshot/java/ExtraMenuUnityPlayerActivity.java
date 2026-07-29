package com.fizzd.connectedworlds.editorport;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.util.Log;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.View;
import android.widget.Toast;
import java.io.File;
import java.io.IOException;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.HashSet;

public class ExtraMenuUnityPlayerActivity extends com.unity3d.player.UnityPlayerActivity {
    private static final String TAG = "ADOFAI_EXTRA_MENU";
    private static boolean asyncInputLoaded;
    private static ExtraMenuUnityPlayerActivity currentActivity;
    private static volatile boolean stArrayModManagerInputForwarding;
    private static boolean stArrayForwardMotionChecked;
    private static boolean stArrayForwardMotionErrorLogged;
    private static Method stArrayForwardMotionEvent;
    private static boolean stArrayModalInputChecked;
    private static boolean stArrayModalInputErrorLogged;
    private static Method stArrayIsModalInputCaptureActive;
    private static Method stArrayRequestModalClose;
    private static boolean stArrayObserveMotionChecked;
    private static boolean stArrayObserveMotionErrorLogged;
    private static Method stArrayObserveMotionEvent;
    private static boolean stArrayObserveKeyChecked;
    private static boolean stArrayObserveKeyErrorLogged;
    private static Method stArrayObserveKeyEvent;
    private static boolean stArrayInputGateLastModal;
    private static boolean stArrayInputGateHasLastModal;
    private static long stArrayInputGateLastMotionLogUptime;
    private static final long STARRAY_INPUT_GATE_MOTION_LOG_INTERVAL_MS = 500L;
    private static boolean stArrayActivityResultChecked;
    private static boolean stArrayActivityResultErrorLogged;
    private static Method stArrayHandleActivityResult;

    static {
        loadExtraMenuLibrary();
        try {
            System.loadLibrary("AsyncInput");
            asyncInputLoaded = true;
            Log.i(TAG, "loaded libAsyncInput.so");
        } catch (Throwable t) {
            asyncInputLoaded = false;
            Log.e(TAG, "failed to load libAsyncInput.so", t);
        }
        try {
            System.loadLibrary("Editor_Pausemenu");
            Log.i(TAG, "loaded libEditor_Pausemenu.so");
        } catch (Throwable t) {
            Log.e(TAG, "failed to load libEditor_Pausemenu.so", t);
        }
    }

    private static void loadExtraMenuLibrary() {
        if (isExtraMenuLibraryAlreadyLoaded()) {
            Log.i(TAG, "reused libadofai_extra_menu.so");
            return;
        }
        try {
            System.loadLibrary("adofai_extra_menu");
            Log.i(TAG, "loaded libadofai_extra_menu.so");
        } catch (Throwable t) {
            Log.e(TAG, "failed to load libadofai_extra_menu.so", t);
        }
    }

    private static boolean isExtraMenuLibraryAlreadyLoaded() {
        try {
            Class<?> verifier = Class.forName("com.fizzd.connectedworlds.editorport.AppLicenseVerifier");
            Object value = verifier.getMethod("ensureNativeLoaded", Context.class)
                    .invoke(null, new Object[] {null});
            return Boolean.TRUE.equals(value);
        } catch (ClassNotFoundException ignored) {
            return false;
        } catch (Throwable t) {
            Log.w(TAG, "could not query license native loader", t);
            return false;
        }
    }

    private static boolean requestStArrayModManager(Activity activity, boolean showOverlay) {
        if (!hasModManagerLicenseCapability()) {
            stArrayModManagerInputForwarding = false;
            if (showOverlay && activity != null) {
                Toast.makeText(activity, "当前授权不包含 ModManager", Toast.LENGTH_SHORT).show();
            }
            Log.i(TAG, "StArray ModManager request blocked by license capability");
            return false;
        }
        try {
            Class<?> bootstrap = Class.forName(
                    "com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
            bootstrap.getMethod("setUiEnabled", boolean.class).invoke(null, true);
            bootstrap.getMethod("setInputHooksEnabled", boolean.class).invoke(null, false);
            stArrayModManagerInputForwarding = showOverlay;
            String methodName = showOverlay ? "launch" : "startInBackground";
            bootstrap.getMethod(methodName, Activity.class).invoke(null, activity);
            Log.i(TAG, "StArray ModManager request showOverlay=" + showOverlay);
            return true;
        } catch (ClassNotFoundException ignored) {
            stArrayModManagerInputForwarding = false;
            Log.i(TAG, "StArray ModManager bootstrap unavailable");
            return false;
        } catch (Throwable t) {
            stArrayModManagerInputForwarding = false;
            Log.w(TAG, "StArray ModManager bootstrap launch failed", t);
            return false;
        }
    }

    private static boolean hasModManagerLicenseCapability() {
        try {
            Class<?> verifier = Class.forName(
                    "com.fizzd.connectedworlds.editorport.AppLicenseVerifier");
            Object value = verifier.getMethod("hasModManagerLicenseCapability").invoke(null);
            return Boolean.TRUE.equals(value);
        } catch (ClassNotFoundException missingInNormalBuild) {
            return true;
        } catch (Throwable t) {
            Log.w(TAG, "ModManager capability query failed", t);
            return false;
        }
    }

    private static void launchStArrayModManager(Activity activity) {
        requestStArrayModManager(activity, true);
    }

    private static void startStArrayModManagerInBackground(Activity activity) {
        requestStArrayModManager(activity, false);
    }

    public static boolean startStArrayModManagerInBackgroundFromNative() {
        try {
            final Activity activity = getUnityActivity();
            if (activity == null) {
                return false;
            }
            return requestStArrayModManager(activity, false);
        } catch (Throwable t) {
            Log.e(TAG, "StArray ModManager background start entry failed", t);
            return false;
        }
    }

    public static void openStArrayModManagerFromNative() {
        try {
            final Activity activity = getUnityActivity();
            if (activity == null) {
                Log.w(TAG, "StArray ModManager launch skipped; no current activity");
                return;
            }
            activity.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    try {
                        launchStArrayModManager(activity);
                    } catch (Throwable t) {
                        Log.e(TAG, "StArray ModManager launch failed on UI thread", t);
                    }
                }
            });
        } catch (Throwable t) {
            Log.e(TAG, "StArray ModManager launch entry failed", t);
        }
    }

    private static native boolean nativeOnTouchEvent(MotionEvent event, int viewWidth, int viewHeight);
    private static native boolean nativeOnKeyEvent(KeyEvent event);
    private static native void nativeOnLifecycleReset();
    private static native void nativeOnLifecyclePause();
    private static native void nativeOnLifecycleResume();
    private static native boolean nativeApplyAsyncInputControl(int control, boolean enabled);

    public static boolean applyAsyncInputControlFromShell(int control, boolean enabled) {
        if (!asyncInputLoaded) {
            return false;
        }
        try {
            return nativeApplyAsyncInputControl(control, enabled);
        } catch (Throwable t) {
            Log.e(TAG, "native async control dispatch failed control=" + control, t);
            return false;
        }
    }

    public static void clearImportedEditorLevelsFromNative() {
        try {
            final Activity activity = getUnityActivity();
            if (activity == null) {
                Log.w(TAG, "clear imported editor levels skipped; no current activity");
                return;
            }
            activity.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    try {
                        clearImportedEditorLevels(activity);
                    } catch (Throwable t) {
                        Log.e(TAG, "clear imported editor levels failed on UI thread", t);
                    }
                }
            });
        } catch (Throwable t) {
            Log.e(TAG, "clear imported editor levels entry failed", t);
        }
    }

    private static void clearImportedEditorLevels(Activity activity) {
        ArrayList<File> roots = getEditorImportsRoots(activity);
        long before = 0L;
        int sessions = 0;
        boolean ok = true;
        for (File root : roots) {
            before += directorySize(root);
            sessions += countImportSessions(root);
            ok &= !root.exists() || deleteRecursively(root);
        }
        String message = ok
                ? "已清理 " + sessions + " 个导入目录，释放 " + formatBytes(before)
                : "清理未完全完成，请查看 logcat";
        Toast.makeText(activity, message, Toast.LENGTH_LONG).show();
        Log.i(TAG, "clear imported editor levels ok=" + ok
                + " sessions=" + sessions
                + " bytes=" + before
                + " roots=" + rootsToString(roots));
    }

    private static ArrayList<File> getEditorImportsRoots(Activity activity) {
        ArrayList<File> roots = new ArrayList<File>();
        HashSet<String> seen = new HashSet<String>();
        File[] externalBases = activity.getExternalFilesDirs(null);
        if (externalBases != null) {
            for (File base : externalBases) {
                addEditorImportsRoot(roots, seen, base);
            }
        }
        addEditorImportsRoot(roots, seen, activity.getExternalFilesDir(null));
        addEditorImportsRoot(roots, seen, activity.getFilesDir());
        return roots;
    }

    private static void addEditorImportsRoot(ArrayList<File> roots, HashSet<String> seen, File base) {
        if (base == null) {
            return;
        }
        File root = new File(base, "EditorImports");
        String key;
        try {
            key = root.getCanonicalPath();
        } catch (IOException ignored) {
            key = root.getAbsolutePath();
        }
        if (seen.add(key)) {
            roots.add(root);
        }
    }

    private static String rootsToString(ArrayList<File> roots) {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < roots.size(); i++) {
            if (i > 0) {
                builder.append(';');
            }
            builder.append(roots.get(i).getAbsolutePath());
        }
        return builder.toString();
    }

    private static int countImportSessions(File file) {
        if (file == null || !file.isDirectory()) {
            return 0;
        }
        File[] children = file.listFiles();
        if (children == null) {
            return 0;
        }
        int count = 0;
        for (File child : children) {
            if (child.isDirectory() && child.getName().startsWith("import_")) {
                count++;
            }
        }
        return count;
    }

    private static long directorySize(File file) {
        if (file == null || !file.exists()) {
            return 0L;
        }
        if (file.isFile()) {
            return file.length();
        }
        File[] children = file.listFiles();
        if (children == null) {
            return 0L;
        }
        long total = 0L;
        for (File child : children) {
            total += directorySize(child);
        }
        return total;
    }

    private static boolean deleteRecursively(File file) {
        if (file == null || !file.exists()) {
            return true;
        }
        boolean ok = true;
        if (file.isDirectory()) {
            File[] children = file.listFiles();
            if (children != null) {
                for (File child : children) {
                    ok &= deleteRecursively(child);
                }
            }
        }
        if (!file.delete() && file.exists()) {
            Log.w(TAG, "Could not delete imported editor file: " + file.getAbsolutePath());
            ok = false;
        }
        return ok;
    }

    private static String formatBytes(long bytes) {
        if (bytes < 1024L) {
            return bytes + " B";
        }
        double value = bytes / 1024.0;
        String[] units = {"KB", "MB", "GB"};
        int unit = 0;
        while (value >= 1024.0 && unit < units.length - 1) {
            value /= 1024.0;
            unit++;
        }
        return String.format(java.util.Locale.ROOT, "%.1f %s", value, units[unit]);
    }

    private static Activity getUnityActivity() {
        if (currentActivity != null) {
            return currentActivity;
        }
        try {
            Class<?> unityPlayer = Class.forName("com.unity3d.player.UnityPlayer");
            Object value = unityPlayer.getField("currentActivity").get(null);
            if (value instanceof Activity) {
                return (Activity) value;
            }
        } catch (Throwable t) {
            Log.e(TAG, "Could not read UnityPlayer.currentActivity", t);
        }
        return null;
    }

    private static boolean isLicenseLaunchAllowed(Activity activity) {
        try {
            Class<?> verifier = Class.forName("com.fizzd.connectedworlds.editorport.AppLicenseVerifier");
            Object value = verifier.getMethod("isLaunchAllowed", Activity.class).invoke(null, activity);
            return Boolean.TRUE.equals(value);
        } catch (ClassNotFoundException missingInNormalBuild) {
            return true;
        } catch (Throwable t) {
            Log.e(TAG, "license gate check failed", t);
            return false;
        }
    }

    private static void returnToLicenseActivity(Activity activity) {
        try {
            Class<?> verifier = Class.forName("com.fizzd.connectedworlds.editorport.AppLicenseVerifier");
            verifier.getMethod("returnToLicenseActivity", Activity.class).invoke(null, activity);
        } catch (Throwable t) {
            Log.e(TAG, "license gate redirect failed", t);
        }
    }

    @Override
    protected void onCreate(android.os.Bundle savedInstanceState) {
        if (!isLicenseLaunchAllowed(this)) {
            Log.w(TAG, "blocked direct game activity launch");
            returnToLicenseActivity(this);
            terminateUnauthorizedProcess();
            return;
        }
        currentActivity = this;
        super.onCreate(savedInstanceState);
        startStArrayModManagerInBackground(this);
    }

    private void terminateUnauthorizedProcess() {
        try {
            finishAndRemoveTask();
        } catch (Throwable ignored) {
            finish();
        }
        android.os.Process.killProcess(android.os.Process.myPid());
        System.exit(0);
    }

    @Override
    protected void onDestroy() {
        if (currentActivity == this) {
            currentActivity = null;
        }
        super.onDestroy();
    }

    @Override
    public boolean dispatchTouchEvent(MotionEvent event) {
        View view = getWindow().getDecorView();
        int viewWidth = view.getWidth();
        int viewHeight = view.getHeight();
        boolean modalCapture = isStArrayModalInputCaptureActive();
        if (modalCapture) {
            // The original MOD Canvas/IMGUI must receive the real Unity event,
            // while gameplay observers and AsyncInput stay outside the modal.
            logStArrayInputGate("route=unity-modal action=" + actionName(event)
                    + " pointers=" + pointerCount(event)
                    + " window=" + viewWidth + "x" + viewHeight);
            return super.dispatchTouchEvent(event);
        }
        boolean forwarded = forwardStArrayMotionEvent(event);
        if (forwarded) {
            logStArrayInputGate("route=modmanager action=" + actionName(event)
                    + " pointers=" + pointerCount(event) + " consumed=true");
            return true;
        }
        observeStArrayMotionEvent(event, viewWidth, viewHeight);
        boolean asyncConsumed = false;
        if (asyncInputLoaded) {
            try {
                asyncConsumed = nativeOnTouchEvent(event, viewWidth, viewHeight);
                if (asyncConsumed) {
                    logStArrayInputGate("route=async action=" + actionName(event)
                            + " pointers=" + pointerCount(event) + " consumed=true");
                    return true;
                }
            } catch (Throwable t) {
                Log.e(TAG, "nativeOnTouchEvent failed", t);
            }
        }
        logStArrayInputGate("route=unity-gameplay action=" + actionName(event)
                + " pointers=" + pointerCount(event)
                + " asyncLoaded=" + asyncInputLoaded
                + " asyncConsumed=" + asyncConsumed);
        return super.dispatchTouchEvent(event);
    }

    private static int pointerCount(MotionEvent event) {
        return event == null ? 0 : event.getPointerCount();
    }

    private static String actionName(MotionEvent event) {
        if (event == null) {
            return "null";
        }
        switch (event.getActionMasked()) {
            case MotionEvent.ACTION_DOWN: return "DOWN";
            case MotionEvent.ACTION_UP: return "UP";
            case MotionEvent.ACTION_MOVE: return "MOVE";
            case MotionEvent.ACTION_CANCEL: return "CANCEL";
            case MotionEvent.ACTION_POINTER_DOWN: return "POINTER_DOWN";
            case MotionEvent.ACTION_POINTER_UP: return "POINTER_UP";
            default: return Integer.toString(event.getActionMasked());
        }
    }

    private static void logStArrayInputGate(String message) {
        long now = android.os.SystemClock.uptimeMillis();
        boolean modal = isStArrayModalInputCaptureActive();
        boolean transition = !stArrayInputGateHasLastModal || modal != stArrayInputGateLastModal;
        boolean periodic = now - stArrayInputGateLastMotionLogUptime >=
                STARRAY_INPUT_GATE_MOTION_LOG_INTERVAL_MS;
        if (!transition && !periodic) {
            return;
        }
        stArrayInputGateHasLastModal = true;
        stArrayInputGateLastModal = modal;
        stArrayInputGateLastMotionLogUptime = now;
        Log.i("StArrayInputGate", "modal=" + modal + " " + message);
    }

    private static void observeStArrayMotionEvent(MotionEvent event, int viewWidth, int viewHeight) {
        if (event == null) {
            return;
        }
        try {
            Method method = stArrayObserveMotionEvent;
            if (method == null && !stArrayObserveMotionChecked) {
                stArrayObserveMotionChecked = true;
                Class<?> bootstrap = Class.forName(
                        "com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
                method = bootstrap.getMethod(
                        "observeGameplayMotionEvent", MotionEvent.class, int.class, int.class);
                stArrayObserveMotionEvent = method;
            }
            if (method != null) {
                method.invoke(null, event, viewWidth, viewHeight);
            }
        } catch (Throwable t) {
            if (!stArrayObserveMotionErrorLogged) {
                stArrayObserveMotionErrorLogged = true;
                Log.w(TAG, "StArray ModManager input observation failed", t);
            }
        }
    }

    private static boolean forwardStArrayMotionEvent(MotionEvent event) {
        if (!stArrayModManagerInputForwarding || event == null) {
            return false;
        }
        try {
            Method method = stArrayForwardMotionEvent;
            if (method == null && !stArrayForwardMotionChecked) {
                stArrayForwardMotionChecked = true;
                Class<?> bootstrap = Class.forName(
                        "com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
                method = bootstrap.getMethod("forwardMotionEvent", MotionEvent.class);
                stArrayForwardMotionEvent = method;
            }
            if (method != null) {
                Object result = method.invoke(null, event);
                return Boolean.TRUE.equals(result);
            }
        } catch (Throwable t) {
            if (!stArrayForwardMotionErrorLogged) {
                stArrayForwardMotionErrorLogged = true;
                Log.w(TAG, "StArray ModManager motion forwarding failed", t);
            }
        }
        return false;
    }

    private static boolean isStArrayModalInputCaptureActive() {
        try {
            Method method = stArrayIsModalInputCaptureActive;
            if (method == null && !stArrayModalInputChecked) {
                stArrayModalInputChecked = true;
                Class<?> bootstrap = Class.forName(
                        "com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
                method = bootstrap.getMethod("isModalInputCaptureActive");
                stArrayIsModalInputCaptureActive = method;
                stArrayRequestModalClose = bootstrap.getMethod("requestModalClose");
            }
            if (method != null) {
                Object result = method.invoke(null);
                return result instanceof Integer && ((Integer) result).intValue() != 0;
            }
        } catch (Throwable t) {
            if (!stArrayModalInputErrorLogged) {
                stArrayModalInputErrorLogged = true;
                Log.w(TAG, "StArray ModManager modal input query failed", t);
            }
        }
        return false;
    }

    private static void requestStArrayModalClose() {
        try {
            Method method = stArrayRequestModalClose;
            if (method != null) {
                method.invoke(null);
            }
        } catch (Throwable t) {
            if (!stArrayModalInputErrorLogged) {
                stArrayModalInputErrorLogged = true;
                Log.w(TAG, "StArray ModManager modal close request failed", t);
            }
        }
    }

    private static void observeStArrayKeyEvent(KeyEvent event) {
        if (event == null) {
            return;
        }
        try {
            Method method = stArrayObserveKeyEvent;
            if (method == null && !stArrayObserveKeyChecked) {
                stArrayObserveKeyChecked = true;
                Class<?> bootstrap = Class.forName(
                        "com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
                method = bootstrap.getMethod("observeGameplayKeyEvent", KeyEvent.class);
                stArrayObserveKeyEvent = method;
            }
            if (method != null) {
                method.invoke(null, event);
            }
        } catch (Throwable t) {
            if (!stArrayObserveKeyErrorLogged) {
                stArrayObserveKeyErrorLogged = true;
                Log.w(TAG, "StArray ModManager key observation failed", t);
            }
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (handleStArrayActivityResult(requestCode, resultCode, data)) {
            return;
        }
        super.onActivityResult(requestCode, resultCode, data);
    }

    private static boolean handleStArrayActivityResult(int requestCode, int resultCode, Intent data) {
        try {
            Method method = stArrayHandleActivityResult;
            if (method == null && !stArrayActivityResultChecked) {
                stArrayActivityResultChecked = true;
                Class<?> bootstrap = Class.forName(
                        "com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
                method = bootstrap.getMethod("handleActivityResult",
                        int.class, int.class, Intent.class);
                stArrayHandleActivityResult = method;
            }
            if (method != null) {
                Object result = method.invoke(null, requestCode, resultCode, data);
                return Boolean.TRUE.equals(result);
            }
        } catch (Throwable t) {
            if (!stArrayActivityResultErrorLogged) {
                stArrayActivityResultErrorLogged = true;
                Log.w(TAG, "StArray ModManager activity result forwarding failed", t);
            }
        }
        return false;
    }

    private boolean dispatchObservedStArrayKeyEvent(KeyEvent event) {
        observeStArrayKeyEvent(event);
        if (asyncInputLoaded) {
            try {
                if (nativeOnKeyEvent(event)) {
                    return true;
                }
            } catch (Throwable t) {
                Log.e(TAG, "nativeOnKeyEvent failed", t);
            }
        }
        return super.dispatchKeyEvent(event);
    }

    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        if (isStArrayModalInputCaptureActive()) {
            if (event != null && event.getKeyCode() == KeyEvent.KEYCODE_BACK) {
                if (event.getAction() == KeyEvent.ACTION_UP) {
                    requestStArrayModalClose();
                }
                return true;
            }
            return dispatchObservedStArrayKeyEvent(event);
        }
        return dispatchObservedStArrayKeyEvent(event);
    }

    @Override
    protected void onPause() {
        if (asyncInputLoaded) {
            try {
                nativeOnLifecyclePause();
            } catch (Throwable t) {
                Log.e(TAG, "nativeOnLifecyclePause failed", t);
            }
        }
        super.onPause();
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (asyncInputLoaded) {
            try {
                nativeOnLifecycleResume();
            } catch (Throwable t) {
                Log.e(TAG, "nativeOnLifecycleResume failed", t);
            }
        }
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        if (asyncInputLoaded) {
            try {
                if (hasFocus) {
                    nativeOnLifecycleResume();
                } else {
                    nativeOnLifecyclePause();
                }
            } catch (Throwable t) {
                Log.e(TAG, "nativeOnLifecycle focus update failed", t);
            }
        }
        super.onWindowFocusChanged(hasFocus);
    }
}
