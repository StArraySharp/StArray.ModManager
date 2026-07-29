# Android touch compatibility reference

This directory packages the Android touch path used by the internal StArray ModManager build for upstream review. It is reference material and is not wired into the upstream build.

Source snapshot:

- Internal commit: `e9392efb63a3be443b27ef544574520d15fc842a`
- Snapshot date: `2026-07-29`
- Validated target: Android 13, Unity 6.0.3 / IL2CPP, arm64-v8a
- Internal regression baseline: Android input contracts `21/21`, managed suite `694/694`

## What this solves

The upstream Android renderer can draw ImGui, but Android `MotionEvent` objects do not automatically reach that ImGui context. The internal implementation establishes an explicit path:

```text
Activity.dispatchTouchEvent
  -> Java event flattening
  -> bounded native queue
  -> render-thread drain before ImGui.NewFrame
  -> ImGuiIO.AddMouse*Event
```

It also separates three owners:

1. ModManager overlay: consumes only gestures that start in the overlay input rectangles.
2. Original Unity modal UI: receives the original `MotionEvent` through `super.dispatchTouchEvent`.
3. Gameplay: receives events not owned by either UI path.

## Directory layout

- `snapshot/`: exact internal production files. These contain game-specific, licensing and PcCompat code and are not drop-in upstream files.
- `portable/`: a small standalone extraction with no PcCompat journal, HookBroker or game package dependency.
- `ARCHITECTURE.md`: ownership, threading and gesture state machines.
- `INTEGRATION.md`: recommended upstream adoption order.
- `ABI.md`: native/JNI/managed entry points.
- `TEST_MATRIX.md`: contracts and device checks required before enabling by default.

## Recommended review order

1. Read `ARCHITECTURE.md` and `ABI.md`.
2. Review `portable/native/starray_touch_bridge.cpp`.
3. Compare it with `snapshot/native/cimgui_compat.cpp`.
4. Review the Activity and render-frame integration examples.
5. Integrate the overlay path first. Treat original MOD modal ownership and gameplay blocking as an optional second phase.

## Deliberate exclusions

The portable implementation does not include:

- PcCompat raw touch journal or KeyViewer lane projection.
- AsyncInput observer registration.
- Unity `EventSystem.Update` detour.
- IME ownership and hidden keyboard view management.
- License gates, runtime bootstrap, HUD drawing or MOD lifecycle.

Those systems are adjacent to touch routing but are not required to make the ModManager overlay touch-capable.

## Current limitations

- ImGui represents touch as one mouse pointer. Multi-touch gameplay events are not consumed unless their gesture starts inside an overlay input rectangle.
- The production queue is fixed at 128 events and drops the oldest event on overflow. Upstream should add overflow telemetry and synthesize a cancel before treating this as a general-purpose input transport.
- Scroll support uses `imgui_internal.h` to find and move the hovered scroll window. Upstream may replace this with an application-level gesture-to-wheel policy if it wants to avoid ImGui internals.
- Activity coordinates must match the ImGui display coordinate space. Surface scaling, cutouts or non-fullscreen rendering require an explicit transform.
