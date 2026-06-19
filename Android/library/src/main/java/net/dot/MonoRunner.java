// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot;

import android.content.Context;
import android.content.res.AssetManager;
import android.util.Log;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;

/**
 * MonoRunner — 简化的 .NET CoreCLR 运行时封装。
 * JNI native 方法实现在 {@code monodroid-coreclr.c} (libmonodroid.so)。
 *
 * <pre>
 *   MonoRunner
 *       .addManagedDir("/sdcard/Runtime/lib")
 *       .addManagedDir("/sdcard/Runtime/mods")
 *       .addNativeDir("/sdcard/Runtime/native")
 *       .start(context, "/sdcard/Runtime", "ModLoader.dll",
 *              "StArray.ModLoader.Mono::Entry");
 *   int ret = MonoRunner.run("ModLoader.dll",
 *              new String[]{"--loader-dir", "/sdcard/Runtime"});
 *   MonoRunner.stop();
 * </pre>
 */
public final class MonoRunner {

    private static final String TAG = "StArray.MonoRunner";

    // ========================================================================
    // Static config (set before start)
    // ========================================================================
    private static final ArrayList<String> sManagedDirs = new ArrayList<>();
    private static final ArrayList<String> sNativeDirs   = new ArrayList<>();

    private static String s_entryPointDll;
    private static String s_entryPointMethod;
    private static boolean s_initialized;

    static {
        System.loadLibrary("System.Security.Cryptography.Native.Android");
        System.loadLibrary("monodroid");
        System.loadLibrary("modloader");
        System.loadLibrary("cimgui");
    }

    // ========================================================================
    // Fluent config API
    // ========================================================================

    /**
     * 添加托管 DLL 搜索目录。
     * 导出为环境变量 {@code TRUSTED_PLATFORM_ASSEMBLIES + MODLOADER_MANAGED_DIRS}。
     */
    public static MonoRunner addManagedDir(String dir) {
        if (!sManagedDirs.contains(dir)) sManagedDirs.add(dir);
        return null; // fluent no-op — all config is static
    }

    /**
     * 添加原生库 .so 搜索目录。
     * 导出为环境变量 {@code NATIVE_DLL_SEARCH_DIRECTORIES + MODLOADER_NATIVE_DIRS}。
     */
    public static MonoRunner addNativeDir(String dir) {
        if (!sNativeDirs.contains(dir)) sNativeDirs.add(dir);
        return null;
    }

    // ========================================================================
    // Init / Run / Stop
    // ========================================================================

