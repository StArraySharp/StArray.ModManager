using System.Runtime.InteropServices;

namespace StArray.ModManager.Il2Cpp;

/// <summary>UnityEngine.Vector3 — 12 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vector3
{
    public float X, Y, Z;

    public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }

    public float LengthSquared() => X * X + Y * Y + Z * Z;
    public float Length() => MathF.Sqrt(LengthSquared());
    public float Dot(Vector3 b) => X * b.X + Y * b.Y + Z * b.Z;
    public Vector3 Normalized() { var len = Length(); return len > 0 ? new(X / len, Y / len, Z / len) : this; }

    public static Vector3 operator *(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vector3 operator /(Vector3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);
    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static bool operator ==(Vector3 a, Vector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

    public override bool Equals(object? obj) => obj is Vector3 v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}

/// <summary>UnityEngine.Vector2 — 8 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vector2
{
    public float X, Y;

    public Vector2(float x, float y) { X = x; Y = y; }
    public float LengthSquared() => X * X + Y * Y;
    public float Length() => MathF.Sqrt(LengthSquared());

    public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
    public override bool Equals(object? obj) => obj is Vector2 v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

/// <summary>UnityEngine.Vector4 — 16 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vector4
{
    public float X, Y, Z, W;
    public Vector4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
}

/// <summary>UnityEngine.Quaternion — 16 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Quaternion
{
    public float X, Y, Z, W;
    public Quaternion(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
    public static Quaternion Identity => new(0, 0, 0, 1);
}

/// <summary>UnityEngine.Matrix4x4 — 64 bytes (4x4 float)</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Matrix4x4
{
    public fixed float M[16];
}

/// <summary>UnityEngine.Rect — 16 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
    public float X, Y, Width, Height;
    public Rect(float x, float y, float w, float h) { X = x; Y = y; Width = w; Height = h; }
}

/// <summary>UnityEngine.Color — 16 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Color
{
    public float R, G, B, A;
    public Color(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
}

/// <summary>UnityEngine.Bounds — 24 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Bounds
{
    public Vector3 Center;
    public Vector3 Extents;
}

/// <summary>UnityEngine.Ray</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;
}

/// <summary>UnityEngine.RaycastHit</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RaycastHit
{
    public Vector3 Point;
    public Vector3 Normal;
}

/// <summary>UnityEngine.Plane — 16 bytes</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Plane
{
    public Vector3 Normal;
    public float Distance;
}
