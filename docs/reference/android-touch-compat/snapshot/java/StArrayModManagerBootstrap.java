package com.fizzd.connectedworlds.editorport;

import android.app.Activity;
import android.content.Context;
import android.content.ContentResolver;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Build;
import android.os.SystemClock;
import android.provider.OpenableColumns;
import android.view.KeyEvent;
import android.view.KeyCharacterMap;
import android.view.InputDevice;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewTreeObserver;
import android.view.WindowInsets;
import android.view.inputmethod.BaseInputConnection;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.InputConnection;
import android.view.inputmethod.InputMethodManager;
import android.hardware.input.InputManager;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.StandardCopyOption;
import java.util.Objects;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

@SuppressWarnings("deprecation")
public final class StArrayModManagerBootstrap {
    private static final String TAG = "StArrayBootstrap";
    private static final long CAP_MODMANAGER_RUNTIME = 1L << 2;
    private static Activity sActivity;
    private static volatile boolean sLaunched;
    private static volatile boolean sCoreClrAttempted;
    private static volatile boolean sLaunchSucceeded;
    private static volatile boolean sEnableUi = false;
    private static volatile boolean sEnableInputHooks = false;
    private static volatile boolean sModalInputCapture;
    private static final Object sLaunchLock = new Object();
    private static KeyboardView sKeyboardView;
    private static volatile boolean sKeyboardShown;
    private static volatile boolean sKeyboardActuallyVisible;
    private static View sImeVisibilityDecor;
    private static long sLastKeyboardRequestUptime;
    private static final long KEYBOARD_REQUEST_MIN_INTERVAL_MS = 180L;
    private static final int REQUEST_IMPORT_MOD_ZIP = 0x535A31;
    private static final int REQUEST_EXPORT_DIAGNOSTICS = 0x535A32;
    private static final Object sImportLock = new Object();
    private static int sImportSerial;
    private static String sImportState = "Idle";
    private static String sImportMessage = "";
    private static String sImportPath = "";
    private static final Object sExportLock = new Object();
    private static int sExportSerial;
    private static String sExportState = "Idle";
    private static String sExportMessage = "";
    private static String sPendingExportContent = "";
    private static final int EXTERNAL_INPUT_KEYBOARD = 1;
    private static final int EXTERNAL_INPUT_CONTROLLER = 1 << 1;
    private static final int EXTERNAL_INPUT_MOUSE = 1 << 2;
    private static boolean sInputDeviceMonitorInstalled;
    private static int sExternalInputDeviceFlags = -1;
    private static final InputManager.InputDeviceListener INPUT_DEVICE_LISTENER =
            new InputManager.InputDeviceListener() {
                @Override
                public void onInputDeviceAdded(int deviceId) {
                    refreshExternalInputDevices();
                }

                @Override
                public void onInputDeviceRemoved(int deviceId) {
                    refreshExternalInputDevices();
                }

                @Override
                public void onInputDeviceChanged(int deviceId) {
                    refreshExternalInputDevices();
                }
            };

    static {
        System.loadLibrary("starray_modmanager");
    }

    private StArrayModManagerBootstrap() {}

    public static void setUiEnabled(boolean enabled) {
        sEnableUi = enabled;
    }

    public static void setInputHooksEnabled(boolean enabled) {
        sEnableInputHooks = enabled;
    }

    public static void setModalInputCapture(boolean active) {
        boolean previous = sModalInputCapture;
        sModalInputCapture = active;
        nativeSetModalInputCapture(active);
        if (active && !previous) {
            // ModManager's hidden 1x1 KeyboardView must not retain Android IME
            // ownership while the original MOD draws through Unity IMGUI/uGUI.
            showKeyboard(false);
        }
        if (previous != active) {
            android.util.Log.i("StArrayInputGate", "modalCapture=" + active
                    + " keyboardShown=" + sKeyboardShown
                    + " launchSucceeded=" + sLaunchSucceeded);
        }
    }

    public static int isModalInputCaptureActive() {
        return sLaunchSucceeded && sModalInputCapture ? 1 : 0;
    }

    public static void requestModalClose() {
        if (sModalInputCapture) {
            nativeRequestModalClose();
        }
    }

    public static int consumeModalCloseRequest() {
        return nativeTakeModalCloseRequest();
    }

    public static void startInBackground(Activity activity) {
        requestLaunch(activity, false);
    }

    public static void launch(Activity activity) {
        requestLaunch(activity, true);
    }

