package starray.android.modmanager;

import android.app.Activity;
import android.app.AlarmManager;
import android.app.AlertDialog;
import android.app.PendingIntent;
import android.content.Context;
import android.os.Handler;
import android.os.Looper;
import android.os.Process;
import android.util.Log;

import java.io.BufferedInputStream;
import java.io.File;
import java.io.IOException;
import java.io.UncheckedIOException;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.concurrent.CompletableFuture;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

/**
 * Mod 管理器自动更新与启动，内置对话框。
 *
 * <pre>
 * ModManagerUpdater.create(activity)
 *     .versionJsonUrl("https://.../version.json")
 *     .basePath("/sdcard/ADOFAI/ModManager")
 *     .start();
 * </pre>
 */
public class ModManagerUpdater {

    private static final String TAG = "ModManagerUpdater";

    private final Activity activity;
    private String versionJsonUrl;
    private Path basePath;

    public static ModManagerUpdater create(Activity activity) {
        return new ModManagerUpdater(activity);
    }

    private ModManagerUpdater(Activity activity) {
        this.activity = activity;
    }

    public ModManagerUpdater versionJsonUrl(String url) {
        this.versionJsonUrl = url;
        return this;
    }

    public ModManagerUpdater basePath(String path) {
        this.basePath = Paths.get(path);
        return this;
    }

    public ModManagerUpdater basePath(Path path) {
        this.basePath = path;
        return this;
    }

    // ──── 入口 ────

    public void start() {
        start(true);
    }

    /**
     * @param useNewThread true=在新线程启动 ModManager，false=当前线程
     */
    public void start(boolean useNewThread) {
        if (versionJsonUrl == null || versionJsonUrl.isEmpty())
            throw new IllegalStateException("versionJsonUrl not set");
        if (basePath == null)
            throw new IllegalStateException("basePath not set");

        var managerDir = basePath.resolve("manager");
        var runtimeDir = basePath.resolve("runtime");
        var modsDir = basePath.resolve("mods");
        var localDll = managerDir.resolve("StArray.ModManager.dll");
        var localVerFile = managerDir.resolve("version.json");


        VersionInfo localVersion = null;
        try {
            if (Files.exists(localDll) && Files.exists(localVerFile)) {
                localVersion = parseVersionJson(new String(Files.readAllBytes(localVerFile)));
                Log.i(TAG, "Local version: " + localVersion.version + " (code=" + localVersion.versionCode + ")");
            } else {
                Log.i(TAG, "No local manager found");
            }
        } catch (IOException e) {
            Log.e(TAG, "Failed to read local version: " + e.getMessage());
        }

        boolean hasLocal = localVersion != null;

        // 有本地版本 → 先启动，后台检查更新
        if (hasLocal) {
            Log.i(TAG, "Launching existing manager v" + localVersion.version);
            launch(managerDir, runtimeDir, modsDir, useNewThread);
        } else {
            Log.i(TAG, "No local manager found, will download");
        }

        VersionInfo finalLocalVersion = localVersion;
        CompletableFuture
                .supplyAsync(() -> {
                    Log.i(TAG, "Fetching remote version.json...");
                    try {
                        return fetchVersionJson();
                    } catch (Exception e) {
                        Log.e(TAG, "Cannot fetch version.json: " + e.getMessage());
                        return null;
                    }
                })
                .thenAccept(remote -> {
                    if (remote == null) {
                        Log.w(TAG, "Remote version check failed (network error)");
                        if (!hasLocal)
                            activity.runOnUiThread(() -> showError("无法连接服务器，且本地无可用管理器"));
                        return;
                    }
                    Log.i(TAG, "Remote version: " + remote.version + " (code=" + remote.versionCode + ")");
                    boolean needUpdate = !hasLocal
                            || finalLocalVersion.versionCode < remote.versionCode;
                    Log.i(TAG, "needUpdate=" + needUpdate + " hasLocal=" + hasLocal);
                    if (!needUpdate) {
                        Log.i(TAG, "Manager is up to date");
                        if (!hasLocal) launch(managerDir, runtimeDir, modsDir, useNewThread);
                    } else if (hasLocal) {
                        Log.i(TAG, "Update available: v" + finalLocalVersion.version + " → v" + remote.version);
                        activity.runOnUiThread(() -> showUpdateDialog(remote, managerDir, runtimeDir, modsDir, useNewThread));
                    } else {
                        Log.i(TAG, "First install: downloading v" + remote.version);
                        activity.runOnUiThread(() -> showDownloadDialog(remote, managerDir, runtimeDir, modsDir, false, useNewThread));
                    }
                })
                .exceptionally(ex -> {
                    Log.e(TAG, "Unexpected error in update check", ex);
                    return null;
                });
    }

