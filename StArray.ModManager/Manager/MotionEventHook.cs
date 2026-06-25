using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.PInvoke;

namespace StArray.ModManager.Manager;

/// <summary>
/// Motion事件Hook - 用于拦截触摸和鼠标的按下、抬起、拖动事件
/// 基于Android InputPublisher::publishMotionEvent的native hook
/// </summary>
public static class MotionEventHook
{
    private static IntPtr _originalFunc30 = IntPtr.Zero;
    private static IntPtr _originalFunc31 = IntPtr.Zero;
    private static IntPtr _originalFunc35 = IntPtr.Zero;
    private static IntPtr _originalInitMotion = IntPtr.Zero;
    private static bool _isHooked;

    private const string TAG = "MotionEventHook";

    /// <summary>
    /// Motion事件回调委托（传递原始 AInputEvent 指针）
    /// </summary>
    /// <param name="inputEvent">AInputEvent* 指针</param>
    public delegate void MotionEventCallback(IntPtr inputEvent);

    private static MotionEventCallback? _callback;

    /// <summary>
    /// 启动Motion事件Hook
    /// </summary>
    /// <param name="callback">事件回调函数</param>
    /// <returns>是否成功</returns>
    public static bool StartHook(MotionEventCallback callback)
    {
        if (_isHooked)
        {
            AndroidUtils.Warn("MotionEventHook", "Already hooked");
            return false;
        }

        _callback = callback;

        try
        {
            int apiLevel = GetApiLevel();
            AndroidUtils.Info(TAG, $"API Level: {apiLevel}");

            // 优先尝试hook InputConsumer::initializeMotionEvent（更可靠）
            string consumerSymbol = "_ZN7android13InputConsumer21initializeMotionEventEPNS_11MotionEventEPKNS_12InputMessageE";
            IntPtr consumerAddr = Dobby.SymbolResolver("libinput.so", consumerSymbol);
            
            if (consumerAddr != IntPtr.Zero)
            {
                AndroidUtils.Info(TAG, $"Found InputConsumer::initializeMotionEvent at 0x{consumerAddr:X}");
                
                unsafe
                {
                    delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int> hookFunc = &InitializeMotionEvent;
                    int result = Dobby.Hook(consumerAddr, (IntPtr)hookFunc, out _originalInitMotion);
                    
                    if (result == 0)
                    {
                        _isHooked = true;
                        AndroidUtils.Info(TAG, "Hooked InputConsumer::initializeMotionEvent successfully");
                        return true;
                    }

                    AndroidUtils.Error(TAG, $"Failed to hook InputConsumer, code: {result}");
                }
            }
            else
            {
                AndroidUtils.Warn(TAG, "InputConsumer::initializeMotionEvent not found, trying InputPublisher");
            }

            // 回退到InputPublisher方案
            IntPtr funcAddress;

            if (apiLevel >= 35)
            {
                // Android 15+ (API 35-37)
                // publishMotionEvent with ui::LogicalDisplayId
                string[] symbols = new[]
                {
                    // 标准符号 (API 35+)
                    "_ZN7android14InputPublisher18publishMotionEventEjiiiNS_2ui16LogicalDisplayIdENSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS1_9TransformEffffRKS8_lljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE",
                    // 完整展开版本
                    "_ZN7android14InputPublisher18publishMotionEventEjiiiNS_2ui16LogicalDisplayIdENSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKNS_2ui9TransformElljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE",
                };
                
                funcAddress = IntPtr.Zero;
                foreach (var symbol in symbols)
                {
                    funcAddress = Dobby.SymbolResolver("libinput.so", symbol);
                    if (funcAddress != IntPtr.Zero)
                    {
                        AndroidUtils.Info("MotionEventHook", "Found symbol for API 35+");
                        break;
                    }
                }
                
                if (funcAddress == IntPtr.Zero)
                {
                    AndroidUtils.Error("MotionEventHook", $"Symbol not found for API {apiLevel}. Tried {symbols.Length} variants.");
                    return false;
                }

                unsafe
                {
                    delegate* unmanaged<IntPtr, uint, int, int, int, int, IntPtr, int, int, int, int, int, int, IntPtr, float, float, float, float, IntPtr, long, long, uint, IntPtr, IntPtr, int> hookFunc = &PublishMotionEvent_API35;
                    int result = Dobby.Hook(funcAddress, (IntPtr)hookFunc, out _originalFunc35);
                
                    if (result != 0)
                    {
                        AndroidUtils.Error("MotionEventHook", $"DobbyHook failed with code: {result}");
                        return false;
                    }
                }
                
                _isHooked = true;
                AndroidUtils.Info("MotionEventHook", $"Hooked API 35+ at 0x{funcAddress:X}");
            }
            else if (apiLevel >= 31)
            {
                // Android 12-14 (API 31-34) - Android 12的特殊签名
                // publishMotionEvent 没有rawTransform，cursor是int类型
                string symbol = "_ZN7android14InputPublisher18publishMotionEventEjiiiiNSt3__15arrayIhLm32EEEiiiiiiNS_20MotionClassificationERKNS_2ui9TransformEffffiilljPKNS_17PointerPropertiesEPKNS_13PointerCoordsE";
                
                funcAddress = Dobby.SymbolResolver("libinput.so", symbol);
                
                if (funcAddress == IntPtr.Zero)
                {
                    AndroidUtils.Error("MotionEventHook", "Symbol not found for API 31-34");
                    return false;
                }

                AndroidUtils.Info("MotionEventHook", $"Found symbol at 0x{funcAddress:X}, attempting hook...");

                unsafe
                {
                    delegate* unmanaged[Cdecl]<IntPtr, uint, int, int, int, int, IntPtr, int, int, int, int, int, int, IntPtr, float, float, float, float, int, int, long, long, uint, IntPtr, IntPtr, int> hookFunc = &PublishMotionEvent_API31;
                    int result = Dobby.Hook(funcAddress, (IntPtr)hookFunc, out _originalFunc31);
                
                    if (result != 0)
                    {
                        AndroidUtils.Error("MotionEventHook", $"DobbyHook failed with code: {result}");
                        return false;
                    }
                }
                
                _isHooked = true;
                AndroidUtils.Info("MotionEventHook", $"Hooked API 31-34 at 0x{funcAddress:X}");
                AndroidUtils.Info("MotionEventHook", $"Original function saved at 0x{_originalFunc31:X}");
            }
            else if (apiLevel >= 30)
            {
                // Android 11-14 (API 30-34)
                // publishMotionEvent with int32_t displayId
                // 尝试多个可能的符号变体
                string[] symbols = new[]
                {
                    // 标准符号 (API 30-34)
                    "_ZN7android14InputPublisher18publishMotionEventEjiiiiNSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKS7_lljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE",
                    // 完整展开版本（不使用backreference）
                    "_ZN7android14InputPublisher18publishMotionEventEjiiiiNSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKNS_2ui9TransformElljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE",
                    // edgeFlags版本（某些版本可能包含edgeFlags参数）
                    "_ZN7android14InputPublisher18publishMotionEventEjiiiiNSt3__15arrayIhLm32EEEiiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKS7_lljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE",
                };
                
                funcAddress = IntPtr.Zero;
                foreach (var symbol in symbols)
                {
                    funcAddress = Dobby.SymbolResolver("libinput.so", symbol);
                    if (funcAddress != IntPtr.Zero)
                    {
                        AndroidUtils.Info("MotionEventHook", "Found symbol for API 30-34");
                        break;
                    }
                }
                
                if (funcAddress == IntPtr.Zero)
                {
                    AndroidUtils.Error("MotionEventHook", $"Symbol not found for API {apiLevel}. Tried {symbols.Length} variants.");
                    AndroidUtils.Error("MotionEventHook", "You may need to check the actual symbol in libinput.so using 'readelf -Ws /system/lib64/libinput.so | grep publishMotion'");
                    return false;
                }

                unsafe
                {
                    delegate* unmanaged<IntPtr, uint, int, int, int, int, IntPtr, int, int, int, int, int, int, IntPtr, float, float, float, float, IntPtr, long, long, uint, IntPtr, IntPtr, int> hookFunc = &PublishMotionEvent_API30;
                    int result = Dobby.Hook(funcAddress, (IntPtr)hookFunc, out _originalFunc30);
                
                    if (result != 0)
                    {
                        AndroidUtils.Error("MotionEventHook", $"DobbyHook failed with code: {result}");
                        return false;
                    }
                }
                
                _isHooked = true;
                AndroidUtils.Info("MotionEventHook", $"Hooked API 30-34 at 0x{funcAddress:X}");
            }
            else
            {
                AndroidUtils.Error("MotionEventHook", $"API {apiLevel} not supported (requires API 30+)");
                return false;
            }

            return _isHooked;
        }
        catch (Exception ex)
        {
            AndroidUtils.Error("MotionEventHook", $"Error: {ex}");
            return false;
        }
    }

