// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot;

import android.util.Log;
import java.util.ArrayList;

/**
 * MonoRunner — CoreCLR 启动器。
 * <pre>
 *   MonoRunner.dotnetRoot("/sdcard/ModLoader/runtime")
 *       .addAssemblyDir("/sdcard/ModLoader/loader")
 *       .run("ModLoader.dll", "StArray.ModLoader.Mono", "Entry");
 * </pre>
 */
public final class MonoRunner {

    private static final String TAG = "StArray.MonoRunner";
    private static String s_dotnetRoot;
    private static final ArrayList<String> s_assemblyDirs = new ArrayList<>();
    private static final ArrayList<String> s_nativeDirs = new ArrayList<>();
    private static boolean s_initialized;

    static {
        System.loadLibrary("System.Security.Cryptography.Native.Android");
        System.loadLibrary("monodroid");
        System.loadLibrary("modloader");
    }

    // ========================================================================
    // Public API
    // ========================================================================

    public static MonoRunner dotnetRoot(String path)  { s_dotnetRoot = path; return null; }

    public static MonoRunner addAssemblyDir(String dir) {
        if (!s_assemblyDirs.contains(dir)) s_assemblyDirs.add(dir); return null;
    }

    public static MonoRunner addNativeDir(String dir) {
        if (!s_nativeDirs.contains(dir)) s_nativeDirs.add(dir); return null;
    }

    /**
     * 启动 CoreCLR + delegate 调用入口方法。
     * @param entryDll   入口 DLL 文件名
     * @param typeName   类型全名
     * @param methodName 方法名
     * @return 托管退出码
     */
    public static int run(String entryDll, String typeName, String methodName) {
        if (s_dotnetRoot == null)
            throw new IllegalStateException("dotnetRoot not set");

        Log.i(TAG, "dotnetRoot=" + s_dotnetRoot
                + " entry=" + entryDll
                + " dirs=" + s_assemblyDirs
                + " native=" + s_nativeDirs);

        // 环境变量
        setEnv("DOTNET_ROOT", s_dotnetRoot);
        setEnv("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
        setEnv("HOME", s_dotnetRoot);

        // 托管搜索路径（dotnetRoot 排最前）
        s_assemblyDirs.add(0, s_dotnetRoot);
        String tpa = join(s_assemblyDirs, ":");
        setEnv("TRUSTED_PLATFORM_ASSEMBLIES", tpa);
        setEnv("APP_PATHS", tpa);

        // 原生搜索路径
        if (!s_nativeDirs.isEmpty())
            setEnv("NATIVE_DLL_SEARCH_DIRECTORIES", join(s_nativeDirs, ":"));

        // 初始化
        int rv = initRuntime(s_dotnetRoot, entryDll, 0);
        if (rv != 0)
            throw new RuntimeException("coreclr_initialize: 0x" + Integer.toHexString(rv));
        s_initialized = true;
        Log.i(TAG, "CoreCLR initialized");

        // delegate 调用
        return execEntryPoint(entryDll, entryDll, typeName, methodName);
    }

    public static void stop() {
        if (!s_initialized) return;
        freeNativeResources();
        s_initialized = false;
    }

    // ========================================================================
    // Native
    // ========================================================================
    public static native int setEnv(String key, String value);
    public static native int initRuntime(String libsDir, String entryPointLibName, int localDateTimeOffset);
    public static native int execEntryPoint(String entryPointLibName,
        String assemblyName, String typeName, String methodName);
    public static native void freeNativeResources();

    private static String join(ArrayList<String> list, String sep) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < list.size(); i++) {
            if (i > 0) sb.append(sep);
            sb.append(list.get(i));
        }
        return sb.toString();
    }
}
