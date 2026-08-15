cmake_minimum_required(VERSION 3.22.1)

project(TexToolWrapAndroid)

# ============================================================
# BASIC CONFIGURATION
# ============================================================

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

if(NOT ANDROID)
    message(FATAL_ERROR
        "TexToolWrap Android harus dibuild menggunakan Android NDK."
    )
endif()

message(STATUS "=======================================")
message(STATUS "TexToolWrap Android")
message(STATUS "=======================================")
message(STATUS "ANDROID_ABI       = ${ANDROID_ABI}")
message(STATUS "ANDROID_PLATFORM  = ${ANDROID_PLATFORM}")
message(STATUS "ANDROID_NDK       = ${CMAKE_ANDROID_NDK}")
message(STATUS "=======================================")


# ============================================================
# ROOT PATHS
# ============================================================

get_filename_component(
    TEXTOOLWRAP_ROOT
    "${CMAKE_CURRENT_SOURCE_DIR}/.."
    ABSOLUTE
)

set(
    PVRTEXLIB_ROOT
    "${TEXTOOLWRAP_ROOT}/PVRTexLib"
)

set(
    ISPC_ROOT
    "${TEXTOOLWRAP_ROOT}/ispc"
)

set(
    CRUNCH_ROOT
    "${TEXTOOLWRAP_ROOT}/crunch"
)

message(STATUS "TexToolWrap root = ${TEXTOOLWRAP_ROOT}")
message(STATUS "PVRTexLib root   = ${PVRTEXLIB_ROOT}")
message(STATUS "ISPC root        = ${ISPC_ROOT}")
message(STATUS "Crunch root      = ${CRUNCH_ROOT}")


# ============================================================
# VERIFY MAIN FILES
# ============================================================

if(NOT EXISTS "${TEXTOOLWRAP_ROOT}/textoolwrap.cpp")

    message(FATAL_ERROR
        "textoolwrap.cpp tidak ditemukan:\n"
        "${TEXTOOLWRAP_ROOT}/textoolwrap.cpp"
    )

endif()


if(NOT EXISTS "${PVRTEXLIB_ROOT}")

    message(FATAL_ERROR
        "PVRTexLib directory tidak ditemukan:\n"
        "${PVRTEXLIB_ROOT}"
    )

endif()


# ============================================================
# PVRTEXLIB INSPECTION
# ============================================================

message(STATUS "")
message(STATUS "=======================================")
message(STATUS "INSPECT PVRTEXLIB")
message(STATUS "=======================================")


file(
    GLOB_RECURSE
    PVRTEXLIB_ALL_FILES
    "${PVRTEXLIB_ROOT}/*"
)

list(LENGTH PVRTEXLIB_ALL_FILES PVRTEXLIB_FILE_COUNT)

message(
    STATUS
    "PVRTexLib total entries = ${PVRTEXLIB_FILE_COUNT}"
)


# ============================================================
# FIND C/C++ SOURCE
# ============================================================

file(
    GLOB_RECURSE
    PVRTEXLIB_CPP
    "${PVRTEXLIB_ROOT}/*.cpp"
)

file(
    GLOB_RECURSE
    PVRTEXLIB_CC
    "${PVRTEXLIB_ROOT}/*.cc"
)

file(
    GLOB_RECURSE
    PVRTEXLIB_CXX
    "${PVRTEXLIB_ROOT}/*.cxx"
)

file(
    GLOB_RECURSE
    PVRTEXLIB_C
    "${PVRTEXLIB_ROOT}/*.c"
)


set(
    PVRTEXLIB_SOURCES
    ${PVRTEXLIB_CPP}
    ${PVRTEXLIB_CC}
    ${PVRTEXLIB_CXX}
    ${PVRTEXLIB_C}
)


# ============================================================
# REMOVE BUILD DIRECTORIES
# ============================================================

list(
    FILTER
    PVRTEXLIB_SOURCES
    EXCLUDE
    REGEX "/build/"
)

