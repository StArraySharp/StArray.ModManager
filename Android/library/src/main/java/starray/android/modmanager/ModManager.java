package starray.android.modmanager;

import android.app.Activity;
import android.content.res.AssetManager;
import android.os.Environment;
import android.util.Log;
import net.dot.MonoRunner;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Objects;

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

    private String runtimeDir;
    public ModManager() {}

    public ModManager dotnetRoot(String path)  { MonoRunner.dotnetRoot(path); runtimeDir = path; return this; }
    public ModManager addAssemblyDir(String dir) { MonoRunner.addAssemblyDir(dir); return this; }
    public ModManager addNativeDir(String dir)   { MonoRunner.addNativeDir(dir); return this; }

    public int start(String dll, String type, String method,String... args) {
        extractRuntime();
        Log.i(TAG, "Starting " + dll + " -> " + type + "::" + method);
        if (args != null && args.length > 0) return MonoRunner.run(dll, type, method, args);
        return MonoRunner.run(dll, type, method);
    }

    private void extractRuntime(){
        if (Files.exists(Paths.get(runtimeDir, "System.Private.CoreLib.dll"))){
            return;
        }
        try {
            Activity activity = ModManagerUtils.getUnityActivity();
            String[] dlls = Objects.requireNonNull(activity).getAssets().list("runtime");
            if (!Files.exists(Paths.get(runtimeDir))){
                Files.createDirectories(Paths.get(runtimeDir));
            }
            for (String dll : Objects.requireNonNull(dlls)) {
                var fis = activity.getAssets().open("runtime/" + dll);
                File outputDll = Paths.get(runtimeDir,new File(dll).getName()).toFile();
                outputDll.createNewFile();
                OutputStream out = new FileOutputStream(outputDll);
                {
                    byte[] buffer = new byte[8192];
                    int length;
                    while ((length = fis.read(buffer)) != -1) {
                        out.write(buffer, 0, length);
                    }
                }
                out.close();
            }
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    public void stop() { MonoRunner.stop(); }

    public static void launch() {
        final String esp = Environment.getExternalStorageDirectory().getAbsolutePath();
        final String runtimeRoot = esp + "/ModManager/runtime";
        final String[] assemblyDirs = {
            runtimeRoot,
            esp + "/ModManager/plugins",
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
                    manager.start("StArray.ModManager.dll", "StArray.ModManager.Managed", "Entry", Objects.requireNonNull(new File(runtimeRoot).getParentFile()).getAbsolutePath() + "/mods");
                } catch (Exception e) {
                    Log.e(TAG, "launch failed", e);
                }
            }
        }, "ModManager-Main").start();
    }
}
