package starray.android.modloader;

import android.util.Log;
import net.dot.MonoRunner;

/**
 * ModLoader — 封装 CoreCLR 启动。
 * <pre>
 *   new ModLoader()
 *       .dotnetRoot("/sdcard/ModLoader/runtime")
 *       .addAssemblyDir("/sdcard/ModLoader/loader")
 *       .start("ModLoader.dll", "StArray.ModLoader.Mono", "Entry");
 * </pre>
 */
public class ModLoader {
    private static final String TAG = "StArray.ModLoader";

    public ModLoader() {}

    public ModLoader dotnetRoot(String path)  { MonoRunner.dotnetRoot(path); return this; }
    public ModLoader addAssemblyDir(String dir) { MonoRunner.addAssemblyDir(dir); return this; }
    public ModLoader addNativeDir(String dir)   { MonoRunner.addNativeDir(dir); return this; }

    public int start(String dll, String type, String method) {
        Log.i(TAG, "Starting " + dll + " -> " + type + "::" + method);
        return MonoRunner.run(dll, type, method);
    }

    public void stop() { MonoRunner.stop(); }
}
