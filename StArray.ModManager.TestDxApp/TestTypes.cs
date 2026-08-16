namespace StArray.ModManager.TestDxApp.TestTypes;

// ════════════════════════════════════════════════════════════════
//  Stub 生成器的测试类型集：覆盖各种形态的成员与类型组合。
//  这些类型会被 AssemblyEmitter 收集进 UnmanagedTypeAssembly.dll，
//  用于验证类型与成员收集的完整性。
// ════════════════════════════════════════════════════════════════

// ── 1. 只读字段与只读结构 ──
public readonly struct ReadonlyPoint
{
    public readonly int X;
    public readonly int Y;

    public ReadonlyPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public double Length => Math.Sqrt(X * X + Y * Y);
}

public class ReadOnlyMembers
{
    public readonly string Name = "fixed";
    public readonly ReadonlyPoint Origin = new(0, 0);
    public static readonly ReadOnlyMembers Shared = new();

    public int ReadOnlyProp => Name.Length;
    public ReadonlyPoint Position => Origin;
}

// ── 2. 嵌套枚举（历史崩溃点：混淆程序集的嵌套枚举迭代器）──
public enum TopLevelEnum
{
    None = 0,
    Alpha = 1,
    Beta = 2,
    Gamma = 0x10,
}

public class NestingHost
{
    public enum InnerMode          // 嵌套枚举：普通
    {
        Idle = 0,
        Working = 1,
        Success = 2,
        Fail = 3,
    }

    public enum InnerLongEnum : ulong  // 嵌套枚举：非 int 底层类型
    {
        Min = ulong.MinValue,
        Mid = 0xFFFF_FFFF_FFFF,
        Max = ulong.MaxValue,
    }

    public enum InnerByteEnum : byte    // 嵌套枚举：byte 底层
    {
        A = 1,
        B = 2,
        C = 255,
    }

    public sealed class DeepHost      // 三层嵌套
    {
        public struct InnerConfig
        {
            public int Depth;
            public InnerMode Mode;

            public enum Level           // 深层嵌套枚举
            {
                L1 = 1,
                L2 = 2,
                L3 = 3,
            }
        }

        public InnerConfig Config => new() { Depth = 3, Mode = InnerMode.Working };
    }
}

// ── 3. 接口与实现（接口方法需要实现的 stub 形态）──
public interface IShape
{
    double Area { get; }
    string Describe();
    bool Contains(double x, double y);
}

public interface IColoredShape : IShape
{
    byte R { get; }
    byte G { get; }
    byte B { get; }
}

public class Circle : IColoredShape
{
    private double _radius;

    public double Radius
    {
        get => _radius;
        set => _radius = value;
    }

    public double Area => Math.PI * _radius * _radius;
    public byte R { get; set; } = 255;
    public byte G { get; set; }
    public byte B { get; set; }

    public string Describe() => $"Circle r={_radius}";
    public bool Contains(double x, double y) => x * x + y * y <= _radius * _radius;
}

// ── 4. 静态成员与各种方法签名 ──
public static class StaticFactory
{
    public const double Pi = Math.PI;
    public const string Greeting = "hello";
    public static int Counter;

    public static int Add(int a, int b) => a + b;
    public static double Mix(double a, float b, int c) => a + b + c;
    public static void NoReturn(byte[] buffer, int offset) { }
    public static unsafe void* UnsafePointer(void* input, nuint length) => input; // 供签名解析测试
    public static T Identity<T>(T value) => value;   // 泛型方法（stub 应降级或跳过）
    public static void Params(params int[] values) { }
    public static void Optional(int a = 1, string b = "x") { }
    public static void RefKind(ref int a, in double b, out string c)
    {
        c = a.ToString();
    }
}

// ── 5. 结构体（值类型）──
public struct MutableSize
{
    public int Width;
    public int Height;

    public int Area => Width * Height;
}

[Flags]
public enum PermissionFlags
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute,
}

// ── 6. 事件与索引器 ──
public class EventBus
{
    public event EventHandler? Changed;
    public event Action<int, string>? DataArrived;

    private readonly Dictionary<string, int> _map = new();

    public int this[string key]
    {
        get => _map.TryGetValue(key, out var v) ? v : 0;
        set => _map[key] = value;
    }

    public int this[int idx] => idx * 2;     // 重载索引器

    public void Raise() => Changed?.Invoke(this, EventArgs.Empty);
}

// ── 7. 继承链与多级基类 ──
public class Animal
{
    public virtual string Speak() => "...";
    public int Age { get; set; }
}

public class Dog : Animal
{
    private readonly string _name;
    public Dog(string name) => _name = name;
    public override string Speak() => "Woof";
    public string Name => _name;
}

public sealed class Puppy : Dog
{
    public Puppy() : base("puppy") { }
    public new string Speak() => "Yip";
}

// ── 8. 显式接口实现与运算符 ──
public class ExplicitImpl : IComparable<ExplicitImpl>, IEquatable<ExplicitImpl>
{
    public int Value { get; set; }

    int IComparable<ExplicitImpl>.CompareTo(ExplicitImpl? other) => Value - (other?.Value ?? 0);
    bool IEquatable<ExplicitImpl>.Equals(ExplicitImpl? other) => Value == other?.Value;

    public static ExplicitImpl operator +(ExplicitImpl a, ExplicitImpl b)
        => new() { Value = a.Value + b.Value };
    public static explicit operator int(ExplicitImpl e) => e.Value;
    public static implicit operator double(ExplicitImpl e) => e.Value;
}

// ── 9. 委托字段与数组 ──
public class DelegateHolder
{
    public delegate int Transformer(int input);        // 嵌套委托类型

    public Transformer? Transform;
    public int[,] Matrix = new int[3, 3];
    public string[] Names = ["a", "b", "c"];
    public TopLevelEnum[] Modes = [TopLevelEnum.Alpha, TopLevelEnum.Beta];
    public readonly int[] FixedSquares = [1, 4, 9, 16];
}

// ── 10. 抽象类 ──
public abstract class RendererBase
{
    public abstract int Priority { get; }
    public abstract void Render(nint target);
    public virtual void Prepare() { }
    protected virtual void Dispose(bool disposing) { }
}
