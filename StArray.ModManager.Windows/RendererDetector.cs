using System;
using StArray.ModManager.RuntimeAbstractions;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Windows;

[Flags]
public enum Renderer
{
    None = 0,
    D3D12 = 1,
    D3D11 = 2,
    D3D9 = 4,
    OpenGL = 8,
    Vulkan = 16
}

public static class RendererDetector
{
    public static Renderer GetGameRenderer()
    {
        Renderer result = Renderer.None;

        int gdt = GraphicsDevice.GetGraphicsDeviceType();
        switch (gdt)
        {
            case 0:  result |= Renderer.D3D9;   break;
            case 2:  result |= Renderer.D3D11;   break;
            case 3:  result |= Renderer.D3D12;   break;
            case 11: result |= Renderer.OpenGL;   break;
            case 13: result |= Renderer.Vulkan;   break;
            default:
                if (Win32Native.GetModuleHandleW("d3d12.dll") != nint.Zero)
                    result |= Renderer.D3D12;
                if (Win32Native.GetModuleHandleW("d3d11.dll") != nint.Zero)
                    result |= Renderer.D3D11;
                if (Win32Native.GetModuleHandleW("d3d9.dll") != nint.Zero)
                    result |= Renderer.D3D9;
                if (Win32Native.GetModuleHandleW("opengl32.dll") != nint.Zero)
                    result |= Renderer.OpenGL;
                if (Win32Native.GetModuleHandleW("vulkan-1.dll") != nint.Zero)
                    result |= Renderer.Vulkan;
                break;
        }

        return result;
    }
}
