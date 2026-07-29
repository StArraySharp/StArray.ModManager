# Test matrix

## Deterministic contracts

- Hidden overlay returns not-consumed and does not grow the native queue.
- Visible overlay outside all rectangles returns not-consumed.
- `DOWN` inside a rectangle latches ownership through move and release outside.
- `CANCEL` releases ownership and ImGui mouse button 0.
- Hiding the overlay releases an active ImGui ID on the next drain.
- Entering modal clears pending overlay events.
- Modal Activity routing bypasses ModManager forwarding and reaches Unity.
- Drain copies and clears under lock, then calls ImGui after unlocking.
- Drain is invoked before `ImGui.NewFrame`.
- Rectangle publication exposes only the last committed frame.

## Device scenarios

| Scenario | Expected result |
| --- | --- |
| Tap ModManager button | One activation, no gameplay click-through |
| Drag vertical list | Scroll starts after threshold; initial row/button is not activated |
| Drag out of overlay then release | Gesture remains consumed until release |
| Tap game outside overlay window | Game receives input; overlay does not activate |
| Hide overlay while finger held | No stuck ImGui mouse-down after reopen |
| Android `ACTION_CANCEL` | No stuck button or scroll state |
| Stylus/mouse input | Correct ImGui source and mouse button mapping |
| Two gameplay fingers outside overlay | Gameplay still receives both pointers |
| Open original Unity settings modal | Original UI receives touch; ModManager does not |
| Back from original modal | Close request occurs once on `ACTION_UP` |
| Activity pause/resume and recreation | Forwarding resumes without duplicate native state |
| 2400x1080 with cutout/insets | Touch coordinates align with rendered widgets |

## Stress checks

- Inject more than 128 events between two render frames and verify overflow is visible in diagnostics before enabling general-purpose use.
- Repeatedly show/hide overlay while tapping and confirm no stale queued `DOWN` survives.
- Alternate overlay and Unity modal ownership for at least 100 cycles.
- Run at 60/90/120 Hz and verify scroll distance remains tied to physical movement, not frame count.

## Internal evidence

The snapshot was exercised in the internal build with Android input contracts `21/21`, managed tests `694/694`, arm64 Release build, JNI export audit `100/100`, and generated proxy audit with zero issues. These numbers cover the complete internal runtime, not just the portable extraction; upstream should add focused tests around its adopted version.
