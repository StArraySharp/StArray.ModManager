package starray.android.modloader;

import android.content.Context;
import android.util.Log;

import net.dot.MonoRunner;

/**
 * StArray ModLoader — Android AAR library entry point.
 * Wraps {@link MonoRunner} for CoreCLR lifecycle.
 *
 * <pre>
 *   int ret = new ModLoader(context, "/sdcard/Runtime", "ModLoader.dll")
 *         .entryMethod("StArray.ModLoader.Mono::Entry")
 *         .addManagedDir("/sdcard/Runtime/mods")
 *         .addNativeDir("/sdcard/Runtime/native")
 *         .start(new String[]{"--verbose"});
 * </pre>
 */
public class ModLoader {
    private static final String TAG = "StArray.ModLoader";

    private final Context context;
    private final String runtimeDir;
    private final String entryPointDll;
    private String entryPointMethod;
    private boolean started;

    public ModLoader(Context context, String runtimeDir, String entryPointDll) {
        this.context = context.getApplicationContext();
        this.runtimeDir = runtimeDir;
        this.entryPointDll = entryPointDll;
    }

    /** 自定义入口方法 "Namespace.Type::Method"。 */
    public ModLoader entryMethod(String method) {
        this.entryPointMethod = method; return this;
    }

    /** 添加托管 DLL 搜索目录。 */
    public ModLoader addManagedDir(String dir) {
        MonoRunner.addManagedDir(dir); return this;
    }

    /** 添加原生 .so 搜索目录。 */
    public ModLoader addNativeDir(String dir) {
        MonoRunner.addNativeDir(dir); return this;
    }

    /**
     * 初始化 CoreCLR + 执行入口（无参）。C# 侧通过 DLL 路径推导根目录。
     * @return managed exit code
     */
    public int start() {
        if (started) { Log.w(TAG, "Already started"); return 0; }
        Log.i(TAG, "Starting: dir=" + runtimeDir
                + " entry=" + entryPointDll);
        MonoRunner.start(context, runtimeDir, entryPointDll, entryPointMethod);
        started = true;
        // 通过 coreclr_create_delegate 调用无参 Entry()
        return MonoRunner.run(entryPointDll);
    }

    public void stop() { if (started) { MonoRunner.stop(); started = false; } }

    public boolean isStarted() { return started; }
    public String getRuntimeDir() { return runtimeDir; }
    public String getEntryPointDll() { return entryPointDll; }
}
