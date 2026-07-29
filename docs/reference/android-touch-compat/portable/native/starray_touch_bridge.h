#pragma once

#ifdef __cplusplus
extern "C" {
#endif

int modmanager_touch_forward_motion_event(
    int action,
    float x,
    float y,
    int tool_type,
    int button_state);

int modmanager_imgui_drain_forwarded_motion_events(void);

void modmanager_overlay_touch_begin_frame(void);
void modmanager_overlay_touch_add_rect(float x, float y, float width, float height);
void modmanager_overlay_touch_commit_frame(void);

int modmanager_overlay_ui_is_visible(void);
void modmanager_overlay_ui_set_visible(int visible);
void modmanager_overlay_input_request_focus_release(void);

int modmanager_modal_input_is_active(void);
void modmanager_modal_input_set_active(int active);
void modmanager_modal_input_request_close(void);
int modmanager_modal_input_take_close_request(void);

#ifdef __cplusplus
}
#endif
