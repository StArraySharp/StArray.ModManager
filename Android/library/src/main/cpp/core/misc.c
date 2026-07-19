//
// Created by StArray on 2026/7/19.
//
#include <stdio.h>
#include <dlfcn.h>
void* modmanager_il2cpp_init() {
    void* handle = dlopen("libil2cpp.so", RTLD_LAZY | RTLD_NOLOAD);
    if (dlerror() == NULL) {
        return handle;
    }
    return 0;
}