    /// <summary>
    /// 停止Hook
    /// </summary>
    public static void StopHook()
    {
        if (!_isHooked) return;

        try
        {
            if (_originalFunc35 != IntPtr.Zero)
            {
                Dobby.Destroy(_originalFunc35);
                _originalFunc35 = IntPtr.Zero;
            }

            if (_originalFunc31 != IntPtr.Zero)
            {
                Dobby.Destroy(_originalFunc31);
                _originalFunc31 = IntPtr.Zero;
            }

            if (_originalInitMotion != IntPtr.Zero)
            {
                Dobby.Destroy(_originalInitMotion);
                _originalInitMotion = IntPtr.Zero;
            }

            if (_originalFunc30 != IntPtr.Zero)
            {
                Dobby.Destroy(_originalFunc30);
                _originalFunc30 = IntPtr.Zero;
            }

            _isHooked = false;
            _callback = null;
            AndroidUtils.Info("MotionEventHook", "Hook stopped");
        }
        catch (Exception ex)
        {
            AndroidUtils.Error("MotionEventHook", $"Error stopping hook: {ex}");
        }
    }

    /// <summary>
    /// 调试方法：尝试查找所有可能的publishMotionEvent符号
    /// </summary>
    public static void DebugFindSymbols()
    {
        AndroidUtils.Info("MotionEventHook", "=== Searching for publishMotionEvent symbols ===");
        
        // 所有已知的符号变体
        var allSymbols = new[]
        {
            // API 28-29 (Android 9-10)
            ("API 28-29", "_ZN7android14InputPublisher18publishMotionEventEjiiiiiiiiiffflljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE"),
            
            // API 30-34 variants
            ("API 30-34 v1", "_ZN7android14InputPublisher18publishMotionEventEjiiiiNSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKS7_lljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE"),
            ("API 30-34 v2", "_ZN7android14InputPublisher18publishMotionEventEjiiiiNSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKNS_2ui9TransformElljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE"),
            ("API 30-34 v3", "_ZN7android14InputPublisher18publishMotionEventEjiiiiNSt3__15arrayIhLm32EEEiiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKS7_lljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE"),
            
            // API 35+ variants
            ("API 35+ v1", "_ZN7android14InputPublisher18publishMotionEventEjiiiNS_2ui16LogicalDisplayIdENSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS1_9TransformEffffRKS8_lljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE"),
            ("API 35+ v2", "_ZN7android14InputPublisher18publishMotionEventEjiiiNS_2ui16LogicalDisplayIdENSt3__15arrayIhLm32EEEiiiiiiNS_18MotionClassificationERKNS_2ui9TransformEffffRKNS_2ui9TransformElljiPKNS_16PointerPropertiesEPKNS_13PointerCoordsE"),
        };

        int foundCount = 0;
        foreach (var (name, symbol) in allSymbols)
        {
            IntPtr addr = Dobby.SymbolResolver("libinput.so", symbol);
            if (addr != IntPtr.Zero)
            {
                AndroidUtils.Info("MotionEventHook", $"✓ FOUND {name}: 0x{addr:X}");
                foundCount++;
            }
            else
            {
                AndroidUtils.Debug("MotionEventHook", $"✗ Not found: {name}");
            }
        }

        AndroidUtils.Info("MotionEventHook", $"=== Found {foundCount}/{allSymbols.Length} symbols ===");
        
        if (foundCount == 0)
        {
            AndroidUtils.Warn("MotionEventHook", "No symbols found! Possible reasons:");
            AndroidUtils.Warn("MotionEventHook", "1. libinput.so path might be different on this device");
            AndroidUtils.Warn("MotionEventHook", "2. Symbol name mangling differs from AOSP");
            AndroidUtils.Warn("MotionEventHook", "3. Need to check actual library: /system/lib64/libinput.so or /system/lib/libinput.so");
        }
    }

