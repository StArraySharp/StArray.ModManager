using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace StArray.ModManager.TestDxApp;

/// <summary>
/// 三角形渲染后端接口
/// </summary>
internal interface ITriangleBackend : IDisposable
{
    /// <summary>在 OnLoad 中初始化所有 GPU 资源</summary>
    void Load(IWindow window);

    /// <summary>每帧渲染三角形</summary>
    void Render(double deltaSeconds);

    /// <summary>窗口尺寸变化</summary>
    void Resize(Vector2D<int> newSize);
}
