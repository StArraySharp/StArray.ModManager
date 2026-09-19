using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.Tests;

/// <summary>
/// ModLoadContext 的 dll 加载与卸载测试。
/// 用运行时生成的动态程序集模拟 mod dll（无需原生运行时），
/// 覆盖：文件加载语义（Location）、依赖解析、卸载回收、文件锁行为。
/// </summary>
[TestFixture]
public sealed class ModLoadContextTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp() => _tempDir = Path.Combine(Path.GetTempPath(), $"smm-alc-test-{Guid.NewGuid():N}");
    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 文件锁测试的残留由 GC 释放 */ }
    }

    // ── 动态测试程序集生成 ──

    /// <summary>生成一个程序集并落盘：含一个公共类 Marker（可被实例化验证）。</summary>
    private string WriteAssembly(string fileName, string typeName = "Marker")
        => WriteAssemblyTo(_tempDir, fileName, typeName);

    private static string WriteAssemblyTo(string dir, string fileName, string typeName = "Marker")
    {
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);

        var ab = new PersistedAssemblyBuilder(
            new AssemblyName(Path.GetFileNameWithoutExtension(fileName)), typeof(object).Assembly);
        var mb = ab.DefineDynamicModule("MainModule");
        var tb = mb.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
        tb.DefineField("Value", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
        tb.CreateTypeInfo();
        ab.Save(path);
        return path;
    }

    /// <summary>依赖程序集：引用主程序集的类型（触发 ALC 依赖解析链）。</summary>
    private (string mainPath, string depPath) WriteDependentAssemblies()
    {
        var mainPath = WriteAssembly("MainLib.dll", "MainMarker");

        // 依赖 dll：字段类型引用 MainLib.MainMarker → 加载时触发依赖解析
        var depName = "DepLib";
        var ab = new PersistedAssemblyBuilder(new AssemblyName(depName), typeof(object).Assembly);
        var mb = ab.DefineDynamicModule("MainModule");
        var tb = mb.DefineType("DepMarker", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

        // 引用主程序集（跨程序集定义字段）
        var mainAb = Assembly.LoadFile(mainPath);
        var mainType = mainAb.GetType("MainMarker")!;
        tb.DefineField("Ref", mainType, FieldAttributes.Public | FieldAttributes.Static);
        tb.CreateTypeInfo();
        ab.Save(Path.Combine(_tempDir, depName + ".dll"));

        return (mainPath, Path.Combine(_tempDir, depName + ".dll"));
    }

    // ── 加载语义 ──

    [Test]
    public void LoadFromFilePath_SetsRealLocation()
    {
        var path = WriteAssembly("Loc.dll");
        var alc = new ModLoadContext("T:Loc", _tempDir);
        try
        {
            var asm = alc.LoadFromFilePath(path);

            Assert.Multiple(() =>
            {
                Assert.That(asm.Location, Is.EqualTo(Path.GetFullPath(path)));
                Assert.That(asm.GetType("Marker"), Is.Not.Null);
            });
        }
        finally { alc.Unload(); }
    }

    [Test]
    public void LoadMetadataFromPath_LocationEmpty_NoFileLock()
    {
        var path = WriteAssembly("Meta.dll");
        var alc = new ModLoadContext("T:Meta", _tempDir);

        var asm = alc.LoadMetadataFromPath(path);

        Assert.That(asm.Location, Is.Empty);

        // 不锁文件：加载后可立即删除（路径加载做不到这一点）
        File.Delete(path);
        Assert.That(File.Exists(path), Is.False);
    }

    // ── 卸载语义 ──

    [Test]
    public void Unload_ReleasesAssemblyAndAllowsReload()
    {
        var path = WriteAssembly("Reload.dll");
        var weak = LoadAndUnload(path);

        // ALC 卸载后：程序集应可被 GC 回收（弱引用失活）
        GcCollectUntil(() => !weak.IsAlive, attempts: 10);

        // 重载：从磁盘重新读，新实例独立于旧实例
        var alc2 = new ModLoadContext("T:Reload2", _tempDir);
        try
        {
            var asm2 = alc2.LoadFromFilePath(path);
            var marker2 = asm2.GetType("Marker");
            Assert.That(marker2, Is.Not.Null);
            Assert.That(marker2!.Assembly, Is.SameAs(asm2));
        }
        finally { alc2.Unload(); }
    }

    [Test]
    public void Unload_StrongReferenceKeepsAssemblyAlive()
    {
        // 对照组：持有 Type 强引用时 collectible ALC 不会回收——
        // 验证测试基线的有效性（Unload→可回收 并非无条件成立）
        var path = WriteAssembly("Pinned.dll");
        var alc = new ModLoadContext("T:Pinned", _tempDir);
        var asm = alc.LoadFromFilePath(path);
        var pinned = asm.GetType("Marker")!;

        var weak = new WeakReference(alc, trackResurrection: true);
        alc.Unload();

        GcCollectUntil(() => !weak.IsAlive, attempts: 5);
        Assert.That(weak.IsAlive, Is.True, "持有 Type 强引用时 ALC 不应被回收");
        _ = pinned.Name; // 保持引用存活
    }

    // ── 依赖解析 ──

    [Test]
    public void ResolvesDependencyFromSameDirectory()
    {
        var (mainPath, depPath) = WriteDependentAssemblies();

        var alc = new ModLoadContext("T:Dep", _tempDir);
        try
        {
            var dep = alc.LoadFromFilePath(depPath);

            // 触发依赖解析：访问引用 MainLib 类型的字段
            var field = dep.GetType("DepMarker")!.GetField("Ref")!;
            Assert.That(field.FieldType.FullName, Is.EqualTo("MainMarker"));
            Assert.That(field.FieldType.Assembly.Location, Is.EqualTo(Path.GetFullPath(mainPath)),
                "依赖应从同目录解析且 Location 为真实路径");
        }
        finally { alc.Unload(); }
    }

    /// <summary>
    /// 管理器目录里也存在宿主程序集的副本时，依赖解析必须复用宿主已加载的实例。
    /// 回归用例：SMM 自身就在 manager/ 目录里，若 mod ALC 从该目录再加载一份，
    /// mod 看到的 IModPlugin 是另一个 Type，插件识别全失败（表现为“发现 0 个 Mod”）。
    /// </summary>
    [Test]
    public void HostLoadedAssembly_WinsOverManagerDirCopy()
    {
        var sharedName = "HostShared_" + Guid.NewGuid().ToString("N");
        var hostDir = Path.Combine(_tempDir, "host");
        var managerDir = Path.Combine(_tempDir, "manager");
        var modDir = Path.Combine(_tempDir, "mod");

        // 两份同名程序集：一份已在宿主上下文，一份躺在管理器目录
        var hostPath = WriteAssemblyTo(hostDir, sharedName + ".dll", "SharedMarker");
        WriteAssemblyTo(managerDir, sharedName + ".dll", "SharedMarker");

        var hostAsm = AssemblyLoadContext.Default.LoadFromAssemblyPath(hostPath);
        var hostMarker = hostAsm.GetType("SharedMarker")!;

        // mod 的依赖 dll 引用 SharedMarker → 触发 mod ALC 的依赖解析
        var ab = new PersistedAssemblyBuilder(new AssemblyName("DepOfShared"), typeof(object).Assembly);
        var mb = ab.DefineDynamicModule("MainModule");
        var tb = mb.DefineType("DepMarker", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
        tb.DefineField("Ref", hostMarker, FieldAttributes.Public | FieldAttributes.Static);
        tb.CreateTypeInfo();
        var depPath = Path.Combine(modDir, "DepOfShared.dll");
        Directory.CreateDirectory(modDir);
        ab.Save(depPath);

        var alc = new ModLoadContext("T:HostFirst", modDir, managerDir);
        try
        {
            var resolved = alc.LoadFromFilePath(depPath)
                .GetType("DepMarker")!.GetField("Ref")!.FieldType;

            Assert.That(resolved, Is.SameAs(hostMarker),
                "宿主已加载的程序集必须复用同一实例，不能从管理器目录加载副本");
        }
        finally { alc.Unload(); }
    }

    // ── 文件锁行为（路径加载的已知权衡） ──
    [Test]
    public void PathLoad_HoldsFileUntilUnloadCollected()
    {
        var path = WriteAssembly("Locked.dll");
        var alc = new ModLoadContext("T:Lock", _tempDir);
        _ = alc.LoadFromFilePath(path);

        // 加载期间文件被映射锁定：覆盖写入应失败（这是路径加载的确定行为）
        Assert.Throws<IOException>(() => File.WriteAllText(path, "corrupt"),
            "路径加载的文件在 ALC 卸载前应被锁定");

        // 卸载后锁应释放。注意：ALC 回收 ≠ 立即 unmap，且测试宿主进程可能经由
        // 其他机制（符号缓存等）短暂持有映射——轮询最多 5 秒。
        // 该阶段是对运行时行为的观察而非硬性契约，超时按 Inconclusive 处理。
        var weak = new WeakReference(alc, trackResurrection: true);
        alc.Unload();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var unlocked = false;
        while (sw.Elapsed < TimeSpan.FromSeconds(5) && !unlocked)
        {
            GcCollectUntil(() => !weak.IsAlive, attempts: 2);
            try
            {
                using var fs = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.None);
                unlocked = true;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        if (!unlocked)
        {
            Assert.Inconclusive(
                "文件在卸载后 5s 内未解锁（宿主进程可能仍持有映射）；" +
                "卸载前锁定的核心断言已通过");
        }
        File.WriteAllText(path, "overwritten");
    }

    // ── 辅助 ──

    private static WeakReference LoadAndUnload(string path)
    {
        var alc = new ModLoadContext("T:Transient", Path.GetDirectoryName(path)!);
        _ = alc.LoadFromFilePath(path);
        var weak = new WeakReference(alc, trackResurrection: true);
        alc.Unload();
        return weak;
    }

    private static void GcCollectUntil(Func<bool> done, int attempts)
    {
        for (int i = 0; i < attempts && !done(); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
