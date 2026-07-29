# Internal production snapshot

These files are copied unchanged from internal commit `e9392efb63a3be443b27ef544574520d15fc842a`.

| Snapshot file | Internal source |
| --- | --- |
| `native/cimgui_compat.cpp` | `StArray.ModManager/Android/library/src/main/cpp/core/cimgui_compat.cpp` |
| `java/StArrayModManagerBootstrap.java` | `StArray.ModManager/Android/library/src/main/java/com/fizzd/connectedworlds/editorport/StArrayModManagerBootstrap.java` |
| `java/ExtraMenuUnityPlayerActivity.java` | `extra_menu_activity/src/com/fizzd/connectedworlds/editorport/ExtraMenuUnityPlayerActivity.java` |
| `managed/ImGuiBackends.cs` | `StArray.ModManager/StArray.ModManager.Android/Native/ImGuiBackends.cs` |
| `managed/ImGuiEGLRender.cs` | `StArray.ModManager/StArray.ModManager.Android/UI/ImGuiEGLRender.cs` |
| `managed/ImGuiInputHandler.cs` | `StArray.ModManager/StArray.ModManager.Android/UI/ImGuiInputHandler.cs` |
| `managed/AndroidModManagerPlatformServices.cs` | `StArray.ModManager/StArray.ModManager.Android/UI/AndroidModManagerPlatformServices.cs` |

The snapshot is evidence, not a proposed patch. In particular:

- `cimgui_compat.cpp` references PcCompat observer functions and realtime input state.
- `StArrayModManagerBootstrap.java` contains runtime startup, IME and application-specific bootstrap code.
- `ExtraMenuUnityPlayerActivity.java` belongs to the ADOFAI mobile host and contains AsyncInput/application routing.
- managed files include internal service abstractions and renderer changes unrelated to touch.

Use `portable/` for an upstream implementation starting point.
