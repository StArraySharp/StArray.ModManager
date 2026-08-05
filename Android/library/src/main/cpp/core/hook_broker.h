#pragma once

#ifdef __cplusplus
extern "C" {
#endif

int modmanager_hook_broker_install(
    const char *owner,
    void *target,
    void *replacement,
    void **continuation_out);

int modmanager_hook_broker_get_chain_count(void);
int modmanager_hook_broker_get_layer_count(void *target);
const char *modmanager_hook_broker_get_last_error(void);

#ifdef __cplusplus
}
#endif
