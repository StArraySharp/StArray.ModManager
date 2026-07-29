using System.Runtime.InteropServices;
using System.Text;
using StArray.ModManager.Native;

namespace StArray.ModManager.Runtime;

public class NativeFuncResolver : IDisposable
{
    private readonly byte[] _fileData;
    private readonly long _textAddr;
    private readonly long _textOffset;
    private readonly long _textSize;
    private byte[]? _textBytes;
    private IntPtr _loadedHandle;

    // 符号表缓存
    private long _dynSymAddr, _dynSymOffset, _dynSymSize;
    private long _dynStrAddr, _dynStrOffset, _dynStrSize;

    public string FilePath { get; }
    public bool IsLoaded => _loadedHandle != IntPtr.Zero;
    public IntPtr LoadedHandle => _loadedHandle;

    public NativeFuncResolver(string elfPath)
    {
        FilePath = elfPath;
        _fileData = File.ReadAllBytes(elfPath);
        ParseElfHeaders(_fileData,
            out _textAddr, out _textOffset, out _textSize,
            out _dynSymAddr, out _dynSymOffset, out _dynSymSize,
            out _dynStrAddr, out _dynStrOffset, out _dynStrSize);
    }

    private ReadOnlySpan<byte> TextSpan =>
        _textBytes ??= _fileData.AsSpan((int)_textOffset, (int)_textSize).ToArray();

    // ===================== 统一入口 =====================

    /// <summary>查找函数 RVA：优先符号名，失败则回退特征码</summary>
    public long FindRva(string mangledSymbol, byte?[]? fallbackPattern = null)
    {
        long rva = FindSymbolRva(mangledSymbol);
        if (rva >= 0) return rva;

        if (fallbackPattern != null)
        {
            rva = FindRva(fallbackPattern);
            return rva;
        }

        throw new KeyNotFoundException(
            $"Symbol '{mangledSymbol}' not found in export table, and no fallback signature provided.");
    }

    /// <summary>加载 + 定位 + 返回函数指针，一步到位</summary>
    public IntPtr Resolve(string mangledSymbol, byte?[]? fallbackPattern = null)
    {
        long rva = FindRva(mangledSymbol, fallbackPattern);
        Load();
        return GetFuncPtr(rva);
    }

    // ===================== 符号表查找 =====================

    /// <summary>在 .dynsym 中搜索符号名，返回 RVA（-1 表示未找到）</summary>
    public long FindSymbolRva(string symbolName)
    {
        var symData = _fileData.AsSpan((int)_dynSymOffset, (int)_dynSymSize);
        var strData = _fileData.AsSpan((int)_dynStrOffset, (int)_dynStrSize);

        // ELF64 symbol entry: 24 bytes
        // st_name(4) st_info(1) st_other(1) st_shndx(2) st_value(8) st_size(8)
        for (int i = 0; i + 24 <= symData.Length; i += 24)
        {
            int nameOff = BitConverter.ToInt32(symData[i..]);
            byte info = symData[i + 4];
            byte other = symData[i + 5];
            short shndx = BitConverter.ToInt16(symData[(i + 6)..]);
            long stValue = BitConverter.ToInt64(symData[(i + 8)..]);
            long stSize = BitConverter.ToInt64(symData[(i + 16)..]);

            // 跳过未定义节 / 值无效的
            if (shndx == 0 || stValue == 0) continue;

            // 取符号名
            int end = nameOff;
            while (end < strData.Length && strData[end] != 0) end++;
            string name = Encoding.ASCII.GetString(strData[nameOff..end]);

            if (name == symbolName)
                return stValue;
        }
        return -1;
    }

    /// <summary>按模式匹配符号名（支持 * 通配符）</summary>
    public long[] FindSymbolsByPattern(string pattern)
    {
        bool Match(string name)
        {
            if (!pattern.Contains('*')) return name == pattern;
            // 简单 glob 匹配
            string p = pattern.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(name, $"^{p}$");
        }

        var result = new List<long>();
        var symData = _fileData.AsSpan((int)_dynSymOffset, (int)_dynSymSize);
        var strData = _fileData.AsSpan((int)_dynStrOffset, (int)_dynStrSize);

        for (int i = 0; i + 24 <= symData.Length; i += 24)
        {
            int nameOff = BitConverter.ToInt32(symData[i..]);
            short shndx = BitConverter.ToInt16(symData[(i + 6)..]);
            long stValue = BitConverter.ToInt64(symData[(i + 8)..]);
            if (shndx == 0 || stValue == 0) continue;

            int end = nameOff;
            while (end < strData.Length && strData[end] != 0) end++;
            string name = Encoding.ASCII.GetString(strData[nameOff..end]);

            if (Match(name))
            {
                Console.WriteLine($"  Found: {name} @ 0x{stValue:x}");
                result.Add(stValue);
            }
        }
        return result.ToArray();
    }