    // ============ Native Hook Functions ============

    /// <summary>
    /// Hook函数 - API 30-34 (int32_t displayId)
    /// </summary>
    [UnmanagedCallersOnly]
    private static int PublishMotionEvent_API30(
        IntPtr thiz, uint seq, int eventId, int deviceId, int source, int displayId,
        IntPtr hmac, // std::array<uint8_t, 32>
        int action, int actionButton, int flags, int metaState, int buttonState,
        int classification, // MotionClassification
        IntPtr transform, // const ui::Transform&
        float xPrecision, float yPrecision, float xCursorPosition, float yCursorPosition,
        IntPtr rawTransform, // const ui::Transform&
        long downTime, long eventTime,
        uint pointerCount,
        IntPtr pointerProperties, // const PointerProperties*
        IntPtr pointerCoords) // const PointerCoords*
    {
        try
        {
            // 直接传递原始 MotionEvent 指针给回调
            // 注意：这里我们需要构造或传递实际的 AInputEvent* 
            // 暂时传递 pointerCoords 作为事件标识
            _callback?.Invoke(thiz);
        }
        catch (Exception ex)
        {
            AndroidUtils.Error("MotionEventHook", $"Callback error: {ex}");
        }

        // 调用原始函数
        if (_originalFunc30 != IntPtr.Zero)
        {
            var original = Marshal.GetDelegateForFunctionPointer<PublishMotionEventDelegate_API30>(_originalFunc30);
            return original(thiz, seq, eventId, deviceId, source, displayId, hmac, 
                action, actionButton, flags, metaState, buttonState, classification,
                transform, xPrecision, yPrecision, xCursorPosition, yCursorPosition,
                rawTransform, downTime, eventTime, pointerCount, pointerProperties, pointerCoords);
        }

        return 0; // OK
    }

