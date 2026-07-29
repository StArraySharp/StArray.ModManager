# Upstream integration

## Phase 1: overlay touch

1. Compile `portable/native/starray_touch_bridge.cpp` and `starray_touch_jni.cpp` into `libstarray_modmanager.so` with the same ImGui instance used by the renderer.
2. Add `portable/java/.../StArrayTouchBridge.java` to the Android bootstrap DEX.
3. Add the `dispatchTouchEvent` routing shown in `TouchForwardingActivity.java` to the host `UnityPlayerActivity` subclass.
4. Add the DllImports from `portable/managed/StArrayTouchNative.cs`.
5. Call `DrainForwardedMotionEvents()` after backend platform-frame setup and before `ImGui.NewFrame()`.
6. Publish every visible overlay window/input region with `BeginOverlayInputFrame`, `AddOverlayInputRect` and `EndOverlayInputFrame`.
7. Call `SetOverlayVisible(false)` before disposing or hiding the renderer.

Expected render order:

```csharp
ImGuiImplOpenGL3.NewFrame();
UpdatePlatformFrame(width, height);
StArrayTouchNative.DrainForwardedMotionEvents();
ImGui.NewFrame();
```

Do not call ImGui from `dispatchTouchEvent`.

## Phase 2: original Unity modal ownership

Only add this phase if ModManager can open a MOD-owned Unity Canvas/IMGUI settings surface.

1. Set native modal state before hiding the ModManager overlay.
2. While modal is active, Activity touch routing must go directly to `super.dispatchTouchEvent`.
3. Back `ACTION_UP` requests modal close and is consumed.
4. Close/fault/unload clears modal state before restoring the ModManager overlay.
5. If gameplay still reacts, add a game-specific gameplay gate. Do not consume the Activity event, because the original Unity UI still needs it.

The internal `EventSystem.Update` detour is not portable Unity API. It depends on exact IL2CPP metadata resolution and the internal HookBroker, so upstream should design its own gate or omit this phase.

## Activity extension points

The internal game Activity also has PcCompat and AsyncInput observers. Upstream does not need them. If an application has its own observer, place it after overlay forwarding declines ownership and before `super.dispatchTouchEvent`:

```text
modal? -> Unity modal
overlay owns? -> consume
optional observe-only producer
optional async gameplay producer
Unity/gameplay
```

## Build requirements

- Android NDK with C++17 support.
- `android/input.h`, JNI and pthread.
- The same Dear ImGui/cimgui version used by the Android renderer.
- `imgui_internal.h` only if retaining the included direct-scroll behavior.

## Migration cautions

- Do not install a second ImGui context for the bridge.
- Do not drain from the Activity/UI thread.
- Do not consume the whole screen merely because the overlay is visible.
- Do not use `WantCaptureMouse` as the current Android event ownership decision.
- Do not send modal events to both ModManager and the original Unity modal.
