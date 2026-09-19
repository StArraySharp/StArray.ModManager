using System.Runtime.InteropServices;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using Gee.External.Capstone.X86;

namespace StArray.ModManager.Runtime.Patching;

/// <summary>
/// Transpiler 风格的 native 代码修补器（类似 Harmony 的 Patch/UnpatchAll 心智模型）：
/// 反汇编（Capstone）→ 备份原字节 → 修改内存 → 可整体还原。
/// <para>用法：</para>
/// <code>
/// using var patcher = new CodePatcher("GameAssembly.dll");
/// patcher.Nop(0x123456, 2);        // nop 掉 2 条指令
/// patcher.SkipFunction(0x130000);  // 函数直接 return 0
/// patcher.RestoreAll();            // 全部还原
/// </code>
/// </summary>
public sealed class CodePatcher : IDisposable
{
    // ── 平台内存 API ──

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(nint address, nuint size, uint newProtect, out uint oldProtect);

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern bool FlushInstructionCache(nint hProcess, nint lpBaseAddress, nuint dwSize);

    [DllImport("libc", SetLastError = true)]
    private static extern int mprotect(nint address, nuint size, int prot);

    [DllImport("libc")]
    private static extern int cacheflush(nint start, nint end, int flags);

    private const uint PAGE_EXECUTE_READWRITE = 0x40;
    private const int PROT_RWX = 7;

    // ── 状态 ──

    private readonly string _module;
    private readonly List<(nint Address, byte[] Original)> _patches = [];
    private bool _disposed;

    public CodePatcher(string module) => _module = module;

    /// <summary>RVA → 绝对地址（委托 HookHelper，复用其模块解析与降级链）。</summary>
    public nint Resolve(long rva) => HookHelper.GetFunctionRVA(_module, rva);

    // ── 反汇编 ──