list(
    FILTER
    PVRTEXLIB_SOURCES
    EXCLUDE
    REGEX "/Build/"
)

list(
    FILTER
    PVRTEXLIB_SOURCES
    EXCLUDE
    REGEX "/android/build/"
)

list(
    FILTER
    PVRTEXLIB_SOURCES
    EXCLUDE
    REGEX "/Android/build/"
)


list(
    LENGTH
    PVRTEXLIB_SOURCES
    PVRTEXLIB_SOURCE_COUNT
)

message(
    STATUS
    "PVRTexLib C/C++ source count = ${PVRTEXLIB_SOURCE_COUNT}"
)


# ============================================================
# FIND HEADERS
# ============================================================

file(
    GLOB_RECURSE
    PVRTEXLIB_HEADERS
    "${PVRTEXLIB_ROOT}/*.h"
)

file(
    GLOB_RECURSE
    PVRTEXLIB_HPP
    "${PVRTEXLIB_ROOT}/*.hpp"
)

set(
    PVRTEXLIB_HEADER_FILES
    ${PVRTEXLIB_HEADERS}
    ${PVRTEXLIB_HPP}
)

list(
    LENGTH
    PVRTEXLIB_HEADER_FILES
    PVRTEXLIB_HEADER_COUNT
)

message(
    STATUS
    "PVRTexLib header count = ${PVRTEXLIB_HEADER_COUNT}"
)


# ============================================================
# FIND PREBUILT LIBRARIES
# ============================================================

file(
    GLOB_RECURSE
    PVRTEXLIB_STATIC_LIBS
    "${PVRTEXLIB_ROOT}/*.a"
)

file(
    GLOB_RECURSE
    PVRTEXLIB_SHARED_LIBS
    "${PVRTEXLIB_ROOT}/*.so"
)

file(
    GLOB_RECURSE
    PVRTEXLIB_WINDOWS_LIBS
    "${PVRTEXLIB_ROOT}/*.lib"
)

file(
    GLOB_RECURSE
    PVRTEXLIB_DLLS
    "${PVRTEXLIB_ROOT}/*.dll"
)


list(
    LENGTH
    PVRTEXLIB_STATIC_LIBS
    PVRTEXLIB_STATIC_COUNT
)

list(
    LENGTH
    PVRTEXLIB_SHARED_LIBS
    PVRTEXLIB_SHARED_COUNT
)

list(
    LENGTH
    PVRTEXLIB_WINDOWS_LIBS
    PVRTEXLIB_WINDOWS_COUNT
)

message(
    STATUS
    "PVRTexLib static libraries = ${PVRTEXLIB_STATIC_COUNT}"
)

message(
    STATUS
    "PVRTexLib shared libraries = ${PVRTEXLIB_SHARED_COUNT}"
)

message(
    STATUS
    "PVRTexLib Windows libraries = ${PVRTEXLIB_WINDOWS_COUNT}"
)


# ============================================================
# PRINT SOURCES
# ============================================================

message(STATUS "")
message(STATUS "PVRTexLib source files:")

foreach(
    SRC
    IN LISTS
    PVRTEXLIB_SOURCES
)

    message(
        STATUS
        "  ${SRC}"
    )

endforeach()


# ============================================================
# PRINT LIBRARIES
# ============================================================

message(STATUS "")
message(STATUS "PVRTexLib libraries:")

foreach(
    LIB
    IN LISTS
    PVRTEXLIB_STATIC_LIBS
)

    message(
        STATUS
        "  STATIC: ${LIB}"
    )

endforeach()


foreach(
    LIB
    IN LISTS
    PVRTEXLIB_SHARED_LIBS
)

    message(
        STATUS
        "  SHARED: ${LIB}"
    )

endforeach()


foreach(
    LIB
    IN LISTS
    PVRTEXLIB_WINDOWS_LIBS
)

    message(
        STATUS
        "  WINDOWS: ${LIB}"
    )

endforeach()


# ============================================================
# INCLUDE DIRECTORIES
# ============================================================

