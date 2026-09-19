using System.Reflection;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.Native;

/// <summary>
/// Capstone 内嵌库解析器：程序集内嵌 libcapstone.so（arm64），
/// Gee.External.Capstone 的 DllImport("capstone") 触发解析时按需
/// 解压到 manager/ 目录，再经 <see cref="AndroidUtils.LoadLibrary"/>
/// 复制进 /data/data/{package}/cache、chmod 755 后 dlopen。
/// <para>接线（Managed.Entry）：</para>
/// <code>
/// NativeLibraryResolver.Install(typeof(CapstoneDisassembler).Assembly);
/// NativeLibraryResolver.ResolveRequested += CapstoneLibrary.Resolve;
/// </code>
/// </summary>
public static class CapstoneLibrary
{
    private const string ResourceName = "StArray.ModManager.Android.Resources.libcapstone.so";
    private const string LibName = "libcapstone.so";

    private static IntPtr _handle;
    private static readonly Lock _lock = new();

    /// <summary>NativeLibraryResolver 订阅入口；只处理 "capstone"。</summary>
    public static IntPtr Resolve(string libraryName, Assembly assembly)
    {
        if (!libraryName.Equals("capstone", StringComparison.OrdinalIgnoreCase))
            return IntPtr.Zero;

        if (_handle != IntPtr.Zero) return _handle;
        lock (_lock)
        {
            if (_handle != IntPtr.Zero) return _handle;
            _handle = EnsureLoaded();
            return _handle;
        }
    }

    private static IntPtr EnsureLoaded()
    {
        try
        {
            // 解压目标：与 Android 管理器 dll 同目录（manager/）
            var dir = Path.GetDirectoryName(Managed.AssemblyPath);
            if (string.IsNullOrEmpty(dir))
            {
                Logger.Error(nameof(CapstoneLibrary), "AssemblyPath not set, cannot extract");
                return IntPtr.Zero;
            }

            var dest = Path.Combine(dir, LibName);

            using var stream = typeof(CapstoneLibrary).Assembly
                .GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                Logger.Error(nameof(CapstoneLibrary), $"resource not found: {ResourceName}");
                return IntPtr.Zero;
            }

            // 缺失或长度不一致才解压；先写 .tmp 再原子替换，
            // 避免解压中途进程被杀留下半截 so 被下次启动误加载
            if (!File.Exists(dest) || new FileInfo(dest).Length != stream.Length)
            {
                var tmp = dest + ".tmp";
                using (var fs = File.Create(tmp))
                    stream.CopyTo(fs);
                File.Move(tmp, dest, overwrite: true);
                Logger.Info(nameof(CapstoneLibrary), $"extracted {LibName} ({stream.Length} bytes) -> {dest}");
            }

            // cache 中转 + chmod 755 + dlopen
            return AndroidUtils.LoadLibrary(dest);
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(CapstoneLibrary), $"EnsureLoaded: {ex}");
            return IntPtr.Zero;
        }
    }
}