    /// <summary>反汇编 <paramref name="addr"/> 处最多 <paramref name="maxCount"/> 条指令。</summary>
    public static unsafe AsmInstruction[] Disassemble(nint addr, int maxCount = 64)
    {
        var code = new byte[maxCount * 16]; // 上限缓冲（x64 最长指令 15B，arm64 定长 4B）
        fixed (byte* p = code)
        {
            Buffer.MemoryCopy((void*)addr, p, code.Length, code.Length);
        }

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 or Architecture.X86 => DisassembleX86(addr, code, maxCount),
            Architecture.Arm64 => DisassembleArm64(addr, code, maxCount),
            _ => throw new PlatformNotSupportedException(
                $"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}"),
        };
    }

    /// <summary>反汇编 <paramref name="addr"/> 处直到累计字节数覆盖 <paramref name="minBytes"/>。</summary>
    public static AsmInstruction[] DisassembleAtLeast(nint addr, int minBytes)
    {
        var instructions = Disassemble(addr, 64);
        var taken = instructions.TakeWhile(i => i.Address < (long)addr + minBytes).ToList();
        if (taken.Count == 0 && instructions.Length > 0) taken.Add(instructions[0]);
        return [.. taken];
    }

    private static AsmInstruction[] DisassembleX86(long startAddr, byte[] code, int count)
    {
        using var disassembler = CapstoneDisassembler.CreateX86Disassembler(
            RuntimeInformation.ProcessArchitecture == Architecture.X86
                ? X86DisassembleMode.Bit32
                : X86DisassembleMode.Bit64);
        disassembler.EnableInstructionDetails = true;
        disassembler.DisassembleSyntax = DisassembleSyntax.Intel;

        return disassembler.Disassemble(code, startAddr, count)
            .Select(ins =>
            {
                bool ripRelative = false;
                if (ins.HasDetails)
                {
                    foreach (var op in ins.Details.Operands)
                    {
                        if (op.Type == X86OperandType.Memory &&
                            op.Memory.Base?.Id == X86RegisterId.X86_REG_RIP)
                        {
                            ripRelative = true;
                            break;
                        }
                    }
                }

                return new AsmInstruction
                {
                    Address = ins.Address,
                    Bytes = ins.Bytes,
                    Mnemonic = ins.Mnemonic,
                    OperandText = ins.Operand,
                    IsPositionDependent = ripRelative,
                };
            })
            .ToArray();
    }

    private static AsmInstruction[] DisassembleArm64(long startAddr, byte[] code, int count)
    {
        using var disassembler = CapstoneDisassembler.CreateArm64Disassembler(Arm64DisassembleMode.Arm);
        disassembler.EnableInstructionDetails = true;
        disassembler.DisassembleSyntax = DisassembleSyntax.Intel;

        return disassembler.Disassemble(code, startAddr, count)
            .Select(ins =>
            {
                // PC 相对：adp/adr/b/bl/cbz/tbz 等由 Capstone 归入 BRANCH_RELATIVE 组
                bool pcRelative = ins.HasDetails &&
                                  ins.Details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE);
                var mnemonic = ins.Mnemonic;
                if (mnemonic is "adrp" or "adr") pcRelative = true;

                return new AsmInstruction
                {
                    Address = ins.Address,
                    Bytes = ins.Bytes,
                    Mnemonic = mnemonic,
                    OperandText = ins.Operand,
                    IsPositionDependent = pcRelative,
                };
            })
            .ToArray();
    }

    // ── 修补操作 ──

    /// <summary>把 <paramref name="addr"/> 处的 <paramref name="instructionCount"/> 条指令 nop 掉（备份后写入）。</summary>
    public void Nop(nint addr, int instructionCount = 1)
    {
        var instructions = Disassemble(addr, instructionCount);
        if (instructions.Length < instructionCount)
            throw new InvalidOperationException(
                $"only disassembled {instructions.Length} instructions at 0x{(long)addr:X}");

        var totalBytes = instructions.Take(instructionCount).Sum(i => i.Size);
        var nop = new byte[totalBytes];
        Array.Fill(nop, RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? (byte)0x1F : (byte)0x90);
        // arm64: 0x1F 0x20 0x03 0xD5 是标准 nop；重复单字节 0x1F 无效 —— 改用逐条 4 字节 nop
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            for (int i = 0; i < totalBytes; i += 4)
            {
                nop[i] = 0x1F; nop[i + 1] = 0x20; nop[i + 2] = 0x03; nop[i + 3] = 0xD5;
            }
        }

        Write(addr, nop);
    }

    /// <summary>向 <paramref name="addr"/> 写入原始字节（备份后写入）。</summary>
    public void Write(nint addr, ReadOnlySpan<byte> data) => WriteInternal(addr, data.ToArray());

    /// <summary>让函数立即返回 0（x64: <c>xor eax,eax; ret</c> / arm64: <c>mov w0,#0; ret</c>）。</summary>
    public void SkipFunction(nint addr) => Write(addr,
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? [0x00, 0x00, 0x80, 0x52, 0xC0, 0x03, 0x5F, 0xD6] // mov w0,#0; ret
            : [0x31, 0xC0, 0xC3]);                               // xor eax,eax; ret

    /// <summary>写入 int32（常用于改指令立即数字段）。</summary>
    public void WriteInt32(nint addr, int value)
        => Write(addr, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));

    /// <summary>写入 int64。</summary>
    public void WriteInt64(nint addr, long value)
        => Write(addr, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));

    /// <summary>写入相对跳转（x64: <c>jmp rel32</c>，范围 ±2GB）。</summary>
    public void Jump(nint from, nint to)
    {
        int rel = (int)((long)to - ((long)from + 5));
        Span<byte> code = [0xE9, 0, 0, 0, 0];
        MemoryMarshal.Write(code[1..], ref rel);
        Write(from, code);
    }

    // ── 还原 ──

    /// <summary>还原指定地址的补丁（写回备份的原字节）。</summary>
    public void Restore(nint addr)
    {
        var idx = _patches.FindIndex(p => p.Address == addr);
        if (idx < 0) return;
        RawWrite(_patches[idx].Address, _patches[idx].Original);
        _patches.RemoveAt(idx);
    }

    /// <summary>还原本实例应用的所有补丁（倒序）。</summary>
    public void RestoreAll()
    {
        for (int i = _patches.Count - 1; i >= 0; i--)
            RawWrite(_patches[i].Address, _patches[i].Original);
        _patches.Clear();
    }

    // ── 内部 ──

    private unsafe void WriteInternal(nint addr, byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // 备份原字节（同一地址二次修补丢弃后写的备份，保留最早的原字节）
        var original = new byte[data.Length];
        fixed (byte* p = original)
        {
            Buffer.MemoryCopy((void*)addr, p, original.Length, original.Length);
        }

        var idx = _patches.FindIndex(p => p.Address == addr);
        if (idx >= 0) _patches.RemoveAt(idx);
        _patches.Add((addr, original));

        RawWrite(addr, data);
    }

    /// <summary>切换页保护 → 写入 → 还原保护 → 刷指令缓存。</summary>
    private static unsafe void RawWrite(nint addr, byte[] data)
    {
        fixed (byte* p = data)
        {
            if (OperatingSystem.IsWindows())
            {
                VirtualProtect(addr, (nuint)data.Length, PAGE_EXECUTE_READWRITE, out var old);
                Buffer.MemoryCopy(p, (void*)addr, data.Length, data.Length);
                if (old != PAGE_EXECUTE_READWRITE)
                    VirtualProtect(addr, (nuint)data.Length, old, out _);
                FlushInstructionCache(GetCurrentProcess(), addr, (nuint)data.Length);
            }
            else
            {
                const int pageSize = 4096;
                var pageStart = (nint)(((long)addr) & ~(pageSize - 1));
                var pageEnd = (nint)((((long)addr) + data.Length + pageSize - 1) & ~(pageSize - 1));
                mprotect(pageStart, (nuint)(pageEnd - pageStart), PROT_RWX);
                Buffer.MemoryCopy(p, (void*)addr, data.Length, data.Length);
                cacheflush(addr, (nint)((long)addr + data.Length), 0);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        RestoreAll();
        _disposed = true;
    }
}
