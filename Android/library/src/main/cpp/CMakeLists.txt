# Sets the minimum CMake version required
cmake_minimum_required(VERSION 3.22.1)

# Project name and version
project("modmanager" VERSION 1.0.0 LANGUAGES C CXX)

# ---- Build options ----
set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# ---- Debug / Release flags ----
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    add_compile_options(-O0 -g -fno-omit-frame-pointer)
else()
    add_compile_options(-O2 -DNDEBUG)
endif()

add_compile_options(-Wno-deprecated-declarations)

# ---- Source files ----
set(MODMANAGER_SOURCES
    core/dobby_hook.cpp
    core/hook_broker.cpp
    core/jni_helper.c
    core/misc.c
)

set(MONODROID_SOURCES
    core/mono_droid_coreclr.c
)

# ---- libmodmanager.so (Dobby + UnityResolve + log_write) ----
add_library(modmanager SHARED ${MODMANAGER_SOURCES})

# ---- libmonodroid.so (CoreCLR JNI host) ----
add_library(monodroid SHARED ${MONODROID_SOURCES})
set_target_properties(monodroid PROPERTIES
    C_STANDARD 11
    C_STANDARD_REQUIRED ON
)

# ---- Prebuilt: Dobby (static library) ----
add_library(dobby STATIC IMPORTED)
set_target_properties(dobby PROPERTIES
    IMPORTED_LOCATION ${CMAKE_CURRENT_SOURCE_DIR}/libs/dobby/libdobby.a
)

# ---- Prebuilt: CoreCLR (shared, embedded .NET runtime) ----
add_library(coreclr SHARED IMPORTED)
set_target_properties(coreclr PROPERTIES
    IMPORTED_LOCATION ${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libcoreclr.so
)

add_library(clrjit SHARED IMPORTED)
set_target_properties(clrjit PROPERTIES
    IMPORTED_LOCATION ${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libclrjit.so
)

# ---- cimgui: build from source as shared library ----
set(IMGUI_STATIC OFF CACHE BOOL "" FORCE)
set(IMGUI_FREETYPE OFF CACHE BOOL "" FORCE)

set(CIMGUI_DIR ${CMAKE_CURRENT_SOURCE_DIR}/libs/cimgui)
add_subdirectory(${CIMGUI_DIR} ${CMAKE_CURRENT_BINARY_DIR}/cimgui)

# ---- Include directories (modmanager) ----
target_include_directories(modmanager
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/core
        ${CIMGUI_DIR}
        ${CIMGUI_DIR}/imgui
        ${CMAKE_CURRENT_SOURCE_DIR}/libs/dobby/include
        ${CMAKE_CURRENT_SOURCE_DIR}/libs/unityresolve
)

# ---- Include directories (monodroid) ----
target_include_directories(monodroid
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/core
        ${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/include
)

# ---- Link libraries (modmanager) ----
find_library(log-lib log)

target_link_libraries(modmanager
    ${log-lib}
    dobby
    cimgui
    android
)

# ---- Link libraries (monodroid) ----
target_link_libraries(monodroid
    ${log-lib}
    coreclr
    clrjit
)

# ---- Post-build: copy CoreCLR runtime shared libs alongside libmonodroid.so ----
add_custom_command(TARGET monodroid POST_BUILD
    # CoreCLR runtime + JIT
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libcoreclr.so"
        "$<TARGET_FILE_DIR:monodroid>/libcoreclr.so"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libclrjit.so"
        "$<TARGET_FILE_DIR:monodroid>/libclrjit.so"
    # CoreCLR native host support libs
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libSystem.Native.so"
        "$<TARGET_FILE_DIR:monodroid>/libSystem.Native.so"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libSystem.Globalization.Native.so"
        "$<TARGET_FILE_DIR:monodroid>/libSystem.Globalization.Native.so"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libSystem.IO.Compression.Native.so"
        "$<TARGET_FILE_DIR:monodroid>/libSystem.IO.Compression.Native.so"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_CURRENT_SOURCE_DIR}/libs/coreclr/libSystem.Security.Cryptography.Native.Android.so"
        "$<TARGET_FILE_DIR:monodroid>/libSystem.Security.Cryptography.Native.Android.so"
    COMMENT "Copying CoreCLR runtime libs to output"
)