    // ──── 对话框 ────

    private void showUpdateDialog(VersionInfo remote, Path mgr, Path rt, Path mods, boolean useNewThread) {
        Log.i(TAG, "Showing update dialog for v" + remote.version);
        new AlertDialog.Builder(activity)
                .setTitle("ModManager")
                .setMessage("有可用更新！\nv" + remote.version + "\n是否更新？")
                .setPositiveButton("更新", (d, w) -> {
                    d.dismiss();
                    showDownloadDialog(remote, mgr, rt, mods, true, useNewThread);
                })
                .setNegativeButton("否", (d, w) -> {
                    d.dismiss();
                    launch(mgr, rt, mods, useNewThread);
                })
                .setCancelable(false)
                .show();
    }

    private void showDownloadDialog(VersionInfo remote, Path mgr, Path rt, Path mods, boolean hasLocal, boolean useNewThread) {
        Log.i(TAG, "Starting download: url=" + remote.managerUrl + " target=" + mgr);
        var dialog = new AlertDialog.Builder(activity)
                .setTitle(hasLocal ? "更新中" : "下载中")
                .setMessage("正在下载...")
                .setCancelable(hasLocal)
                .create();
        if (!hasLocal) {
            dialog.setCancelable(false);
            dialog.setCanceledOnTouchOutside(false);
            Log.i(TAG, "First install: dialog non-cancelable");
        }
        dialog.show();

        CompletableFuture
                .supplyAsync(() -> downloadAndExtractSync(remote, mgr, dialog))
                .thenAccept(dir -> {
                    Log.i(TAG, "Download complete, restarting app");
                    activity.runOnUiThread(() -> {
                        dialog.dismiss();
                        restartApp();
                    });
                })
                .exceptionally(ex -> {
                    var msg = ex.getCause() != null ? ex.getCause().getMessage() : ex.getMessage();
                    if (msg == null) msg = "下载失败";
                    Log.e(TAG, "Download failed: " + msg, ex);
                    String finalMsg = msg;
                    activity.runOnUiThread(() -> {
                        dialog.dismiss();
                        if (hasLocal) {
                            Log.i(TAG, "Falling back to existing manager");
                            launch(mgr, rt, mods, useNewThread);
                        } else showError(finalMsg);
                    });
                    return null;
                });
    }

    private void showError(String message) {
        Log.w(TAG, "Showing error dialog: " + message);
        new AlertDialog.Builder(activity)
                .setTitle("错误")
                .setMessage(message)
                .setPositiveButton("确定", (d, w) -> d.dismiss())
                .show();
    }

    // ──── 下载（同步，后台线程调用） ────

