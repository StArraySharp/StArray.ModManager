cmake_minimum_required(VERSION 3.15)
cmake_policy(SET CMP0091 NEW)
project(cimgui)

set (CMAKE_CXX_STANDARD 20)

if (EXISTS ${CMAKE_CURRENT_SOURCE_DIR}/imgui/imgui_tables.cpp)
	set(TABLES_SOURCE "imgui/imgui_tables.cpp")
else()
	set(TABLES_SOURCE "")
endif()


#general settings
file(GLOB IMGUI_BASE_SOURCES
    cimgui.cpp
    imgui/imgui.cpp
    imgui/imgui_draw.cpp
    imgui/imgui_demo.cpp
    imgui/imgui_widgets.cpp
	${TABLES_SOURCE}
)

# Backends — platform auto-detect
if (WIN32 OR MINGW OR MSYS)
    list(APPEND IMGUI_BASE_SOURCES
        imgui/backends/imgui_impl_win32.cpp
        imgui/backends/imgui_impl_dx9.cpp
        imgui/backends/imgui_impl_dx11.cpp
        imgui/backends/imgui_impl_dx12.cpp
        imgui/backends/imgui_impl_opengl3.cpp
    )
elseif (ANDROID)
    list(APPEND IMGUI_BASE_SOURCES
        imgui/backends/imgui_impl_android.cpp
        imgui/backends/imgui_impl_opengl3.cpp
        imgui/backends/imgui_impl_vulkan.cpp
    )
else()
    # Linux / macOS / etc.
    list(APPEND IMGUI_BASE_SOURCES
        imgui/backends/imgui_impl_opengl3.cpp
        imgui/backends/imgui_impl_vulkan.cpp
    )
endif()

set(IMGUI_SOURCES ${IMGUI_BASE_SOURCES})

set(IMGUI_STATIC "no" CACHE STRING "Build as a static library")
set(IMGUI_FREETYPE "no" CACHE STRING "Build with freetype library (static)")
set(IMGUI_LIBRARIES )

#add library and link
if (IMGUI_STATIC)
    add_library(cimgui STATIC ${IMGUI_SOURCES})
else (IMGUI_STATIC)
    add_library(cimgui SHARED ${IMGUI_SOURCES})
endif (IMGUI_STATIC)

if(IMGUI_FREETYPE)
	target_compile_definitions(cimgui PUBLIC IMGUI_ENABLE_FREETYPE)
	target_compile_definitions(cimgui PUBLIC IMGUI_ENABLE_STB_TRUETYPE)
endif(IMGUI_FREETYPE)

if(IMGUI_WCHAR32)
	target_compile_definitions(cimgui PUBLIC IMGUI_USE_WCHAR32)
endif(IMGUI_WCHAR32)

add_definitions("-DIMGUI_USER_CONFIG=\"../cimconfig.h\"")
target_compile_definitions(cimgui PUBLIC IMGUI_DISABLE_OBSOLETE_FUNCTIONS=1)
if (WIN32 OR MINGW OR MSYS)
    target_compile_definitions(cimgui PUBLIC IMGUI_IMPL_API=__declspec\(dllexport\))
    list(APPEND IMGUI_LIBRARIES imm32 d3d9 d3d11 d3d12 d3dcompiler dxgi dwmapi gdi32 opengl32)
elseif (ANDROID)
    target_compile_definitions(cimgui PUBLIC IMGUI_IMPL_API=extern\t\"C\"\t)
    list(APPEND IMGUI_LIBRARIES log android vulkan EGL GLESv3)
else()
    target_compile_definitions(cimgui PUBLIC IMGUI_IMPL_API=extern\t\"C\"\t)
endif()

target_include_directories(cimgui PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_include_directories(cimgui PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/imgui)
set_target_properties(cimgui PROPERTIES PREFIX "")
target_link_libraries(cimgui ${IMGUI_LIBRARIES})

#install
install(TARGETS cimgui
    RUNTIME DESTINATION  .
    LIBRARY DESTINATION  .
    ARCHIVE DESTINATION  .
)

#test
set(CIMGUI_TEST "no" CACHE STRING "Enable compilation of a test unit based on imgui null")

if (CIMGUI_TEST)
  add_subdirectory(test)
endif ()