    // ===================== 特征码搜索 =====================

    public long FindRva(params byte?[] pattern)
    {
        int offset = Search(TextSpan, pattern);
        if (offset < 0)
            throw new KeyNotFoundException("Signature not found in .text section");
        return _textAddr + offset;
    }

    public long FindRva(byte[] pattern) =>
        FindRva(pattern.Select(b => (byte?)b).ToArray());

    public long[] FindAllRva(params byte?[] pattern)
    {
        var result = new List<long>();
        int pos = 0;
        while ((pos = Search(TextSpan, pattern, pos)) >= 0)
        {
            result.Add(_textAddr + pos);
            pos++;
        }
        return result.ToArray();
    }

    // ===================== 库加载 =====================

    public void Load()
    {
        if (_loadedHandle != IntPtr.Zero) return;
        if (OperatingSystem.IsWindows()) _loadedHandle = NativeLibrary.Load(FilePath);
        else
        {
            var filename = Path.GetFileName(FilePath);
            _loadedHandle = DL.GetBaseAddress(filename);
            if (_loadedHandle == IntPtr.Zero)
            {
                DL.Open(filename, DL.RTLDFlags.RTLD_LAZY);
                _loadedHandle = DL.GetBaseAddress(filename);
            }
            if (_loadedHandle == IntPtr.Zero)
                throw new DllNotFoundException($"Failed to get base address of '{FilePath}'");
        }
    }

    public IntPtr GetFuncPtr(long rva)
    {
        if (_loadedHandle == IntPtr.Zero)
            throw new InvalidOperationException("Library not loaded. Call Load() first.");
        return new IntPtr((long)_loadedHandle + rva);
    }

    public void Dispose()
    {
        if (_loadedHandle != IntPtr.Zero)
        {
            NativeLibrary.Free(_loadedHandle);
            _loadedHandle = IntPtr.Zero;
        }
    }

    // ===================== 工具 =====================

    public static byte?[] ParseHexPattern(string hex)
    {
        var parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new byte?[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            result[i] = parts[i] == "??" ? null : Convert.ToByte(parts[i], 16);
        return result;
    }

    // ===================== 私有方法 =====================

    private static int Search(ReadOnlySpan<byte> data, byte?[] pattern, int start = 0)
    {
        for (int i = start; i <= data.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                byte? b = pattern[j];
                if (b.HasValue && data[i + j] != b.Value)
                { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    private static void ParseElfHeaders(byte[] d,
        out long textAddr, out long textOffset, out long textSize,
        out long dynSymAddr, out long dynSymOffset, out long dynSymSize,
        out long dynStrAddr, out long dynStrOffset, out long dynStrSize)
    {
        textAddr = textOffset = textSize = 0;
        dynSymAddr = dynSymOffset = dynSymSize = 0;
        dynStrAddr = dynStrOffset = dynStrSize = 0;

        if (d[0] != 0x7f || d[1] != 0x45 || d[2] != 0x4c || d[3] != 0x46)
            throw new InvalidDataException("Not an ELF file");

        int shOff = (int)BitConverter.ToInt64(d, 0x28);
        short shEntSize = BitConverter.ToInt16(d, 0x3a);
        short shNum = BitConverter.ToInt16(d, 0x3c);
        short shStrNdx = BitConverter.ToInt16(d, 0x3e);

        int Off(int idx, int f) => shOff + idx * shEntSize + f;
        int strTabOff = (int)BitConverter.ToInt64(d, Off(shStrNdx, 0x18));

        string SecName(int idx)
        {
            int no = BitConverter.ToInt32(d, Off(idx, 0)), end = no;
            while (d[strTabOff + end] != 0) end++;
            return Encoding.ASCII.GetString(d, strTabOff + no, end - no);
        }

        for (int i = 0; i < shNum; i++)
        {
            string name = SecName(i);
            long addr = BitConverter.ToInt64(d, Off(i, 0x10));
            long offset = BitConverter.ToInt64(d, Off(i, 0x18));
            long size = BitConverter.ToInt64(d, Off(i, 0x20));

            switch (name)
            {
                case ".text":    textAddr = addr; textOffset = offset; textSize = size; break;
                case ".dynsym":  dynSymAddr = addr; dynSymOffset = offset; dynSymSize = size; break;
                case ".dynstr":  dynStrAddr = addr; dynStrOffset = offset; dynStrSize = size; break;
            }
        }

        if (textSize == 0) throw new InvalidDataException(".text section not found");
        if (dynSymSize == 0) throw new InvalidDataException(".dynsym section not found");
        if (dynStrSize == 0) throw new InvalidDataException(".dynstr section not found");
    }
}