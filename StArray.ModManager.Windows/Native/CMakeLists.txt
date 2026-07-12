cmake_minimum_required(VERSION 3.20)
project(StArray.ModManager.Windows.Native LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(MSVC)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

# Statically link GCC/C++ runtime
if(MINGW)
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -static -static-libgcc -static-libstdc++")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -static -static-libgcc -static-libstdc++")
endif()

# ---- MinHook (static lib) ----
add_library(MinHook STATIC
    minhook/src/buffer.c
    minhook/src/hook.c
    minhook/src/trampoline.c
    minhook/src/hde/hde32.c
    minhook/src/hde/hde64.c
)
target_include_directories(MinHook PUBLIC minhook/include)
target_compile_definitions(MinHook PRIVATE _CRT_SECURE_NO_WARNINGS)

# ---- cimgui: build from source as static library ----
set(IMGUI_STATIC ON CACHE BOOL "" FORCE)
set(CIMGUI_DIR ${CMAKE_CURRENT_SOURCE_DIR}/cimgui)
add_subdirectory(${CIMGUI_DIR} ${CMAKE_CURRENT_BINARY_DIR}/cimgui)

# ---- Main hook DLL ----
add_library(${PROJECT_NAME} SHARED
        main.cpp
        dx9hook.cpp
        dx11hook.cpp
        dx12hook.cpp
)
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")
target_include_directories(${PROJECT_NAME} PRIVATE
    minhook/include
    ${CIMGUI_DIR}
    ${CIMGUI_DIR}/imgui
    ${CIMGUI_DIR}/imgui/backends
)
target_link_libraries(${PROJECT_NAME} PRIVATE MinHook d3d11 d3dcompiler dxgi dwmapi gdi32 cimgui)

add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_CURRENT_SOURCE_DIR}/../bin/${CMAKE_BUILD_TYPE}/net10.0"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "$<TARGET_FILE:${PROJECT_NAME}>"
        "${CMAKE_CURRENT_SOURCE_DIR}/../bin/${CMAKE_BUILD_TYPE}/net10.0/"
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "$<TARGET_FILE:${PROJECT_NAME}>"
        "E:\\adofai\\Stable\\Corehold\\managed"
)