    /**
     * 初始化 .NET CoreCLR 运行时 + 指定自定义入口方法。
     *
     * @param context          Android Context
     * @param runtimeDir       Runtime 文件根目录
     * @param entryPointDll    入口程序集名，如 "ModLoader.dll"
     * @param entryPointMethod 入口方法 "Namespace.Type::Method"，
     *                         如 "StArray.ModLoader.Mono::Entry"
     */
    public static MonoRunner start(Context context, String runtimeDir,
                                   String entryPointDll, String entryPointMethod) {
        s_entryPointDll = entryPointDll;
        s_entryPointMethod = entryPointMethod;

        // 默认搜索路径
        sManagedDirs.add(runtimeDir);
        sManagedDirs.add(runtimeDir + "/lib");

        Log.i(TAG, "Starting CoreCLR: dir=" + runtimeDir
                + " entry=" + entryPointDll
                + (entryPointMethod != null ? " method=" + entryPointMethod : "")
                + " managed=" + sManagedDirs
                + " native=" + sNativeDirs);

        String filesDir = context.getFilesDir().getAbsolutePath();
        String cacheDir = context.getCacheDir().getAbsolutePath();

        // 解压 assets 到内部存储
        //extractRuntimeFiles(context, filesDir);

        // ---- 基础环境变量 ----
        setEnv("HOME", runtimeDir);
        setEnv("TMPDIR", cacheDir);
        setEnv("DOTNET_ROOT", runtimeDir);
        setEnv("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        // ---- 托管 DLL 搜索路径 ----
        if (!sManagedDirs.isEmpty()) {
            String paths = join(sManagedDirs, ":");
            setEnv("TRUSTED_PLATFORM_ASSEMBLIES", paths);
            setEnv("APP_PATHS", paths);
            setEnv("MODLOADER_MANAGED_DIRS", paths);
        }

        // ---- 原生 .so 搜索路径 ----
        if (!sNativeDirs.isEmpty()) {
            String paths = join(sNativeDirs, ":");
            setEnv("NATIVE_DLL_SEARCH_DIRECTORIES", paths);
            setEnv("MODLOADER_NATIVE_DIRS", paths);
        }

        // ---- Initialize CoreCLR ----
        int rv = initRuntime(runtimeDir, entryPointDll, 0);
        if (rv != 0) {
            Log.e(TAG, "CoreCLR init failed: code=" + rv);
            throw new RuntimeException("CoreCLR init returned " + rv);
        }
        s_initialized = true;
        Log.i(TAG, "CoreCLR initialized");
        return null;
    }

    /**
     * 初始化 CoreCLR（使用默认 {@code Program.Main} 入口）。
     */
    public static MonoRunner start(Context context, String runtimeDir, String entryPointDll) {
        return start(context, runtimeDir, entryPointDll, null);
    }

    /**
     * 执行入口程序集并传入命令行参数。
     * C# 侧通过 {@code Environment.GetCommandLineArgs()} 获取。
     *
     * @param entryPointDll 入口程序集名
     * @param args          命令行参数
     * @return 程序退出码
     */
    /**
     * 通过 coreclr_create_delegate 直接调用托管方法。
     * @param entryPointDll 程序集文件（如 "ModLoader.dll"）
     * @param typeName      完整类型名（如 "StArray.ModLoader.Mono"）
     * @param methodName    方法名（如 "Entry"）
     */
    public static int run(String entryPointDll, String typeName, String methodName) {
        Log.i(TAG, "Exec delegate: " + entryPointDll
                + " → " + typeName + "::" + methodName);
        int ret = execEntryPoint(entryPointDll, entryPointDll, typeName, methodName);
        Log.i(TAG, "Returned: " + ret);
        return ret;
    }

    /** Convenience: run with default "StArray.ModLoader.Mono::Entry". */
    public static int run(String entryPointDll) {
        return run(entryPointDll, "StArray.ModLoader.Mono", "Entry");
    }

    /** 关闭 CoreCLR 并释放原生资源。 */
    public static void stop() {
        if (!s_initialized) return;
        Log.i(TAG, "Shutting down CoreCLR");
        freeNativeResources();
        s_initialized = false;
    }

    // ========================================================================
    // File extraction
    // ========================================================================

    /** 从 APK assets 递归解压所有文件到 destDir。 */
    public static void extractRuntimeFiles(Context context, String destDir) {
        try { copyAssetsRecursive(context.getAssets(), "", destDir); }
        catch (IOException e) { Log.e(TAG, "extract failed", e); }
    }

    private static void copyAssetsRecursive(AssetManager am, String path,
                                            String destDir) throws IOException {
        String[] items = am.list(path);
        if (items == null || items.length == 0) {
            try (InputStream in = am.open(path);
                 FileOutputStream out = new FileOutputStream(new File(destDir, path))) {
                byte[] buf = new byte[8192];
                int len;
                while ((len = in.read(buf)) > 0) out.write(buf, 0, len);
            }
            return;
        }
        for (String item : items) {
            copyAssetsRecursive(am, path.isEmpty() ? item : path + "/" + item, destDir);
        }
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static String join(ArrayList<String> list, String sep) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < list.size(); i++) {
            if (i > 0) sb.append(sep);
            sb.append(list.get(i));
        }
        return sb.toString();
    }

    // ========================================================================
    // Native (monodroid-coreclr.c)
    // ========================================================================

    public static native int setEnv(String key, String value);
    public static native int initRuntime(String libsDir, String entryPointLibName, int localDateTimeOffset);
    /** coreclr_create_delegate + call */
    public static native int execEntryPoint(String entryPointLibName,
        String assemblyName, String typeName, String methodName);
    public static native void freeNativeResources();
}
