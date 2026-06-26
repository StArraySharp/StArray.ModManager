package starray.android.modmanager;

import android.app.Activity;
import android.app.AlarmManager;
import android.app.AlertDialog;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
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
            if (Files.exists(localDll) && Files.exists(localVerFile))
                localVersion = parseVersionJson(Files.readString(localVerFile));
        } catch (IOException ignored) {}

        boolean hasLocal = localVersion != null;

        // 有本地版本 → 先启动，后台检查更新
        if (hasLocal) {
            launch(managerDir, runtimeDir, modsDir);
        }

        CompletableFuture
            .supplyAsync(() -> {
                try { return fetchVersionJson(); }
                catch (UncheckedIOException e) {
                    Log.w(TAG, "Cannot fetch version.json: " + e.getMessage());
                    return null;
                }
            })
            .thenAcceptAsync(remote -> {
                if (remote == null) {
                    if (!hasLocal) showError("无法连接服务器，且本地无可用管理器");
                    return;
                }
                boolean needUpdate = !hasLocal
                    || localVersion.versionCode < remote.versionCode;
                if (!needUpdate) {
                    if (!hasLocal) launch(managerDir, runtimeDir, modsDir);
                    // 有本地且已最新 → 已在上方启动，无需操作
                } else if (hasLocal) {
                    showUpdateDialog(remote, managerDir, runtimeDir, modsDir);
                } else {
                    showDownloadDialog(remote, managerDir, runtimeDir, modsDir, false);
                }
            }, mainHandler::post);
    }

    // ──── 对话框 ────

    private void showUpdateDialog(VersionInfo remote, Path mgr, Path rt, Path mods) {
        new AlertDialog.Builder(activity)
            .setTitle("ModManager")
            .setMessage("有可用更新！\nv" + remote.version + "\n是否更新？")
            .setPositiveButton("更新", (d, w) -> {
                d.dismiss();
                showDownloadDialog(remote, mgr, rt, mods, true);
            })
            .setNegativeButton("否", (d, w) -> {
                d.dismiss();
                launch(mgr, rt, mods);
            })
            .setCancelable(false)
            .show();
    }

    private void showDownloadDialog(VersionInfo remote, Path mgr, Path rt, Path mods, boolean hasLocal) {
        var dialog = new AlertDialog.Builder(activity)
            .setTitle(hasLocal ? "更新中" : "下载中")
            .setMessage("正在下载...")
            .setCancelable(hasLocal)
            .create();
        if (!hasLocal) {
            dialog.setCancelable(false);
            dialog.setCanceledOnTouchOutside(false);
        }
        dialog.show();

        CompletableFuture
            .supplyAsync(() -> downloadAndExtractSync(remote, mgr, dialog))
            .thenAcceptAsync(dir -> {
                dialog.dismiss();
                restartApp();
            }, mainHandler::post)
            .exceptionally(ex -> {
                var msg = ex.getCause() != null ? ex.getCause().getMessage() : ex.getMessage();
                if (msg == null) msg = "下载失败";
                dialog.dismiss();
                if (hasLocal) launch(mgr, rt, mods);
                else showError(msg);
                return null;
            });
    }

    private void showError(String message) {
        new AlertDialog.Builder(activity)
            .setTitle("错误")
            .setMessage(message)
            .setPositiveButton("确定", (d, w) -> d.dismiss())
            .show();
    }

    // ──── 下载（同步，后台线程调用） ────

    private Path downloadAndExtractSync(VersionInfo version, Path targetDir, AlertDialog dialog) {
        try {
            Files.createDirectories(targetDir);
            var zipFile = targetDir.getParent().resolve("manager-download.zip");

            updateDialog(dialog, "正在下载...", -1);
            downloadFile(version.managerUrl, zipFile,
                pct -> updateDialog(dialog, "正在下载 " + pct + "%", pct));

            if (version.sha256 != null && !version.sha256.isEmpty()) {
                updateDialog(dialog, "校验中...", -1);
                var actual = sha256(zipFile);
                if (!actual.equalsIgnoreCase(version.sha256)) {
                    Files.delete(zipFile);
                    throw new IOException("SHA-256 mismatch");
                }
            }

            updateDialog(dialog, "正在解压...", -1);
            clearDirectory(targetDir.toFile());
            unzip(zipFile, targetDir);

            var marker = "{\"version\":\"" + version.version +
                "\",\"versionCode\":" + version.versionCode + "}";
            Files.writeString(targetDir.resolve("version.json"), marker);

            Files.delete(zipFile);
            Log.i(TAG, "Manager v" + version.version + " installed");
            return targetDir;
        } catch (IOException e) {
            throw new UncheckedIOException(e);
        }
    }

    // ──── 启动 ────

    private void launch(Path mgr, Path rt, Path mods) {
        try {
            if (!Files.exists(mods)) Files.createDirectories(mods);
            new ModManager()
                .dotnetRoot(rt.toString())
                .addAssemblyDir(mgr.toAbsolutePath().toString())
                .start("StArray.ModManager.dll",
                       "StArray.ModManager.Managed", "Entry",
                       mods.toAbsolutePath().toString());
            Log.i(TAG, "ModManager started");
        } catch (IOException e) {
            throw new UncheckedIOException(e);
        }
    }

    // ──── 远程版本 ────

    private VersionInfo fetchVersionJson() {
        try {
            return parseVersionJson(httpGetString(versionJsonUrl));
        } catch (IOException e) {
            throw new UncheckedIOException(e);
        }
    }

    private static class VersionInfo {
        String version;
        int versionCode;
        String managerUrl;
        String sha256;
    }

    private static VersionInfo parseVersionJson(String json) {
        var v = new VersionInfo();
        v.version = extractJsonString(json, "version");
        v.versionCode = Integer.parseInt(extractJsonString(json, "versionCode", "0"));
        v.managerUrl = extractJsonString(json, "manager");
        v.sha256 = extractJsonString(json, "sha256", "");
        return v;
    }

    // ──── HTTP ────

    private static String httpGetString(String urlStr) throws IOException {
        var conn = openConnection(urlStr, 15_000, 15_000);
        try (var in = conn.getInputStream()) {
            return new String(in.readAllBytes());
        } finally { conn.disconnect(); }
    }

    @FunctionalInterface
    private interface ProgressCallback { void onProgress(int percent); }

    private static void downloadFile(String urlStr, Path dest, ProgressCallback cb) throws IOException {
        var conn = openConnection(urlStr, 30_000, 120_000);
        int cl = conn.getContentLength();
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
        } finally { conn.disconnect(); }
    }

    private static HttpURLConnection openConnection(String urlStr, int cto, int rto) throws IOException {
        var conn = (HttpURLConnection) new URL(urlStr).openConnection();
        conn.setRequestMethod("GET");
        conn.setConnectTimeout(cto);
        conn.setReadTimeout(rto);
        conn.setInstanceFollowRedirects(true);
        return conn;
    }

    // ──── 文件 ────

    private static void unzip(Path zip, Path targetDir) throws IOException {
        try (var zis = new ZipInputStream(new BufferedInputStream(Files.newInputStream(zip)))) {
            ZipEntry entry;
            while ((entry = zis.getNextEntry()) != null) {
                var f = targetDir.resolve(entry.getName());
                if (entry.isDirectory()) Files.createDirectories(f);
                else { Files.createDirectories(f.getParent());
                       Files.copy(zis, f, StandardCopyOption.REPLACE_EXISTING); }
                zis.closeEntry();
            }
        }
    }

    private static String sha256(Path file) throws IOException {
        try {
            var md = MessageDigest.getInstance("SHA-256");
            try (var in = Files.newInputStream(file)) {
                byte[] buf = new byte[8192]; int read;
                while ((read = in.read(buf)) != -1) md.update(buf, 0, read);
            }
            var sb = new StringBuilder();
            for (byte b : md.digest()) sb.append(String.format("%02x", b));
            return sb.toString();
        } catch (NoSuchAlgorithmException e) { throw new IOException(e); }
    }

    private static void clearDirectory(File dir) {
        var files = dir.listFiles();
        if (files != null) for (var f : files) {
            if (f.isDirectory()) clearDirectory(f);
            f.delete();
        }
    }

    // ──── 重启 ────

    private void restartApp() {
        var intent = activity.getPackageManager()
            .getLaunchIntentForPackage(activity.getPackageName());
        if (intent == null) {
            Process.killProcess(Process.myPid());
            return;
        }
        int flags = PendingIntent.FLAG_ONE_SHOT | PendingIntent.FLAG_IMMUTABLE;
        var pending = PendingIntent.getActivity(activity, 0, intent, flags);
        var alarm = (AlarmManager) activity.getSystemService(Context.ALARM_SERVICE);
        alarm.set(AlarmManager.RTC, System.currentTimeMillis() + 200, pending);
        Process.killProcess(Process.myPid());
    }

    private static void updateDialog(AlertDialog d, String msg, int pct) {
        mainHandler.post(() -> { if (d.isShowing()) d.setMessage(msg); });
    }

    private static final Handler mainHandler = new Handler(Looper.getMainLooper());

    // ──── JSON ────

    private static String extractJsonString(String json, String key) {
        return extractJsonString(json, key, null);
    }

    private static String extractJsonString(String json, String key, String def) {
        var p = "\"" + key + "\"";
        int s = json.indexOf(p);
        if (s < 0) return def;
        s = json.indexOf('"', s + p.length());
        if (s < 0) return def;
        int e = json.indexOf('"', s + 1);
        return e < 0 ? def : json.substring(s + 1, e);
    }
}
