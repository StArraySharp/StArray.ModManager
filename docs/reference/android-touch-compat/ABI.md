# ABI reference

## Portable native exports

| Export | Purpose |
| --- | --- |
| `modmanager_touch_forward_motion_event` | Flattened Android event ingress; returns overlay ownership |
| `modmanager_imgui_drain_forwarded_motion_events` | Render-thread queue drain into current ImGui context |
| `modmanager_overlay_touch_begin_frame` | Begin pending rectangle publication |
| `modmanager_overlay_touch_add_rect` | Add one pixel-space overlay rectangle |
| `modmanager_overlay_touch_commit_frame` | Atomically publish pending rectangles |
| `modmanager_overlay_ui_is_visible` | Query overlay visibility |
| `modmanager_overlay_ui_set_visible` | Set visibility and request focus release when hidden |
| `modmanager_overlay_input_request_focus_release` | Release touch/button state on next drain |
| `modmanager_modal_input_is_active` | Query Unity modal ownership |
| `modmanager_modal_input_set_active` | Enter/leave modal ownership and clear queued overlay events |
| `modmanager_modal_input_request_close` | Publish a Back close request |
| `modmanager_modal_input_take_close_request` | Atomically consume a close request |

## JNI methods

The portable Java class is `starray.android.modmanager.touch.StArrayTouchBridge`.

| Java method | JNI result |
| --- | --- |
| `nativeForwardMotionEvent(int,float,float,int,int)` | `boolean` |
| `nativeSetOverlayVisible(boolean)` | `void` |
| `nativeSetModalInputCapture(boolean)` | `void` |
| `nativeIsModalInputCaptureActive()` | `int` |
| `nativeRequestModalClose()` | `void` |
| `nativeTakeModalCloseRequest()` | `int` |

## Event fields

The overlay bridge intentionally stores only fields needed by ImGui:

- masked action
- x/y in display pixels
- tool type
- button state

PcCompat observation stores pointer identity, pointer count, event time, viewport, source, device and flags in a separate journal. Those fields are not needed for ModManager overlay interaction and are not part of the portable ABI.

## Calling rules

- `modmanager_touch_forward_motion_event`: Android Activity thread, any time after native library load.
- rectangle and visibility APIs: managed render/UI owner.
- drain: thread owning the current ImGui context, before `ImGui.NewFrame`.
- modal state: lifecycle coordinator; set native state before changing Java/overlay mirrors.
