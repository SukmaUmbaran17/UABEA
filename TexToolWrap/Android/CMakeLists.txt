cmake_minimum_required(VERSION 3.22.1)

project(TexToolWrapAndroid)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# ============================================================
# ANDROID
# ============================================================

if(NOT ANDROID)
    message(FATAL_ERROR "TexToolWrap Android harus dibuild menggunakan Android NDK")
endif()

message(STATUS "=======================================")
message(STATUS "TexToolWrap Android")
message(STATUS "=======================================")
message(STATUS "ANDROID_ABI       = ${ANDROID_ABI}")
message(STATUS "ANDROID_PLATFORM  = ${ANDROID_PLATFORM}")
message(STATUS "ANDROID_NDK       = ${CMAKE_ANDROID_NDK}")
message(STATUS "=======================================")


# ============================================================
# PATH
# ============================================================

get_filename_component(TEXTOOLWRAP_ROOT
    "${CMAKE_CURRENT_SOURCE_DIR}/.."
    ABSOLUTE
)

set(PVRTEXLIB_ROOT
    "${TEXTOOLWRAP_ROOT}/PVRTexLib"
)

set(ISPC_ROOT
    "${TEXTOOLWRAP_ROOT}/ispc"
)

set(CRUNCH_ROOT
    "${TEXTOOLWRAP_ROOT}/crunch"
)

message(STATUS "TexToolWrap root = ${TEXTOOLWRAP_ROOT}")
message(STATUS "PVRTexLib root   = ${PVRTEXLIB_ROOT}")
message(STATUS "ISPC root        = ${ISPC_ROOT}")
message(STATUS "Crunch root      = ${CRUNCH_ROOT}")


# ============================================================
# VERIFY DIRECTORIES
# ============================================================

if(NOT EXISTS "${TEXTOOLWRAP_ROOT}/textoolwrap.cpp")
    message(FATAL_ERROR
        "textoolwrap.cpp tidak ditemukan: ${TEXTOOLWRAP_ROOT}/textoolwrap.cpp"
    )
endif()

if(NOT EXISTS "${PVRTEXLIB_ROOT}")
    message(FATAL_ERROR
        "PVRTexLib tidak ditemukan: ${PVRTEXLIB_ROOT}"
    )
endif()

if(NOT EXISTS "${ISPC_ROOT}")
    message(WARNING
        "ISPC tidak ditemukan: ${ISPC_ROOT}"
    )
endif()

if(NOT EXISTS "${CRUNCH_ROOT}")
    message(WARNING
        "Crunch tidak ditemukan: ${CRUNCH_ROOT}"
    )
endif()


# ============================================================
# FIND PVRTEXLIB SOURCE
# ============================================================

file(GLOB_RECURSE PVRTEXLIB_CPP
    "${PVRTEXLIB_ROOT}/*.cpp"
)

file(GLOB_RECURSE PVRTEXLIB_C
    "${PVRTEXLIB_ROOT}/*.c"
)

file(GLOB_RECURSE PVRTEXLIB_HEADERS
    "${PVRTEXLIB_ROOT}/*.h"
)

list(APPEND PVRTEXLIB_SOURCES
    ${PVRTEXLIB_CPP}
    ${PVRTEXLIB_C}
)

# Remove obvious non-source directories/files
list(FILTER PVRTEXLIB_SOURCES EXCLUDE REGEX "/Android/build/")
list(FILTER PVRTEXLIB_SOURCES EXCLUDE REGEX "/android/build/")
list(FILTER PVRTEXLIB_SOURCES EXCLUDE REGEX "/build/")
list(FILTER PVRTEXLIB_SOURCES EXCLUDE REGEX "/tests?/")
list(FILTER PVRTEXLIB_SOURCES EXCLUDE REGEX "/test/")
list(FILTER PVRTEXLIB_SOURCES EXCLUDE REGEX "/examples?/")


list(LENGTH PVRTEXLIB_SOURCES PVRTEXLIB_SOURCE_COUNT)

message(STATUS "PVRTexLib source count = ${PVRTEXLIB_SOURCE_COUNT}")