    private Path downloadAndExtractSync(VersionInfo version, Path targetDir, AlertDialog dialog) {
        try {
            Log.i(TAG, "Downloading v" + version.version + " → " + targetDir);
            Files.createDirectories(targetDir);
            var zipFile = targetDir.getParent().resolve("manager-download.zip");

            updateDialog(dialog, "正在下载...", -1);
            downloadFile(version.managerUrl, zipFile,
                    pct -> updateDialog(dialog, "正在下载 " + pct + "%", pct));

            long zipSize = Files.size(zipFile);
            Log.i(TAG, "Downloaded " + zipSize + " bytes");

            if (version.sha256 != null && !version.sha256.isEmpty()) {
                updateDialog(dialog, "校验中...", -1);
                Log.i(TAG, "Verifying SHA-256...");
                var actual = sha256(zipFile);
                if (!actual.equalsIgnoreCase(version.sha256)) {
                    Log.e(TAG, "SHA-256 mismatch! expected=" + version.sha256 + " actual=" + actual);
                    Files.delete(zipFile);
                    throw new IOException("SHA-256 mismatch");
                }
                Log.i(TAG, "SHA-256 OK");
            }

            updateDialog(dialog, "正在解压...", -1);
            clearDirectory(targetDir.toFile());
            unzip(zipFile, targetDir);

            var marker = "{\"version\":\"" + version.version +
                    "\",\"versionCode\":" + version.versionCode + "}";
            Files.write(targetDir.resolve("version.json"), marker.getBytes());

            Files.delete(zipFile);
            Log.i(TAG, "Manager installed successfully");
            return targetDir;
        } catch (IOException e) {
            Log.e(TAG, "downloadAndExtractSync failed: " + e.getMessage(), e);
            throw new UncheckedIOException(e);
        }
    }

    // ──── 启动 ────

    private void launch(Path mgr, Path rt, Path mods, boolean useNewThread) {
        Runnable task = () -> {
            try {
                Log.i(TAG, "Launching ModManager: mgr=" + mgr + " rt=" + rt + " mods=" + mods);
                if (!Files.exists(mods)) {
                    Files.createDirectories(mods);
                    Log.i(TAG, "Created mods directory: " + mods);
                }
                new ModManager()
                        .dotnetRoot(rt.toString())
                        .addAssemblyDir(mgr.toAbsolutePath().toString())
                        .start("StArray.ModManager.dll",
                                "StArray.ModManager.Managed", "Entry",
                                mods.toAbsolutePath().toString());
                Log.i(TAG, "ModManager started");
            } catch (IOException e) {
                Log.e(TAG, "Failed to create mods directory: " + e.getMessage(), e);
                throw new UncheckedIOException(e);
            }
        };
        if (useNewThread) new Thread(task).start();
        else task.run();
    }
    // ──── 远程版本 ────

    private VersionInfo fetchVersionJson () {
        try {
            var json = httpGetString(versionJsonUrl);
            return parseVersionJson(json);
        } catch (IOException e) {
            Log.e(TAG, "Failed to fetch version.json: " + e.getMessage(), e);
            throw new UncheckedIOException(e);
        }
    }

    private static class VersionInfo {
        String version;
        int versionCode;
        String managerUrl;
        String sha256;
    }

    private static VersionInfo parseVersionJson (String json){
        var v = new VersionInfo();
        v.version = extractJsonString(json, "version");
        try {
            v.versionCode = Integer.parseInt(extractJsonString(json, "versionCode", "0"));
        } catch (NumberFormatException e) {
            v.versionCode = 0;
        }
        v.managerUrl = extractJsonString(json, "manager");
        v.sha256 = extractJsonString(json, "sha256", "");
        return v;
    }

    // ──── HTTP ────

    private static String httpGetString (String urlStr) throws IOException {
        var conn = openConnection(urlStr, 15_000, 15_000);
        try (var in = conn.getInputStream();
             var out = new java.io.ByteArrayOutputStream()) {
            byte[] buf = new byte[4096];
            int read;
            while ((read = in.read(buf)) != -1) out.write(buf, 0, read);
            return out.toString("UTF-8");
        } finally {
            conn.disconnect();
        }
    }

    @FunctionalInterface
    private interface ProgressCallback {
        void onProgress(int percent);
    }

