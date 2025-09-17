cmake_minimum_required(VERSION 3.16)
project(xdp_shaper C)

# ---- Options you can tweak in CLion (Settings > CMake profiles > CMake options) ----
#   -DBPF_CLANG=/usr/bin/clang
#   -DBPF_ARCH=x86         (x86, arm64, arm, s390, powerpc)
#   -DENABLE_ASAN=ON
# ------------------------------------------------------------------------------------

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

if(NOT CMAKE_SYSTEM_NAME STREQUAL "Linux")
    message(FATAL_ERROR "This project must be built on Linux.")
endif()

# Prefer Release by default
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "" FORCE)
endif()

# ---- Dependencies (libbpf, libelf, zlib) ----
find_package(PkgConfig REQUIRED)
pkg_check_modules(LIBBPF QUIET libbpf)

if(LIBBPF_FOUND)
    message(STATUS "Found libbpf via pkg-config")
    set(LIBBPF_INCLUDE_DIRS ${LIBBPF_INCLUDE_DIRS})
    set(LIBBPF_LIBRARIES ${LIBBPF_LIBRARIES})
else()
    message(STATUS "pkg-config couldn't find libbpf; falling back to -lbpf -lelf -lz")
    set(LIBBPF_INCLUDE_DIRS "/usr/include" "/usr/include/bpf")
    set(LIBBPF_LIBRARIES bpf elf z)
endif()

include_directories(${LIBBPF_INCLUDE_DIRS})

# ---- User-space executable ----
add_executable(xdp_shaper xdp_shaper.c)

target_compile_options(xdp_shaper PRIVATE -O2 -g -Wall -Wextra)

# Optional AddressSanitizer (toggle with -DENABLE_ASAN=ON)
option(ENABLE_ASAN "Enable AddressSanitizer for user-space binary" OFF)
if(ENABLE_ASAN)
    target_compile_options(xdp_shaper PRIVATE -fsanitize=address -fno-omit-frame-pointer)
    target_link_options(xdp_shaper PRIVATE -fsanitize=address)
endif()

# Link order matters on some systems; keep libs after the target
target_link_libraries(xdp_shaper PRIVATE ${LIBBPF_LIBRARIES})

# Nice-to-have install rules
include(GNUInstallDirs)
install(TARGETS xdp_shaper RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR})
install(FILES ${BPF_OBJ} DESTINATION ${CMAKE_INSTALL_BINDIR})