# ============================================================
# PVRTEXLIB TARGET
# ============================================================

if(PVRTEXLIB_SOURCE_COUNT GREATER 0)

    message(STATUS "Building PVRTexLib from source")

    add_library(
        PVRTexLib
        STATIC
        ${PVRTEXLIB_SOURCES}
    )

    target_include_directories(
        PVRTexLib
        PUBLIC
            "${PVRTEXLIB_ROOT}"
    )

    # Common include directories if present
    if(EXISTS "${PVRTEXLIB_ROOT}/include")
        target_include_directories(
            PVRTexLib
            PUBLIC
                "${PVRTEXLIB_ROOT}/include"
        )
    endif()

    if(EXISTS "${PVRTEXLIB_ROOT}/src")
        target_include_directories(
            PVRTexLib
            PUBLIC
                "${PVRTEXLIB_ROOT}/src"
        )
    endif()

    target_compile_definitions(
        PVRTexLib
        PRIVATE
            ANDROID
    )

    set_target_properties(
        PVRTexLib
        PROPERTIES
            POSITION_INDEPENDENT_CODE ON
    )

else()

    message(FATAL_ERROR
        "Tidak menemukan source C/C++ PVRTexLib."
    )

endif()


# ============================================================
# TEXTOOLWRAP SOURCE
# ============================================================

add_library(
    textoolwrap
    SHARED
        "${TEXTOOLWRAP_ROOT}/textoolwrap.cpp"
)


# ============================================================
# TEXTOOLWRAP INCLUDE
# ============================================================

target_include_directories(
    textoolwrap
    PRIVATE

        "${TEXTOOLWRAP_ROOT}"

        "${PVRTEXLIB_ROOT}"
)

if(EXISTS "${PVRTEXLIB_ROOT}/include")
    target_include_directories(
        textoolwrap
        PRIVATE
            "${PVRTEXLIB_ROOT}/include"
    )
endif()

if(EXISTS "${PVRTEXLIB_ROOT}/src")
    target_include_directories(
        textoolwrap
        PRIVATE
            "${PVRTEXLIB_ROOT}/src"
    )
endif()


# ============================================================
# COMPILE DEFINITIONS
# ============================================================

target_compile_definitions(
    textoolwrap
    PRIVATE
        ANDROID
)


# ============================================================
# LINK PVRTEXLIB
# ============================================================

target_link_libraries(
    textoolwrap
    PRIVATE
        PVRTexLib
)


# ============================================================
# ANDROID SYSTEM LIBRARIES
# ============================================================

find_library(
    ANDROID_LOG_LIB
    log
)

find_library(
    ANDROID_DL_LIB
    dl
)

find_library(
    ANDROID_M_LIB
    m
)

target_link_libraries(
    textoolwrap
    PRIVATE
        ${ANDROID_LOG_LIB}
        ${ANDROID_DL_LIB}
        ${ANDROID_M_LIB}
)


# ============================================================
# WARNINGS
# ============================================================

if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")

    target_compile_options(
        PVRTexLib
        PRIVATE
            -fPIC
            -Wno-unused-parameter
            -Wno-unused-variable
            -Wno-unused-function
            -Wno-deprecated-declarations
    )

    target_compile_options(
        textoolwrap
        PRIVATE
            -fPIC
            -Wno-unused-parameter
            -Wno-unused-variable
            -Wno-unused-function
            -Wno-deprecated-declarations
    )

endif()


# ============================================================
# OUTPUT
# ============================================================

set_target_properties(
    textoolwrap
    PROPERTIES
        OUTPUT_NAME "textoolwrap"
)


# ============================================================
# BUILD INFORMATION
# ============================================================

message(STATUS "")
message(STATUS "=======================================")
message(STATUS "TEXTOOLWRAP CONFIGURATION COMPLETE")
message(STATUS "=======================================")
message(STATUS "Target       : textoolwrap")
message(STATUS "Architecture : ${ANDROID_ABI}")
message(STATUS "PVRTexLib    : enabled")
message(STATUS "Output       : libtextoolwrap.so")
message(STATUS "=======================================")
