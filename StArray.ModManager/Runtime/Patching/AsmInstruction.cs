namespace StArray.ModManager.Runtime.Patching;

/// <summary>
/// 反汇编出的单条指令（架构无关的归一化视图，隔离 Capstone 类型）。
/// </summary>
public sealed class AsmInstruction
{
    /// <summary>指令在进程内的绝对地址。</summary>
    public long Address { get; init; }

    /// <summary>指令原始字节。</summary>
    public byte[] Bytes { get; init; } = [];

    /// <summary>助记符（如 "mov"、"jmp"）。</summary>
    public string Mnemonic { get; init; } = "";

    /// <summary>操作数文本（Intel 语法）。</summary>
    public string OperandText { get; init; } = "";

    /// <summary>指令字节长度。</summary>
    public int Size => Bytes.Length;

    /// <summary>
    /// 是否位置相关（x64：RIP 相对寻址；arm64：PC 相对分支 / ADRP）。
    /// 复制/移动该指令时必须重定位，否则语义改变。
    /// </summary>
    public bool IsPositionDependent { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"0x{Address:X}: {Mnemonic} {OperandText}";
}
