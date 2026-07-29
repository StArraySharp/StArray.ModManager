# Architecture

## Ownership states

### Overlay hidden

- Java forwarding returns `false` without queueing.
- Unity/gameplay receives the original event.
- Hiding requests a focus release so a previous ImGui press cannot remain held.

### Overlay visible, gesture outside registered rectangles

- The event is queued so ImGui mouse position remains current.
- Native ownership returns `false`.
- Activity continues to Unity/gameplay.

### Overlay visible, gesture starts inside a registered rectangle

- `DOWN` latches overlay ownership.
- All subsequent `MOVE`, `UP` or `CANCEL` events in that gesture remain owned, even after leaving the rectangle.
- Activity returns `true`, preventing gameplay click-through.
- `UP` and `CANCEL` release ownership.

Ownership is based on the frame-committed rectangle set, not `ImGuiIO.WantCaptureMouse`. `WantCaptureMouse` is one frame late for deciding whether Android should dispatch the current `DOWN` to Unity.

### Original Unity modal active

- ModManager forwarding is bypassed.
- The real Android event is passed to `super.dispatchTouchEvent`, preserving the original MOD Canvas/IMGUI path.
- Gameplay observation is disabled for that event.
- Back is owned by the modal close path.
- Blocking Unity gameplay while preserving a specific Unity modal requires a game-specific gate. The internal build uses a metadata-resolved `EventSystem.Update` hook; it is intentionally absent from `portable/`.

## Thread boundary

```text
Android UI thread                    Unity/render thread
-----------------                    -------------------
dispatchTouchEvent
  flatten MotionEvent
  lock queue
  append event
  unlock queue
                                      backend NewFrame
                                      lock queue
                                      copy + clear queue
                                      unlock queue
                                      update ImGuiIO
                                      ImGui.NewFrame
```

No ImGui API is called from the Android UI thread. This is the primary safety property of the bridge.

## Queue policy

- Capacity: 128 flattened events.
- Synchronization: `pthread_mutex_t`.
- Overflow: drop oldest, retain newest.
- Hidden overlay or active Unity modal: do not enqueue.
- Drain: copy to a stack buffer, clear shared queue, then call ImGui outside the lock.

The shared mutex is never held while calling ImGui.

## Gesture mapping

| Android action | ImGui action |
| --- | --- |
| `DOWN`, `POINTER_DOWN` | source event, position, mouse button 0 down |
| `MOVE`, `HOVER_MOVE` | source event, position, optional touch scroll |
| `UP`, `POINTER_UP` | position, mouse button 0 up |
| `CANCEL` | mouse button 0 up and clear local gesture state |
| `BUTTON_PRESS/RELEASE` | map primary, secondary and tertiary mouse buttons |

For touch scrolling, vertical movement must exceed 8 physical pixels and dominate horizontal movement by a factor of 1.08. Once scrolling starts, the bridge releases mouse button 0 and clears the active ImGui ID so the initial press cannot also activate a button.

## Rectangle publication

The UI publishes input regions transactionally each rendered frame:

```text
begin_frame
  add_rect(window A)
  add_rect(window B)
commit_frame
```

The Android UI thread reads only the last committed set. It never sees a partially rebuilt list.

## Lifecycle invariants

- Overlay hide must release ImGui focus and button state.
- Modal enter must clear queued overlay events.
- Activity recreation must re-establish Java native method access.
- Queue draining must happen before `ImGui.NewFrame`.
- Input coordinates and `ImGuiIO.DisplaySize` must use the same pixel space.
