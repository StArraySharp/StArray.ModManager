cmake_minimum_required(VERSION 3.20)
project(StArray.ModManager.Windows.Native LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(MSVC)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

# ---- MinHook (submodule, static lib) ----
add_library(MinHook STATIC
    minhook/src/buffer.c
    minhook/src/hook.c
    minhook/src/trampoline.c
    minhook/src/hde/hde32.c
    minhook/src/hde/hde64.c
)
target_include_directories(MinHook PUBLIC minhook/include)
target_compile_definitions(MinHook PRIVATE _CRT_SECURE_NO_WARNINGS)

# ---- kiero2 (submodule, static lib, all backends except D3D10) ----
add_library(kiero STATIC
    kiero2/kiero_intern.cpp
    kiero2/kiero_d3d9.cpp
    kiero2/kiero_d3d11.cpp
    kiero2/kiero_d3d12.cpp
    kiero2/kiero_opengl.cpp
    kiero2/kiero_vulkan.cpp
)
target_include_directories(kiero PUBLIC kiero2)
target_compile_definitions(kiero PRIVATE KIERO_BUILD_D3D9 KIERO_BUILD_D3D11 KIERO_BUILD_D3D12 KIERO_BUILD_OPENGL KIERO_BUILD_VULKAN)

# ---- Main hook DLL ----
add_library(${PROJECT_NAME} SHARED main.cpp)
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")  # no "lib" prefix
target_include_directories(${PROJECT_NAME} PRIVATE . kiero2 minhook/include)
target_link_libraries(${PROJECT_NAME} PRIVATE kiero MinHook d3d12 dxgi ${CMAKE_CURRENT_SOURCE_DIR}/libcimgui.a)

# Copy DLL to C# build output
add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "$<TARGET_FILE:${PROJECT_NAME}>"
        "${CMAKE_CURRENT_SOURCE_DIR}/../bin/${CMAKE_BUILD_TYPE}/net10.0/"
)
