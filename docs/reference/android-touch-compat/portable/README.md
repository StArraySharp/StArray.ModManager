# Portable extraction

This directory is an upstream-oriented extraction, not an exact copy.

## Differences from the production snapshot

- JNI uses the neutral package `starray.android.modmanager.touch`.
- PcCompat and AsyncInput observer calls are removed.
- License, runtime bootstrap, IME and game-specific code are removed.
- Queue overflow requests an ImGui cancel before replaying retained events.
- Overlay hide clears queued events before requesting focus release.
- Modal enter clears queued events and requests focus release directly.
- Native ingress is exposed as `modmanager_touch_forward_motion_event`, with thin JNI wrappers in a separate file.

The production snapshot remains the behavioral evidence. The portable version is the recommended implementation starting point because its dependencies and ownership are explicit.

## Files

- `native/starray_touch_bridge.h`: stable C ABI.
- `native/starray_touch_bridge.cpp`: queue, rectangles, ownership and ImGui drain.
- `native/starray_touch_jni.cpp`: Java package-specific JNI wrappers.
- `java/.../StArrayTouchBridge.java`: `MotionEvent` flattening and modal state.
- `java/.../TouchForwardingActivity.java`: host Activity routing example.
- `managed/StArrayTouchNative.cs`: DllImport surface.
- `managed/TouchFrameIntegration.cs`: render order and rectangle publication example.

The Java class assumes `libstarray_modmanager.so` is already loaded by the existing bootstrap. Add `System.loadLibrary("starray_modmanager")` only if upstream does not already guarantee that order.

## Native compilation check

The extraction was syntax-checked with Android NDK `25.2.9519653`, target `aarch64-linux-android26`, C++17 and the repository's Dear ImGui 1.91.6 headers. The Java examples were compiled with JDK 17 against Android 34 using Java 8 source/target compatibility.
