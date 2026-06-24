package starray.android.modmanager;

import android.util.Log;
import net.dot.MonoRunner;

import java.io.File;

/**
 * ModManager — 封装 CoreCLR 启动。
 * <pre>
 *   new ModManager()
 *       .dotnetRoot("/sdcard/ModManager/runtime")
 *       .addAssemblyDir("/sdcard/ModManager/loader")
 *       .start("ModManager.dll", "StArray.ModManager.Mono", "Entry");
 * </pre>
 */
public class ModManager {
    private static final String TAG = "StArray.ModManager";

    public ModManager() {}

    public ModManager dotnetRoot(String path)  { MonoRunner.dotnetRoot(path); return this; }
    public ModManager addAssemblyDir(String dir) { MonoRunner.addAssemblyDir(dir); return this; }
    public ModManager addNativeDir(String dir)   { MonoRunner.addNativeDir(dir); return this; }

    public int start(String dll, String type, String method,String... args) {
        Log.i(TAG, "Starting " + dll + " -> " + type + "::" + method);
        if (args != null && args.length > 0) return MonoRunner.run(dll, type, method, args);
        return MonoRunner.run(dll, type, method);
    }

    public void stop() { MonoRunner.stop(); }

    public static void launch() {
        final String runtimeRoot = "/sdcard/ModManager/runtime";
        final String[] assemblyDirs = {
            runtimeRoot,
            "/sdcard/ModManager/plugins",
        };
        final String[] nativeDirs = {
            runtimeRoot,
        };

        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    var manager = new ModManager()
                            .dotnetRoot(runtimeRoot);
                    for (String dir : assemblyDirs)
                        manager.addAssemblyDir(dir);
                    for (String dir : nativeDirs)
                        manager.addNativeDir(dir);
                    manager.start("StArray.ModManager.dll", "StArray.ModManager.Managed", "Entry",new File(runtimeRoot).getParentFile().getAbsolutePath() + "/mods");
                } catch (Exception e) {
                    Log.e(TAG, "launch failed", e);
                }
            }
        }, "ModManager-Main").start();
    }
}