    private static void requestLaunch(Activity activity, boolean showOverlay) {
        if (activity == null) {
            return;
        }
        if (!nativeHasCapability(CAP_MODMANAGER_RUNTIME)) {
            android.util.Log.i(TAG, "launch rejected: modmanager_runtime capability unavailable");
            return;
        }
        installInputDeviceMonitor(activity);
        installImeVisibilityObserver(activity);
        synchronized (sLaunchLock) {
            if (sLaunched || sCoreClrAttempted) {
                sActivity = activity;
                try {
                    nativeSetOverlayVisible(showOverlay);
                } catch (Throwable ignored) {
                }
                android.util.Log.i(TAG, "CoreCLR bootstrap already active; overlay=" + showOverlay
                        + " launched=" + sLaunched
                        + " coreClrAttempted=" + sCoreClrAttempted
                        + " succeeded=" + sLaunchSucceeded);
                return;
            }
            sActivity = activity;
            // CoreCLR, mod scanning and native hook synchronization start in
            // the background. The ImGui renderer is installed lazily by the
            // managed side when the menu is first shown or a fallback overlay
            // really needs it, so normal gameplay does not enter the EGL hook.
            sEnableUi = true;
            sLaunched = true;
        }

        try {
            nativeSetOverlayVisible(showOverlay);
        } catch (Throwable t) {
            android.util.Log.w(TAG, "could not set initial overlay visibility", t);
        }

        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    launchOnWorker();
                    sLaunchSucceeded = true;
                } catch (Throwable t) {
                    android.util.Log.e(TAG, "launch failed; restart app before retrying CoreCLR bootstrap", t);
                    if (!sCoreClrAttempted) {
                        synchronized (sLaunchLock) {
                            sLaunched = false;
                        }
                    }
                }
            }
        }, "StArray-ModManager").start();
    }

    private static void installInputDeviceMonitor(Activity activity) {
        if (!sInputDeviceMonitorInstalled) {
            synchronized (sLaunchLock) {
                if (!sInputDeviceMonitorInstalled) {
                    InputManager manager = (InputManager) activity.getSystemService(
                            Context.INPUT_SERVICE);
                    if (manager != null) {
                        manager.registerInputDeviceListener(INPUT_DEVICE_LISTENER, null);
                        sInputDeviceMonitorInstalled = true;
                    }
                }
            }
        }
        refreshExternalInputDevices();
    }

    private static void refreshExternalInputDevices() {
        int flags = 0;
        for (int deviceId : InputDevice.getDeviceIds()) {
            InputDevice device = InputDevice.getDevice(deviceId);
            if (device == null || device.isVirtual() || !device.isExternal()) {
                continue;
            }
            int sources = device.getSources();
            if (device.getKeyboardType() == InputDevice.KEYBOARD_TYPE_ALPHABETIC) {
                flags |= EXTERNAL_INPUT_KEYBOARD;
            }
            if ((sources & InputDevice.SOURCE_GAMEPAD) == InputDevice.SOURCE_GAMEPAD ||
                    (sources & InputDevice.SOURCE_JOYSTICK) == InputDevice.SOURCE_JOYSTICK ||
                    (sources & InputDevice.SOURCE_DPAD) == InputDevice.SOURCE_DPAD) {
                flags |= EXTERNAL_INPUT_CONTROLLER;
            }
            if ((sources & InputDevice.SOURCE_MOUSE) == InputDevice.SOURCE_MOUSE) {
                flags |= EXTERNAL_INPUT_MOUSE;
            }
        }
        if (sExternalInputDeviceFlags == flags) {
            return;
        }
        sExternalInputDeviceFlags = flags;
        try {
            nativeSetExternalInputDevices(flags);
        } catch (Throwable t) {
            android.util.Log.w(TAG, "could not publish external input device state", t);
        }
    }

    private static void launchOnWorker() throws IOException {
        if (!nativeHasCapability(CAP_MODMANAGER_RUNTIME)) {
            throw new IllegalStateException("modmanager_runtime capability unavailable");
        }
        Activity activity = Objects.requireNonNull(sActivity);
        File internalRoot = new File(activity.getFilesDir(), "ModManager");
        File externalRoot = new File(activity.getExternalFilesDir(null), "ModManager");
        File runtime = new File(internalRoot, "runtime");
        File plugins = new File(internalRoot, "plugins");
        File config = new File(internalRoot, "config");
        File mods = new File(externalRoot, "mods");
        extractAssetDir(activity, "runtime", runtime);
        if (!nativeVerifyRuntimeManifest(runtime.getAbsolutePath())) {
            android.util.Log.w(TAG, "runtime manifest mismatch; rebuilding extracted cache once");
            deleteChildren(runtime);
            extractAssetDir(activity, "runtime", runtime);
            if (!nativeVerifyRuntimeManifest(runtime.getAbsolutePath())) {
                throw new IllegalStateException(
                        "runtime manifest verification failed root=" + nativeRuntimeManifestRoot());
            }
        }
        requireRuntimeFile(runtime, "libcoreclr.so");
        requireRuntimeFile(runtime, "StArray.ModManager.Android.dll");
        requireRuntimeFile(runtime, "pc_compat_capabilities/pccompat_capabilities_android");
        requireRuntimeFile(runtime, "pc_compat_capabilities/pccompat_capability_whitelist.json");
        requireRuntimeFile(runtime, "pc_compat_capabilities/pccompat_capabilities_android.manifest.json");
        plugins.mkdirs();
        config.mkdirs();
        mods.mkdirs();

        String appPaths = runtime.getAbsolutePath() + ":" + plugins.getAbsolutePath();
        nativeSetEnv("DOTNET_ROOT", runtime.getAbsolutePath());
        nativeSetEnv("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
        nativeSetEnv("HOME", runtime.getAbsolutePath());
        nativeSetEnv("TRUSTED_PLATFORM_ASSEMBLIES", appPaths);
        nativeSetEnv("APP_PATHS", appPaths);
        nativeSetEnv("NATIVE_DLL_SEARCH_DIRECTORIES", runtime.getAbsolutePath());
        nativeSetEnv("STARRAY_MODMANAGER_RUNTIME_ROOT", runtime.getAbsolutePath());
        nativeSetEnv("STARRAY_MODMANAGER_ENABLE_UI", sEnableUi ? "1" : "0");
        nativeSetEnv("STARRAY_MODMANAGER_ENABLE_INPUT_HOOKS", sEnableInputHooks ? "1" : "0");
        nativeSetEnv("STARRAY_MODMANAGER_ENABLE_FILE_LOG", "0");
        nativeSetEnv("STARRAY_MODMANAGER_BENCHMARK", "0");

        android.util.Log.i(TAG, "initializing CoreCLR runtime=" + runtime.getAbsolutePath());
        sCoreClrAttempted = true;
        int init = nativeInitRuntime(runtime.getAbsolutePath(), "StArray.ModManager.Android.dll", 0);
        if (init != 0) {
            android.util.Log.e(TAG, "nativeInitRuntime failed rc=0x" + Integer.toHexString(init));
            throw new IllegalStateException("coreclr init failed: 0x" + Integer.toHexString(init));
        }
        nativeExecEntryPointWithArgs(
                "StArray.ModManager.Android.dll",
                "StArray.ModManager.Android.dll",
                "StArray.ModManager.Android.Managed",
                "Entry",
                new String[] { mods.getAbsolutePath(), config.getAbsolutePath() });
    }

    public static void requestImportModZip() {
        final Activity activity = sActivity;
        if (activity == null) {
            setImportStatus("Error", "Activity is not ready", "");
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
                    intent.addCategory(Intent.CATEGORY_OPENABLE);
                    intent.setType("*/*");
                    intent.putExtra(Intent.EXTRA_MIME_TYPES, new String[] {
                            "application/zip",
                            "application/x-zip-compressed",
                            "application/octet-stream"
                    });
                    setImportStatus("Selecting", "Choose a PC MOD zip file", "");
                    activity.startActivityForResult(intent, REQUEST_IMPORT_MOD_ZIP);
                } catch (Throwable t) {
                    android.util.Log.e(TAG, "requestImportModZip failed", t);
                    setImportStatus("Error", t.toString(), "");
                }
            }
        });
    }

    public static boolean handleActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == REQUEST_EXPORT_DIAGNOSTICS) {
            handleDiagnosticsExportResult(resultCode, data);
            return true;
        }
        if (requestCode != REQUEST_IMPORT_MOD_ZIP) {
            return false;
        }

        final Activity activity = sActivity;
        final Uri uri = data != null ? data.getData() : null;
        if (resultCode != Activity.RESULT_OK || activity == null || uri == null) {
            setImportStatus("Cancelled", "Import cancelled", "");
            return true;
        }

        try {
            int flags = data.getFlags() &
                    (Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
            activity.getContentResolver().takePersistableUriPermission(uri, flags);
        } catch (Throwable ignored) {
            // Some providers do not offer persistable permission. The immediate stream copy below is enough.
        }

        setImportStatus("Importing", "Copying selected zip", "");
        new Thread(new Runnable() {
            @Override
            public void run() {
                importModZip(activity, uri);
            }
        }, "StArray-ModImport").start();
        return true;
    }

    public static void requestExportDiagnostics(String suggestedName, String content) {
        final Activity activity = sActivity;
        if (activity == null) {
            setExportStatus("Error", "Activity is not ready");
            return;
        }

        synchronized (sExportLock) {
            sPendingExportContent = content != null ? content : "";
        }
        final String safeName = sanitizeFileName(suggestedName);
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT);
                    intent.addCategory(Intent.CATEGORY_OPENABLE);
                    intent.setType("text/plain");
                    intent.putExtra(Intent.EXTRA_TITLE,
                            safeName.length() > 0 ? safeName : "pccompat_diagnostics.txt");
                    setExportStatus("Selecting", "Choose where to save the diagnostics report");
                    activity.startActivityForResult(intent, REQUEST_EXPORT_DIAGNOSTICS);
                } catch (Throwable t) {
                    android.util.Log.e(TAG, "requestExportDiagnostics failed", t);
                    setExportStatus("Error", t.toString());
                }
            }
        });
    }

    public static String getLastExportStatusJson() {
        synchronized (sExportLock) {
            return "{"
                    + "\"serial\":" + sExportSerial + ","
                    + "\"state\":\"" + jsonEscape(sExportState) + "\","
                    + "\"message\":\"" + jsonEscape(sExportMessage) + "\""
                    + "}";
        }
    }

    private static void handleDiagnosticsExportResult(int resultCode, Intent data) {
        final Activity activity = sActivity;
        final Uri uri = data != null ? data.getData() : null;
        if (resultCode != Activity.RESULT_OK || activity == null || uri == null) {
            synchronized (sExportLock) {
                sPendingExportContent = "";
            }
            setExportStatus("Cancelled", "Export cancelled");
            return;
        }

        final String content;
        synchronized (sExportLock) {
            content = sPendingExportContent;
            sPendingExportContent = "";
        }
        setExportStatus("Exporting", "Writing diagnostics report");
        new Thread(new Runnable() {
            @Override
            public void run() {
                try (OutputStream output = activity.getContentResolver().openOutputStream(uri, "wt")) {
                    if (output == null) {
                        throw new IOException("Could not open the selected document");
                    }
                    output.write(content.getBytes(StandardCharsets.UTF_8));
                    output.flush();
                    setExportStatus("Exported", "Diagnostics report saved");
                } catch (Throwable t) {
                    android.util.Log.e(TAG, "diagnostics export failed", t);
                    setExportStatus("Error", t.toString());
                }
            }
        }, "StArray-DiagnosticsExport").start();
    }

    public static String getLastImportStatusJson() {
        synchronized (sImportLock) {
            return "{"
                    + "\"serial\":" + sImportSerial + ","
                    + "\"state\":\"" + jsonEscape(sImportState) + "\","
                    + "\"message\":\"" + jsonEscape(sImportMessage) + "\","
                    + "\"path\":\"" + jsonEscape(sImportPath) + "\""
                    + "}";
        }
    }

    private static void importModZip(Activity activity, Uri uri) {
        File copiedZip = null;
        File tempDir = null;
        try {
            File externalRoot = new File(activity.getExternalFilesDir(null), "ModManager");
            File imports = new File(externalRoot, "imports");
            File tempRoot = new File(externalRoot, "import_tmp");
            File mods = new File(externalRoot, "mods");
            imports.mkdirs();
            tempRoot.mkdirs();
            mods.mkdirs();

            String displayName = sanitizeFileName(resolveDisplayName(activity, uri));
            if (displayName.length() == 0) {
                displayName = "imported_mod.zip";
            }
            if (!displayName.toLowerCase(java.util.Locale.ROOT).endsWith(".zip")) {
                displayName += ".zip";
            }

            copiedZip = uniqueFile(imports, displayName);
            copyUriToFile(activity, uri, copiedZip);
            setImportStatus("Importing", "Extracting " + copiedZip.getName(), copiedZip.getAbsolutePath());

            tempDir = uniqueDirectory(tempRoot, stripExtension(copiedZip.getName()));
            unzipSafely(copiedZip, tempDir);

            File modRoot = findImportedModRoot(tempDir);
            String targetName = sanitizeFileName(stripExtension(modRoot.getName()));
            if (targetName.length() == 0 || targetName.startsWith("import_tmp")) {
                targetName = sanitizeFileName(stripExtension(copiedZip.getName()));
            }
            File targetDir = uniqueDirectory(mods, targetName);
            moveDirectory(modRoot, targetDir);
            if (!sameFile(tempDir, targetDir) && tempDir.exists()) {
                deleteChildren(tempDir);
                tempDir.delete();
            }

            setImportStatus("Imported", "Imported " + targetDir.getName(), targetDir.getAbsolutePath());
            android.util.Log.i(TAG, "Mod zip imported zip=" + copiedZip.getAbsolutePath()
                    + " target=" + targetDir.getAbsolutePath());
        } catch (Throwable t) {
            android.util.Log.e(TAG, "importModZip failed", t);
            if (tempDir != null && tempDir.exists()) {
                try {
                    deleteChildren(tempDir);
                    tempDir.delete();
                } catch (Throwable ignored) {
                }
            }
            setImportStatus("Error", t.toString(), copiedZip != null ? copiedZip.getAbsolutePath() : "");
        }
    }

    private static String resolveDisplayName(Activity activity, Uri uri) {
        ContentResolver resolver = activity.getContentResolver();
        Cursor cursor = null;
        try {
            cursor = resolver.query(uri, null, null, null, null);
            if (cursor != null && cursor.moveToFirst()) {
                int index = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                if (index >= 0) {
                    String name = cursor.getString(index);
                    if (name != null) {
                        return name;
                    }
                }
            }
        } catch (Throwable ignored) {
        } finally {
            if (cursor != null) {
                cursor.close();
            }
        }

        String last = uri.getLastPathSegment();
        return last != null ? last : "imported_mod.zip";
    }

    private static void copyUriToFile(Activity activity, Uri uri, File outFile) throws IOException {
        InputStream in = activity.getContentResolver().openInputStream(uri);
        if (in == null) {
            throw new IOException("Could not open selected zip");
        }

        byte[] buffer = new byte[65536];
        try (InputStream source = in; OutputStream out = new FileOutputStream(outFile)) {
            int read;
            while ((read = source.read(buffer)) != -1) {
                out.write(buffer, 0, read);
            }
        }
    }

    private static void unzipSafely(File zipFile, File destDir) throws IOException {
        destDir.mkdirs();
        String destCanonical = destDir.getCanonicalPath() + File.separator;
        byte[] buffer = new byte[65536];
        try (ZipInputStream zip = new ZipInputStream(new FileInputStream(zipFile), StandardCharsets.UTF_8)) {
            ZipEntry entry;
            while ((entry = zip.getNextEntry()) != null) {
                String entryName = normalizeZipEntryName(entry.getName());
                if (entryName == null || entryName.length() == 0) {
                    continue;
                }

                File out = new File(destDir, entryName);
                String outCanonical = out.getCanonicalPath();
                if (!outCanonical.equals(destDir.getCanonicalPath()) &&
                        !outCanonical.startsWith(destCanonical)) {
                    throw new IOException("Unsafe zip entry: " + entryName);
                }

                if (entry.isDirectory() || entryName.endsWith("/")) {
                    out.mkdirs();
                } else {
                    File parent = out.getParentFile();
                    if (parent != null) {
                        parent.mkdirs();
                    }
                    try (OutputStream os = new FileOutputStream(out)) {
                        int read;
                        while ((read = zip.read(buffer)) != -1) {
                            os.write(buffer, 0, read);
                        }
                    }
                }
                zip.closeEntry();
            }
        }
    }

    private static String normalizeZipEntryName(String entryName) {
        if (entryName == null) {
            return null;
        }

        String normalized = entryName.replace('\\', '/');
        while (normalized.startsWith("./")) {
            normalized = normalized.substring(2);
        }
        return normalized;
    }

    private static File findImportedModRoot(File root) {
        if (looksLikeModRoot(root)) {
            return root;
        }

        File[] children = root.listFiles();
        if (children == null) {
            return root;
        }
        for (File child : children) {
            if (child.isDirectory() && looksLikeModRoot(child)) {
                return child;
            }
        }
        return root;
    }

    private static boolean looksLikeModRoot(File dir) {
        if (!dir.isDirectory()) {
            return false;
        }
        if (new File(dir, "Info.json").isFile() || new File(dir, "JAModInfo.json").isFile()) {
            return true;
        }
        File[] files = dir.listFiles();
        if (files == null) {
            return false;
        }
        for (File file : files) {
            if (file.isFile() && file.getName().toLowerCase(java.util.Locale.ROOT).endsWith(".dll")) {
                return true;
            }
        }
        return false;
    }

    private static void moveDirectory(File source, File target) throws IOException {
        File parent = target.getParentFile();
        if (parent != null) {
            parent.mkdirs();
        }
        try {
            Files.move(source.toPath(), target.toPath(), StandardCopyOption.ATOMIC_MOVE);
            return;
        } catch (Throwable ignored) {
        }
        try {
            Files.move(source.toPath(), target.toPath());
            return;
        } catch (Throwable ignored) {
        }
        copyDirectory(source, target);
        deleteChildren(source);
        if (!source.delete() && source.exists()) {
            throw new IOException("Could not delete import staging dir: " + source.getAbsolutePath());
        }
    }

    private static void copyDirectory(File source, File target) throws IOException {
        if (source.isDirectory()) {
            target.mkdirs();
            File[] children = source.listFiles();
            if (children == null) {
                return;
            }
            for (File child : children) {
                copyDirectory(child, new File(target, child.getName()));
            }
            return;
        }

        File parent = target.getParentFile();
        if (parent != null) {
            parent.mkdirs();
        }
        Files.copy(source.toPath(), target.toPath(), StandardCopyOption.REPLACE_EXISTING);
    }

    private static File uniqueFile(File dir, String fileName) {
        dir.mkdirs();
        String safe = sanitizeFileName(fileName);
        String base = stripExtension(safe);
        String ext = safe.endsWith(".zip") ? ".zip" : "";
        File candidate = new File(dir, safe);
        int index = 1;
        while (candidate.exists()) {
            candidate = new File(dir, base + "_" + index + ext);
            index++;
        }
        return candidate;
    }

    private static File uniqueDirectory(File dir, String name) {
        dir.mkdirs();
        String base = sanitizeFileName(name);
        if (base.length() == 0) {
            base = "mod";
        }
        File candidate = new File(dir, base);
        int index = 1;
        while (candidate.exists()) {
            candidate = new File(dir, base + "_" + index);
            index++;
        }
        return candidate;
    }

    private static String sanitizeFileName(String name) {
        if (name == null) {
            return "";
        }
        String clean = new File(name).getName().trim();
        clean = clean.replaceAll("[\\\\/:*?\"<>|\\p{Cntrl}]+", "_");
        while (clean.startsWith(".")) {
            clean = clean.substring(1);
        }
        return clean;
    }

    private static String stripExtension(String name) {
        int dot = name.lastIndexOf('.');
        return dot > 0 ? name.substring(0, dot) : name;
    }

    private static boolean sameFile(File a, File b) {
        try {
            return a.getCanonicalPath().equals(b.getCanonicalPath());
        } catch (IOException ignored) {
            return a.getAbsolutePath().equals(b.getAbsolutePath());
        }
    }

    private static void setImportStatus(String state, String message, String path) {
        synchronized (sImportLock) {
            sImportSerial++;
            sImportState = state != null ? state : "Idle";
            sImportMessage = message != null ? message : "";
            sImportPath = path != null ? path : "";
        }
    }

    private static void setExportStatus(String state, String message) {
        synchronized (sExportLock) {
            sExportSerial++;
            sExportState = state != null ? state : "Idle";
            sExportMessage = message != null ? message : "";
        }
    }

    private static String jsonEscape(String value) {
        if (value == null) {
            return "";
        }
        StringBuilder sb = new StringBuilder(value.length() + 16);
        for (int i = 0; i < value.length(); i++) {
            char ch = value.charAt(i);
            switch (ch) {
                case '\\': sb.append("\\\\"); break;
                case '"': sb.append("\\\""); break;
                case '\n': sb.append("\\n"); break;
                case '\r': sb.append("\\r"); break;
                case '\t': sb.append("\\t"); break;
                default:
                    if (ch < 0x20) {
                        sb.append(String.format(java.util.Locale.ROOT, "\\u%04x", (int) ch));
                    } else {
                        sb.append(ch);
                    }
                    break;
            }
        }
        return sb.toString();
    }

    private static void extractAssetDir(Activity activity, String assetDir, File targetDir) throws IOException {
        String stamp = Long.toString(getPackageUpdateStamp(activity));
        File stampFile = new File(targetDir, ".asset_stamp");
        if (stampFile.exists()
                && stamp.equals(new String(Files.readAllBytes(stampFile.toPath()), StandardCharsets.UTF_8))
                && new File(targetDir, "System.Private.CoreLib.dll").exists()) {
            return;
        }
        deleteChildren(targetDir);
        targetDir.mkdirs();
        String[] files = activity.getAssets().list(assetDir);
        if (files == null) {
            return;
        }
        byte[] buffer = new byte[8192];
        copyAssetTree(activity, assetDir, targetDir, buffer);
        Files.write(stampFile.toPath(), stamp.getBytes(StandardCharsets.UTF_8));
    }

    private static void copyAssetTree(Activity activity, String assetDir, File targetDir, byte[] buffer)
            throws IOException {
        String[] children = activity.getAssets().list(assetDir);
        if (children == null) {
            return;
        }
        targetDir.mkdirs();
        for (String name : children) {
            String childAsset = assetDir + "/" + name;
            File out = new File(targetDir, new File(name).getName());
            if (assetHasChildren(activity, childAsset)) {
                copyAssetTree(activity, childAsset, out, buffer);
                continue;
            }
            try (java.io.InputStream in = activity.getAssets().open(childAsset);
                 OutputStream os = new FileOutputStream(out)) {
                int read;
                while ((read = in.read(buffer)) != -1) {
                    os.write(buffer, 0, read);
                }
            }
        }
    }

    private static boolean assetHasChildren(Activity activity, String assetPath) throws IOException {
        String[] children = activity.getAssets().list(assetPath);
        return children != null && children.length > 0;
    }

    @SuppressWarnings("deprecation")
    private static long getPackageUpdateStamp(Activity activity) {
        try {
            return activity.getPackageManager()
                    .getPackageInfo(activity.getPackageName(), 0)
                    .lastUpdateTime;
        } catch (Throwable ignored) {
            return 0L;
        }
    }

    private static void requireRuntimeFile(File runtime, String name) {
        if (!new File(runtime, name).isFile()) {
            throw new IllegalStateException("missing runtime asset: " + name);
        }
    }

    private static void deleteChildren(File dir) throws IOException {
        File[] files = dir.listFiles();
        if (files == null) {
            return;
        }
        for (File file : files) {
            if (file.isDirectory()) {
                deleteChildren(file);
            }
            if (!file.delete()) {
                throw new IOException("failed to delete " + file.getAbsolutePath());
            }
        }
    }

    public static void showKeyboard(final boolean show) {
        final Activity activity = sActivity;
        if (activity == null) {
            android.util.Log.i("StArrayInputGate", "imeRequest=" + show + " ignored=noActivity");
            return;
        }
        android.util.Log.i("StArrayInputGate", "imeRequest=" + show
                + " keyboardShown=" + sKeyboardShown
                + " actuallyVisible=" + sKeyboardActuallyVisible
                + " viewFocused=" + (sKeyboardView != null && sKeyboardView.hasFocus()));
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                InputMethodManager imm = (InputMethodManager)
                        activity.getSystemService(Context.INPUT_METHOD_SERVICE);
                if (imm == null) {
                    return;
                }
                long now = SystemClock.uptimeMillis();
                if (show == sKeyboardShown) {
                    if ((!show && !sKeyboardActuallyVisible)
                            || (show && sKeyboardActuallyVisible
                                && sKeyboardView != null && sKeyboardView.hasFocus())) {
                        android.util.Log.i("StArrayInputGate", "imeRequest=" + show
                                + " skipped=alreadyInRequestedState");
                        return;
                    }
                }
                if (show && now - sLastKeyboardRequestUptime < KEYBOARD_REQUEST_MIN_INTERVAL_MS) {
                    android.util.Log.i("StArrayInputGate", "imeRequest=" + show
                            + " skipped=rateLimit");
                    return;
                }
                sLastKeyboardRequestUptime = now;

                if (show) {
                    if (sKeyboardView == null) {
                        sKeyboardView = new KeyboardView(activity);
                        activity.addContentView(sKeyboardView, new ViewGroup.LayoutParams(1, 1));
                    }
                    if (!sKeyboardView.hasFocus()) {
                        sKeyboardView.requestFocus();
                    }
                    sKeyboardShown = true;
                    imm.showSoftInput(sKeyboardView, InputMethodManager.SHOW_IMPLICIT);
                    android.util.Log.i("StArrayInputGate", "imeApplied=show focus="
                            + sKeyboardView.hasFocus());
                } else if (sKeyboardView != null) {
                    imm.hideSoftInputFromWindow(sKeyboardView.getWindowToken(), 0);
                    sKeyboardView.clearFocus();
                    sKeyboardShown = false;
                    android.util.Log.i("StArrayInputGate", "imeApplied=hide");
                }
            }
        });
    }

    private static void installImeVisibilityObserver(Activity activity) {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                View decor = activity.getWindow().getDecorView();
                if (sImeVisibilityDecor == decor) {
                    return;
                }
                sImeVisibilityDecor = decor;
                decor.getViewTreeObserver().addOnGlobalLayoutListener(
                        new ViewTreeObserver.OnGlobalLayoutListener() {
                    @Override
                    public void onGlobalLayout() {
                        WindowInsets insets = decor.getRootWindowInsets();
                        if (insets == null) {
                            return;
                        }
                        boolean visible;
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                            visible = insets.isVisible(WindowInsets.Type.ime());
                        } else {
                            visible = insets.getSystemWindowInsetBottom()
                                    > insets.getStableInsetBottom();
                        }
                        boolean previous = sKeyboardActuallyVisible;
                        sKeyboardActuallyVisible = visible;
                        if (previous != visible) {
                            android.util.Log.i("StArrayInputGate",
                                    "imeActuallyVisible=" + visible
                                            + " requested=" + sKeyboardShown);
                        }
                    }
                });
                decor.requestApplyInsets();
            }
        });
    }

    public static boolean forwardMotionEvent(MotionEvent event) {
        if (!sEnableUi || !sLaunchSucceeded || event == null) {
            return false;
        }
        int pointerIndex = event.getActionIndex();
        if (pointerIndex < 0 || pointerIndex >= event.getPointerCount()) {
            pointerIndex = 0;
        }
        int action = event.getActionMasked();
        int toolType = event.getToolType(pointerIndex);
        int buttonState = event.getButtonState();
        boolean consumed = false;
        if (action == MotionEvent.ACTION_MOVE) {
            int historySize = event.getHistorySize();
            for (int i = 0; i < historySize; i++) {
                consumed |= nativeForwardMotionEvent(
                        action,
                        event.getHistoricalX(pointerIndex, i),
                        event.getHistoricalY(pointerIndex, i),
                        toolType,
                        buttonState);
            }
        }
        consumed |= nativeForwardMotionEvent(
                action,
                event.getX(pointerIndex),
                event.getY(pointerIndex),
                toolType,
                buttonState);
        return consumed;
    }

    public static void observeGameplayMotionEvent(
            MotionEvent event, int viewportWidth, int viewportHeight) {
        if (!sLaunchSucceeded || event == null) {
            return;
        }
        int pointerIndex = event.getActionIndex();
        if (pointerIndex < 0 || pointerIndex >= event.getPointerCount()) {
            pointerIndex = 0;
        }
        int pointerId = event.getPointerCount() > 0 ? event.getPointerId(pointerIndex) : -1;
        nativeObserveGameplayMotionEvent(
                event.getActionMasked(),
                pointerId,
                event.getPointerCount(),
                event.getEventTime(),
                event.getPointerCount() > 0 ? event.getX(pointerIndex) : 0.0f,
                event.getPointerCount() > 0 ? event.getY(pointerIndex) : 0.0f,
                viewportWidth,
                viewportHeight,
                event.getSource(),
                event.getDeviceId(),
                event.getFlags());
    }

    public static void observeGameplayKeyEvent(KeyEvent event) {
        if (!sLaunchSucceeded || event == null) {
            return;
        }
        if (event.getDeviceId() == KeyCharacterMap.VIRTUAL_KEYBOARD) {
            return;
        }
        nativeObserveGameplayKeyEvent(
                event.getAction(),
                event.getKeyCode(),
                event.getScanCode(),
                event.getMetaState(),
                event.getDeviceId(),
                event.getRepeatCount(),
                event.getEventTime(),
                event.getSource(),
                event.getFlags());
    }

    public static native int nativeSetEnv(String key, String value);
    public static native boolean nativeHasCapability(long capability);
    public static native boolean nativeVerifyRuntimeManifest(String runtimeRoot);
    public static native String nativeRuntimeManifestRoot();
    public static native int nativeInitRuntime(String runtimeDir, String entryPointDll, int localDateTimeOffset);
    public static native int nativeExecEntryPointWithArgs(
            String entryPointDll,
            String assemblyName,
            String typeName,
            String methodName,
            String[] args);
    public static native void nativeFreeNativeResources();
    public static native void nativeSendChar(int unicode);
    public static native void nativeSendKey(int keyCode);
    public static native void nativeSetOverlayVisible(boolean visible);
    public static native void nativeSetModalInputCapture(boolean active);
    public static native void nativeRequestModalClose();
    public static native int nativeTakeModalCloseRequest();
    public static native boolean nativeForwardMotionEvent(
            int action, float x, float y, int toolType, int buttonState);
    public static native void nativeObserveGameplayMotionEvent(
            int action,
            int pointerId,
            int pointerCount,
            long eventTimeMillis,
            float x,
            float y,
            int viewportWidth,
            int viewportHeight,
            int source,
            int deviceId,
            int flags);
    public static native void nativeObserveGameplayKeyEvent(
            int action,
            int keyCode,
            int scanCode,
            int metaState,
            int deviceId,
            int repeatCount,
            long eventTimeMillis,
            int source,
            int flags);
    public static native void nativeSetExternalInputDevices(int flags);

    private static final class KeyboardView extends View {
        KeyboardView(Context context) {
            super(context);
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
                            nativeSendChar((int)c);
                        }
                    }
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
                        int unicode = event.getUnicodeChar(event.getMetaState());
                        if (unicode != 0) {
                            nativeSendChar(unicode);
                            return true;
                        }
                    }
                    return super.sendKeyEvent(event);
                }
            };
        }
    }
}