    private static void downloadFile (String urlStr, Path dest, ProgressCallback cb) throws
            IOException {
        Log.i(TAG, "Downloading " + urlStr);
        var conn = openConnection(urlStr, 30_000, 120_000);
        int cl = conn.getContentLength();
        Log.i(TAG, "Content-Length: " + cl);
        try (var in = new BufferedInputStream(conn.getInputStream());
             var out = Files.newOutputStream(dest)) {
            byte[] buf = new byte[8192];
            int read, total = 0;
            while ((read = in.read(buf)) != -1) {
                out.write(buf, 0, read);
                total += read;
                if (cl > 0) {
                    int pct = (int) (100L * total / cl);
                    mainHandler.post(() -> cb.onProgress(pct));
                }
            }
            Log.i(TAG, "Download complete: " + total + " bytes");
        } finally {
            conn.disconnect();
        }
    }

    private static HttpURLConnection openConnection (String urlStr,int cto, int rto) throws
            IOException {
        var conn = (HttpURLConnection) new URL(urlStr).openConnection();
        conn.setRequestMethod("GET");
        conn.setConnectTimeout(cto);
        conn.setReadTimeout(rto);
        conn.setInstanceFollowRedirects(true);
        return conn;
    }

    // ──── 文件 ────

    private static void unzip (Path zip, Path targetDir) throws IOException {
        try (var zis = new ZipInputStream(new BufferedInputStream(Files.newInputStream(zip)))) {
            ZipEntry entry;
            while ((entry = zis.getNextEntry()) != null) {
                var f = targetDir.resolve(entry.getName());
                if (entry.isDirectory()) Files.createDirectories(f);
                else {
                    Files.createDirectories(f.getParent());
                    Files.copy(zis, f, StandardCopyOption.REPLACE_EXISTING);
                }
                zis.closeEntry();
            }
        }
    }

    private static String sha256 (Path file) throws IOException {
        try {
            var md = MessageDigest.getInstance("SHA-256");
            try (var in = Files.newInputStream(file)) {
                byte[] buf = new byte[8192];
                int read;
                while ((read = in.read(buf)) != -1) md.update(buf, 0, read);
            }
            var sb = new StringBuilder();
            for (byte b : md.digest()) sb.append(String.format("%02x", b));
            return sb.toString();
        } catch (NoSuchAlgorithmException e) {
            throw new IOException(e);
        }
    }

    private static void clearDirectory (File dir){
        var files = dir.listFiles();
        if (files != null) for (var f : files) {
            if (f.isDirectory()) clearDirectory(f);
            f.delete();
        }
    }

    // ──── 重启 ────

    private void restartApp () {
        Log.i(TAG, "Restarting app...");
        var intent = activity.getPackageManager()
                .getLaunchIntentForPackage(activity.getPackageName());
        if (intent == null) {
            Log.w(TAG, "No launch intent, killing process");
            Process.killProcess(Process.myPid());
            return;
        }
        int flags = PendingIntent.FLAG_ONE_SHOT | PendingIntent.FLAG_IMMUTABLE;
        var pending = PendingIntent.getActivity(activity, 0, intent, flags);
        var alarm = (AlarmManager) activity.getSystemService(Context.ALARM_SERVICE);
        alarm.set(AlarmManager.RTC, System.currentTimeMillis() + 200, pending);
        Log.i(TAG, "Alarm scheduled, killing process");
        Process.killProcess(Process.myPid());
    }

    private static void updateDialog (AlertDialog d, String msg,int pct){
        mainHandler.post(() -> {
            if (d.isShowing()) d.setMessage(msg);
        });
    }

    private static final Handler mainHandler = new Handler(Looper.getMainLooper());

    // ──── JSON ────

    private static String extractJsonString (String json, String key){
        return extractJsonString(json, key, null);
    }

    private static String extractJsonString (String json, String key, String def){
        var p = "\"" + key + "\"";
        int s = json.indexOf(p);
        if (s < 0) return def;
        s = json.indexOf('"', s + p.length());
        if (s < 0) return def;
        int e = json.indexOf('"', s + 1);
        return e < 0 ? def : json.substring(s + 1, e);
    }
}