set(
    PVRTEXLIB_INCLUDE_DIRS
    "${PVRTEXLIB_ROOT}"
)


if(EXISTS "${PVRTEXLIB_ROOT}/include")

    list(
        APPEND
        PVRTEXLIB_INCLUDE_DIRS
        "${PVRTEXLIB_ROOT}/include"
    )

endif()


if(EXISTS "${PVRTEXLIB_ROOT}/Include")

    list(
        APPEND
        PVRTEXLIB_INCLUDE_DIRS
        "${PVRTEXLIB_ROOT}/Include"
    )

endif()


if(EXISTS "${PVRTEXLIB_ROOT}/src")

    list(
        APPEND
        PVRTEXLIB_INCLUDE_DIRS
        "${PVRTEXLIB_ROOT}/src"
    )

endif()


if(EXISTS "${PVRTEXLIB_ROOT}/Source")

    list(
        APPEND
        PVRTEXLIB_INCLUDE_DIRS
        "${PVRTEXLIB_ROOT}/Source"
    )

endif()


# ============================================================
# BUILD PVRTEXLIB
# ============================================================

if(PVRTEXLIB_SOURCE_COUNT GREATER 0)

    message(STATUS "")
    message(STATUS "=======================================")
    message(STATUS "BUILDING PVRTEXLIB FROM SOURCE")
    message(STATUS "=======================================")

    add_library(
        PVRTexLib
        STATIC
        ${PVRTEXLIB_SOURCES}
    )


    target_include_directories(
        PVRTexLib
        PUBLIC
        ${PVRTEXLIB_INCLUDE_DIRS}
    )


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


elseif(PVRTEXLIB_SHARED_COUNT GREATER 0)

    message(STATUS "")
    message(STATUS "=======================================")
    message(STATUS "USING PREBUILT PVRTEXLIB SHARED LIBRARY")
    message(STATUS "=======================================")

    list(
        GET
        PVRTEXLIB_SHARED_LIBS
        0
        PVRTEXLIB_SO
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
        "${PVRTEXLIB_SO}"
    )


elseif(PVRTEXLIB_STATIC_COUNT GREATER 0)

    message(STATUS "")
    message(STATUS "=======================================")
    message(STATUS "USING PREBUILT PVRTEXLIB STATIC LIBRARY")
    message(STATUS "=======================================")

    list(
        GET
        PVRTEXLIB_STATIC_LIBS
        0
        PVRTEXLIB_A
    )

    add_library(
        PVRTexLib
        STATIC
        IMPORTED
    )

    set_target_properties(
        PVRTexLib
        PROPERTIES
        IMPORTED_LOCATION
        "${PVRTEXLIB_A}"
    )

else()

    message(STATUS "")
    message(STATUS "=======================================")
    message(STATUS "PVRTEXLIB SOURCE NOT FOUND")
    message(STATUS "=======================================")

    message(
        FATAL_ERROR
        "PVRTexLib tidak mempunyai source C/C++ atau library Android yang dapat digunakan."
    )

endif()


# ============================================================
# TEXTOOLWRAP
# ============================================================

message(STATUS "")
message(STATUS "=======================================")
message(STATUS "BUILD TEXTOOLWRAP")
message(STATUS "=======================================")


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

    ${PVRTEXLIB_INCLUDE_DIRS}
)


# ============================================================
# ANDROID DEFINITIONS
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
# COMPILER OPTIONS
# ============================================================

if(
    CMAKE_CXX_COMPILER_ID
    MATCHES
    "Clang"
)

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

    OUTPUT_NAME
    "textoolwrap"
)


# ============================================================
# FINAL
# ============================================================

message(STATUS "")
message(STATUS "=======================================")
message(STATUS "TEXTOOLWRAP CONFIGURATION COMPLETE")
message(STATUS "=======================================")
message(STATUS "Target      : textoolwrap")
message(STATUS "ABI         : ${ANDROID_ABI}")
message(STATUS "PVRTexLib   : detected")
message(STATUS "Output      : libtextoolwrap.so")
message(STATUS "=======================================")
