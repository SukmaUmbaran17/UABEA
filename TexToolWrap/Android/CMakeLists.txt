cmake_minimum_required(VERSION 3.22.1)

project(textoolwrap)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(
    textoolwrap
    SHARED
    ../textoolwrap.cpp
)

target_include_directories(
    textoolwrap
    PRIVATE

    ../PVRTexLib/Include
    ../ispc/include
    ../crunch/inc
)

add_library(
    PVRTexLib
    SHARED
    IMPORTED
)

set_target_properties(
    PVRTexLib
    PROPERTIES
    IMPORTED_LOCATION
    "${CMAKE_CURRENT_SOURCE_DIR}/../PVRTexLib/Android/${ANDROID_ABI}/libPVRTexLib.so"
)

add_library(
    ispc_texcomp
    SHARED
    IMPORTED
)

set_target_properties(
    ispc_texcomp
    PROPERTIES
    IMPORTED_LOCATION
    "${CMAKE_CURRENT_SOURCE_DIR}/../ispc/Android/${ANDROID_ABI}/libispc_texcomp.so"
)

add_library(
    crnlib
    SHARED
    IMPORTED
)

set_target_properties(
    crnlib
    PROPERTIES
    IMPORTED_LOCATION
    "${CMAKE_CURRENT_SOURCE_DIR}/../crunch/Android/${ANDROID_ABI}/libcrnlib.so"
)

target_link_libraries(
    textoolwrap
    PVRTexLib
    ispc_texcomp
    crnlib
    log
)
