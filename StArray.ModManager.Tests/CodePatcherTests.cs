using StArray.ModManager.Runtime.Patching;

namespace StArray.ModManager.Tests;

/// <summary>CodePatcher 反汇编冒烟测试（x64）。</summary>
public class CodePatcherTests
{
    [Test]
    public unsafe void Disassemble_X64_DecodesInstructions()
    {
        // xor eax, eax ; mov rax, 0x1122334455667788 ; ret
        byte[] code = [0x31, 0xC0, 0x48, 0xB8, 0x78, 0x56, 0x34, 0x12, 0x78, 0x56, 0x34, 0x12, 0xC3];

        fixed (byte* p = code)
        {
            var instructions = CodePatcher.Disassemble((nint)p, 3);

            Assert.That(instructions.Length, Is.EqualTo(3));
            Assert.That(instructions[0].Mnemonic, Is.EqualTo("xor"));
            Assert.That(instructions[0].Size, Is.EqualTo(2));
            Assert.That(instructions[1].Mnemonic, Is.EqualTo("movabs"));
            Assert.That(instructions[1].Size, Is.EqualTo(10));
            Assert.That(instructions[2].Mnemonic, Is.EqualTo("ret"));
            Assert.That(instructions[0].IsPositionDependent, Is.False);
        }
    }

    [Test]
    public unsafe void Disassemble_X64_DetectsRipRelative()
    {
        // lea rax, [rip + 0x10]  → 48 8D 05 10 00 00 00
        byte[] code = [0x48, 0x8D, 0x05, 0x10, 0x00, 0x00, 0x00];

        fixed (byte* p = code)
        {
            var instructions = CodePatcher.Disassemble((nint)p, 1);

            Assert.That(instructions.Length, Is.EqualTo(1));
            Assert.That(instructions[0].Mnemonic, Is.EqualTo("lea"));
            Assert.That(instructions[0].IsPositionDependent, Is.True);
        }
    }

    [Test]
    public unsafe void Disassemble_AtLeast_ReturnsInstructionBoundaries()
    {
        // 2 字节 + 2 字节 + 1 字节：要求覆盖 3 字节应返回前 2 条
        byte[] code = [0x31, 0xC0, 0x85, 0xC0, 0xC3];

        fixed (byte* p = code)
        {
            var instructions = CodePatcher.DisassembleAtLeast((nint)p, 3);

            Assert.That(instructions.Length, Is.EqualTo(2));
            Assert.That(instructions.Sum(i => i.Size), Is.GreaterThanOrEqualTo(3));
        }
    }
}
