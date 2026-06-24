using ImGuiNET;
using StArray.ModManager.Java;
using StArray.ModManager.UI;

namespace StArray.ModManager.Manager;

/// <summary>
/// 基于MotionEventHook的ImGui输入提供者
/// 使用 ImGui_ImplAndroid_HandleInputEvent 处理原始触摸事件
/// </summary>
public class MotionInputProvider : IInputProvider
{
    private bool _isHookActive;
    private float _screenWidth = 1080f;
    private float _screenHeight = 2400f;
    private float _glWidth = 1080f;
    private float _glHeight = 2400f;

    public MotionInputProvider()
    {
    }

    public void SetScreenSize(float screenWidth, float screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void SetGLSize(float glWidth, float glHeight)
    {
        _glWidth = glWidth;
        _glHeight = glHeight;
    }

    public void Start()
    {
        if (_isHookActive) return;

        bool success = MotionEventHook.StartHook(OnMotionEvent);
        if (success)
        {
            _isHookActive = true;
            AndroidLog.Info("MotionInputProvider", "Started with ImGui_ImplAndroid_HandleInputEvent");
        }
        else
        {
            AndroidLog.Error("MotionInputProvider", "Failed to start hook");
        }
    }

    public void Stop()
    {
        if (!_isHookActive) return;

        MotionEventHook.StopHook();
        _isHookActive = false;
        AndroidLog.Info("MotionInputProvider", "Stopped");
    }

    public void UpdateInput(ImGuiIOPtr io)
    {
        // 输入由 ImGui_ImplAndroid_HandleInputEvent 直接处理
        // 这里不需要手动更新
    }

    private void OnMotionEvent(IntPtr inputEvent)
    {
        try
        {
            // 使用官方 ImGui Android backend 处理输入事件
            // 参考: ImGui_ImplAndroid_HandleInputEvent((AInputEvent*)inputEvent, scale)
            // 注意：C++ 版本接受 scale 参数，C 绑定可能不同
            ImGuiImplAndroid.HandleInputEvent(inputEvent);
        }
        catch (Exception ex)
        {
            AndroidLog.Error("MotionInputProvider", $"Error handling input event: {ex}");
        }
    }
}