    /// <summary>
    /// Hook函数 - API 35+ (ui::LogicalDisplayId)
    /// </summary>
    [UnmanagedCallersOnly]
    private static int PublishMotionEvent_API35(
        IntPtr thiz, uint seq, int eventId, int deviceId, int source, 
        int displayId, // ui::LogicalDisplayId (实际上是int32_t包装)
        IntPtr hmac, // std::array<uint8_t, 32>
        int action, int actionButton, int flags, int metaState, int buttonState,
        int classification, // MotionClassification
        IntPtr transform, // const ui::Transform&
        float xPrecision, float yPrecision, float xCursorPosition, float yCursorPosition,
        IntPtr rawTransform, // const ui::Transform&
        long downTime, long eventTime,
        uint pointerCount,
        IntPtr pointerProperties, // const PointerProperties*
        IntPtr pointerCoords) // const PointerCoords*
    {
        try
        {
            // 只处理按下(0)、抬起(1)、移动(2)
            if (action >= 0 && action <= 2 && pointerCount > 0)
            {
                // 读取第一个触摸点的坐标
                unsafe
                {
                    PointerCoords* coords = (PointerCoords*)pointerCoords;
                    if (coords != null)
                    {
                        // 传递事件指针给 ImGui_ImplAndroid_HandleInputEvent  
                        _callback?.Invoke(thiz);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AndroidUtils.Error("MotionEventHook", $"Callback error: {ex}");
        }

        // 调用原始函数
        if (_originalFunc35 != IntPtr.Zero)
        {
            var original = Marshal.GetDelegateForFunctionPointer<PublishMotionEventDelegate_API35>(_originalFunc35);
            return original(thiz, seq, eventId, deviceId, source, displayId, hmac, 
                action, actionButton, flags, metaState, buttonState, classification,
                transform, xPrecision, yPrecision, xCursorPosition, yCursorPosition,
                rawTransform, downTime, eventTime, pointerCount, pointerProperties, pointerCoords);
        }

        return 0; // OK
    }

    /// <summary>
    /// Hook函数 - InputConsumer::initializeMotionEvent (通用方案)
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int InitializeMotionEvent(IntPtr consumer, IntPtr motionEvent, IntPtr inputMessage)
    {
        AndroidUtils.Debug(TAG, $"InitializeMotionEvent called: motionEvent=0x{motionEvent:X}");
        
        try
        {
            // 使用JNI或直接调用虚函数太复杂
            // 最简单的方法：在调用原始函数后，从已初始化的MotionEvent读取
            // 或者：从InputMessage的固定位置读取关键数据
            
            unsafe
            {
                if (inputMessage != IntPtr.Zero && motionEvent != IntPtr.Zero)
                {
                    // 先调用原始函数初始化MotionEvent
                    if (_originalInitMotion != IntPtr.Zero)
                    {
                        var original = Marshal.GetDelegateForFunctionPointer<InitializeMotionEventDelegate>(_originalInitMotion);
                        int result = original(consumer, motionEvent, inputMessage);
                        
                        // 现在MotionEvent已被初始化，尝试读取
                        // MotionEvent::getAction() - 虚函数表偏移
                        byte* objPtr = (byte*)motionEvent;
                        void** vtable = *(void***)objPtr;
                        
                        // 尝试直接读取成员（估算）
                        // 简化方案：假设action、pointer数据在合理范围内
                        // 从InputMessage读取action（更可靠）
                        byte* msgPtr = (byte*)inputMessage;
                        int msgType = *(int*)msgPtr;
                        
                        if (msgType == 1) // MOTION
                        {
                            // InputMessage body，跳过header
                            byte* bodyPtr = msgPtr + 8;
                            
                            // 尝试读取action - 在不同位置
                            int action = -1;
                            uint pointerCount = 0;
                            
                            // 尝试多个可能的偏移
                            for (int offset = 0; offset < 200; offset += 4)
                            {
                                int testAction = *(int*)(bodyPtr + offset);
                                if (testAction >= 0 && testAction <= 10)
                                {
                                    // 可能是action
                                    AndroidUtils.Debug(TAG, $"Found potential action={testAction} at offset {offset}");
                                }
                            }
                        }
                        
                        return result;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AndroidUtils.Error(TAG, $"InitializeMotionEvent error: {ex}");
        }

        // 调用原始函数
        if (_originalInitMotion != IntPtr.Zero)
        {
            var original = Marshal.GetDelegateForFunctionPointer<InitializeMotionEventDelegate>(_originalInitMotion);
            return original(consumer, motionEvent, inputMessage);
        }

        return 0;
    }

    /// <summary>
    /// Hook函数 - API 31-34 (Android 12-14, 没有rawTransform, cursor是int)
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int PublishMotionEvent_API31(
        IntPtr thiz, uint seq, int eventId, int deviceId, int source, int displayId,
        IntPtr hmac, // std::array<uint8_t, 32>
        int action, int actionButton, int flags, int metaState, int buttonState,
        int classification, // MotionClassification
        IntPtr transform, // const ui::Transform&
        float xPrecision, float yPrecision, float xCursorPosition, float yCursorPosition,
        int xCursorPositionInt, int yCursorPositionInt, // cursor位置是int类型
        long downTime, long eventTime,
        uint pointerCount,
        IntPtr pointerProperties, // const PointerProperties*
        IntPtr pointerCoords) // const PointerCoords*
    {
        AndroidUtils.Debug("MotionEventHook", $"API31 Hook called: action={action}, pointers={pointerCount}, source=0x{source:X}");
        
        try
        {
            // 只处理按下(0)、抬起(1)、移动(2)
            if (action >= 0 && action <= 2 && pointerCount > 0)
            {
                // 读取第一个触摸点的坐标
                unsafe
                {
                    PointerCoords* coords = (PointerCoords*)pointerCoords;
                    if (coords != null)
                    {
                        // 传递事件指针给 ImGui_ImplAndroid_HandleInputEvent
                        _callback?.Invoke(thiz);
                    }
                    else
                    {
                        AndroidUtils.Warn("MotionEventHook", "PointerCoords is null");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AndroidUtils.Error("MotionEventHook", $"Callback error: {ex}");
        }

        // 调用原始函数
        if (_originalFunc31 != IntPtr.Zero)
        {
            var original = Marshal.GetDelegateForFunctionPointer<PublishMotionEventDelegate_API31>(_originalFunc31);
            return original(thiz, seq, eventId, deviceId, source, displayId, hmac, 
                action, actionButton, flags, metaState, buttonState, classification,
                transform, xPrecision, yPrecision, xCursorPosition, yCursorPosition,
                xCursorPositionInt, yCursorPositionInt, downTime, eventTime, 
                pointerCount, pointerProperties, pointerCoords);
        }

        return 0; // OK
    }

    // 辅助函数：从PointerCoords提取轴值
    private static unsafe float GetAxisValue(PointerCoords* coords, int axis)
    {
        if (coords == null) return 0f;
        
        // PointerCoords使用bit mask存储哪些轴有值
        ulong bits = coords->bits;
        if ((bits & (1ul << axis)) == 0)
            return 0f;

        // 计算该轴在values数组中的索引
        int index = 0;
        for (int i = 0; i < axis; i++)
        {
            if ((bits & (1ul << i)) != 0)
                index++;
        }

        return coords->values[index];
    }

    // ============ Native Structures ============

    /// <summary>
    /// PointerCoords - 触摸点坐标数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct PointerCoords
    {
        public ulong bits;               // 位掩码，标识哪些轴有值
        public fixed float values[32];   // 轴值数组
        public byte isResampled;         // 是否已重采样
    }

    /// <summary>
    /// InputMessage::Body::Motion 结构（简化版）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct InputMessageMotion
    {
        public int eventId;
        public uint pointerCount;
        public long eventTime;
        public int deviceId;
        public int source;
        public int displayId;
        public fixed byte hmac[32];
        public int action;
        public int actionButton;
        public int flags;
        public int metaState;
        public int buttonState;
        public int classification;
        public int edgeFlags;
        public long downTime;
        public float dsdx, dtdx, dtdy, dsdy, tx, ty;
        public float xPrecision, yPrecision;
        public float xCursorPosition, yCursorPosition;
        public float dsdxRaw, dtdxRaw, dtdyRaw, dsdyRaw, txRaw, tyRaw;
        public fixed byte pointers[1024]; // Pointer数组的简化
        
        // 辅助方法获取指针数据
        public ref PointerCoords GetPointerCoords(int index)
        {
            fixed (byte* ptr = pointers)
            {
                // 每个Pointer包含: PointerProperties(8字节) + PointerCoords(~280字节)
                int offset = index * 288; // 近似偏移
                return ref *(PointerCoords*)(ptr + offset + 8);
            }
        }
    }

    // ============ Delegates ============

    /// <summary>
    /// InputConsumer::initializeMotionEvent 签名
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeMotionEventDelegate(IntPtr consumer, IntPtr motionEvent, IntPtr inputMessage);

    /// <summary>
    /// publishMotionEvent函数签名 - API 30-34
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int PublishMotionEventDelegate_API30(
        IntPtr thiz, uint seq, int eventId, int deviceId, int source, int displayId,
        IntPtr hmac, int action, int actionButton, int flags, int metaState, int buttonState,
        int classification, IntPtr transform, float xPrecision, float yPrecision, 
        float xCursorPosition, float yCursorPosition, IntPtr rawTransform, 
        long downTime, long eventTime, uint pointerCount, 
        IntPtr pointerProperties, IntPtr pointerCoords);

    /// <summary>
    /// publishMotionEvent函数签名 - API 35+
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int PublishMotionEventDelegate_API35(
        IntPtr thiz, uint seq, int eventId, int deviceId, int source, int displayId,
        IntPtr hmac, int action, int actionButton, int flags, int metaState, int buttonState,
        int classification, IntPtr transform, float xPrecision, float yPrecision, 
        float xCursorPosition, float yCursorPosition, IntPtr rawTransform, 
        long downTime, long eventTime, uint pointerCount, 
        IntPtr pointerProperties, IntPtr pointerCoords);

    /// <summary>
    /// publishMotionEvent函数签名 - API 31-34 (Android 12-14, 没有rawTransform)
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int PublishMotionEventDelegate_API31(
        IntPtr thiz, uint seq, int eventId, int deviceId, int source, int displayId,
        IntPtr hmac, int action, int actionButton, int flags, int metaState, int buttonState,
        int classification, IntPtr transform, float xPrecision, float yPrecision, 
        float xCursorPosition, float yCursorPosition, int xCursorPositionInt, int yCursorPositionInt,
        long downTime, long eventTime, uint pointerCount, 
        IntPtr pointerProperties, IntPtr pointerCoords);

    // ============ Native Methods ============

    [DllImport("android")]
    private static extern int android_get_device_api_level();

    private static int GetApiLevel()
    {
        try
        {
            // 使用Android NDK的方法获取API级别
            return android_get_device_api_level();
        }
        catch
        {
            // 如果失败，尝试读取系统属性
            try
            {
                string? sdkInt = Environment.GetEnvironmentVariable("ro.build.version.sdk");
                if (int.TryParse(sdkInt, out int level))
                    return level;
            }
            catch { }
            
            return 35; // 默认返回35
        }
    }

    // ============ Constants ============

    /// <summary>
    /// Motion动作常量
    /// </summary>
    public static class MotionAction
    {
        public const int ACTION_DOWN = 0;        // 按下
        public const int ACTION_UP = 1;          // 抬起
        public const int ACTION_MOVE = 2;        // 移动/拖动
        public const int ACTION_CANCEL = 3;      // 取消
        public const int ACTION_OUTSIDE = 4;     // 外部
        public const int ACTION_POINTER_DOWN = 5;  // 多点触控按下
        public const int ACTION_POINTER_UP = 6;    // 多点触控抬起
        public const int ACTION_HOVER_MOVE = 7;    // 悬停移动
        public const int ACTION_SCROLL = 8;        // 滚动
        public const int ACTION_HOVER_ENTER = 9;   // 悬停进入
        public const int ACTION_HOVER_EXIT = 10;   // 悬停退出

        public static string GetActionName(int action)
        {
            return action switch
            {
                ACTION_DOWN => "DOWN",
                ACTION_UP => "UP",
                ACTION_MOVE => "MOVE",
                ACTION_CANCEL => "CANCEL",
                ACTION_OUTSIDE => "OUTSIDE",
                ACTION_POINTER_DOWN => "POINTER_DOWN",
                ACTION_POINTER_UP => "POINTER_UP",
                ACTION_HOVER_MOVE => "HOVER_MOVE",
                ACTION_SCROLL => "SCROLL",
                ACTION_HOVER_ENTER => "HOVER_ENTER",
                ACTION_HOVER_EXIT => "HOVER_EXIT",
                _ => $"UNKNOWN({action})"
            };
        }
    }

    /// <summary>
    /// 输入源类型
    /// </summary>
    public static class InputSource
    {
        public const int SOURCE_UNKNOWN = 0;
        public const int SOURCE_KEYBOARD = 0x00000101;
        public const int SOURCE_DPAD = 0x00000201;
        public const int SOURCE_GAMEPAD = 0x00000401;
        public const int SOURCE_TOUCHSCREEN = 0x00001002;
        public const int SOURCE_MOUSE = 0x00002002;
        public const int SOURCE_STYLUS = 0x00004002;
        public const int SOURCE_TOUCHPAD = 0x00100008;
        public const int SOURCE_JOYSTICK = 0x01000010;

        public static string GetSourceName(int source)
        {
            return source switch
            {
                SOURCE_KEYBOARD => "KEYBOARD",
                SOURCE_DPAD => "DPAD",
                SOURCE_GAMEPAD => "GAMEPAD",
                SOURCE_TOUCHSCREEN => "TOUCHSCREEN",
                SOURCE_MOUSE => "MOUSE",
                SOURCE_STYLUS => "STYLUS",
                SOURCE_TOUCHPAD => "TOUCHPAD",
                SOURCE_JOYSTICK => "JOYSTICK",
                _ => $"0x{source:X}"
            };
        }
    }

    /// <summary>
    /// PointerCoords轴常量
    /// </summary>
    public static class Axis
    {
        public const int AXIS_X = 0;
        public const int AXIS_Y = 1;
        public const int AXIS_PRESSURE = 2;
        public const int AXIS_SIZE = 3;
        public const int AXIS_TOUCH_MAJOR = 4;
        public const int AXIS_TOUCH_MINOR = 5;
        public const int AXIS_TOOL_MAJOR = 6;
        public const int AXIS_TOOL_MINOR = 7;
        public const int AXIS_ORIENTATION = 8;
    }
}

