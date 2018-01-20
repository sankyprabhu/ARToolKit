#
# ARToolKit v6.x Camera Calibration (Desktop) Utility
#
# To invoke CMake on this file, from the chosen build directory, invoke:
#     cmake <path to directory containing this file>.
#
# Author(s): Philip Lamb <phil@artoolkit.org>
#

#
# Packages required: libjpeg-dev libopencv-calib3d-dev libssl-dev libcurl4-openssl-dev
#

cmake_minimum_required( VERSION 3.2 )

# Any directives that must go before the project command.

set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "Specifies what build types (configurations) will be available." FORCE)
if(DEFINED CMAKE_BUILD_TYPE)
  set_property( CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS ${CMAKE_CONFIGURATION_TYPES} )
endif()

project("artoolkit6_calib_camera"
        VERSION 6.0.0
        LANGUAGES CXX C
)

set(ARTOOLKIT6_HOME "${CMAKE_CURRENT_SOURCE_DIR}/../../artoolkit6")
set(ARTOOLKIT6_CAMERA_CALIBRATION_HOME "${CMAKE_CURRENT_SOURCE_DIR}/..")

list(APPEND CMAKE_FIND_ROOT_PATH "${ARTOOLKIT6_HOME}/Source/depends/linux")
list(APPEND CMAKE_FIND_ROOT_PATH "${ARTOOLKIT6_CAMERA_CALIBRATION_HOME}/depends/linux")

add_definitions("-DTARGET_PLATFORM_LINUX=1")

# Installation settings.
# If the user didn't define CMAKE_INSTALL_PREFIX, set a local install destination.
# When building packages, this will typically be overridden with DESTDIR (e.g. "make install DESTDIR=/tmp").
if (CMAKE_INSTALL_PREFIX_INITIALIZED_TO_DEFAULT)
    set (CMAKE_INSTALL_PREFIX "${ARTOOLKIT6_CAMERA_CALIBRATION_HOME}" CACHE PATH "default install path" FORCE )
endif()

# Enable C++11 for GCC/Clang compilers.
set(CMAKE_CXX_STANDARD "11")
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

include_directories(${ARTOOLKIT6_CAMERA_CALIBRATION_HOME})

find_package(OpenGL REQUIRED)
include_directories(${OPENGL_INCLUDE_DIR})

find_package(SDL2 REQUIRED)
include(${SDL2_DIR}/sdl2-config.cmake)
string(STRIP ${SDL2_LIBRARIES} SDL2_LIBRARIES)
include_directories(${SDL2_INCLUDE_DIRS})

find_package(JPEG REQUIRED)
include_directories(${JPEG_INCLUDE_DIR})

find_path(
    OPENCV_INCLUDE_DIR
    NAMES calib3d.hpp
    PATH_SUFFIXES opencv2/calib3d
)
include_directories(${OPENCV_INCLUDE_DIR}/../..)
find_library(OPENCV_CALIB3D_LIBRARY NAMES opencv_calib3d)
find_library(OPENCV_FEATURES2D_LIBRARY NAMES opencv_features2d)
find_library(OPENCV_IMGPROC_LIBRARY NAMES opencv_imgproc)
find_library(OPENCV_FLANN_LIBRARY NAMES opencv_flann)
find_library(OPENCV_CORE_LIBRARY NAMES opencv_core)

find_package(CURL REQUIRED)
include_directories(${CURL_INCLUDE_DIRS})

find_package(OpenSSL REQUIRED)
include_directories(${CURL_INCLUDE_DIRS})

find_package(PkgConfig)
pkg_check_modules(LIBCONFIG REQUIRED libconfig)
include_directories(${LIBCONFIG_INCLUDE_DIRS})

include_directories(${ARTOOLKIT6_HOME}/Source/depends/linux/include)

include(${ARTOOLKIT6_HOME}/SDK/lib/AR6/AR6.cmake)

set(SOURCE
    ../calib_camera.cpp
    ../calib_camera.h
    ../Calibration.hpp
    ../Calibration.cpp
    ../calc.cpp
    ../calc.hpp
    ../fileUploader.c
    ../fileUploader.h
    ../flow.cpp
    ../flow.hpp
    ../prefs.hpp
    ../prefsLibConfig.cpp
    ../prefsNull.cpp
    ../Eden/Eden.h
    ../Eden/EdenError.h
    ../Eden/EdenGLFont.c
    ../Eden/EdenGLFont.h
    ../Eden/EdenMath.h
    ../Eden/EdenMessage.c
    ../Eden/EdenMessage.h
    ../Eden/EdenSurfaces.c
    ../Eden/EdenSurfaces.h
    ../Eden/EdenTime.c
    ../Eden/EdenTime.h
    ../Eden/EdenUtil.c
    ../Eden/EdenUtil.h
    ../Eden/glStateCache.c
    ../Eden/glStateCache.h
    ../Eden/gluttext.h
    ../Eden/readtex.c
    ../Eden/readtex.h
    ../Eden/gluttext/glut_8x13.c
    ../Eden/gluttext/glut_9x15.c
    ../Eden/gluttext/glut_bitmap.c
    ../Eden/gluttext/glut_bwidth.c
    ../Eden/gluttext/glut_hel10.c
    ../Eden/gluttext/glut_hel12.c
    ../Eden/gluttext/glut_hel18.c
    ../Eden/gluttext/glut_mroman.c
    ../Eden/gluttext/glut_roman.c
    ../Eden/gluttext/glut_stroke.c
    ../Eden/gluttext/glut_swidth.c
    ../Eden/gluttext/glut_tr10.c
    ../Eden/gluttext/glut_tr24.c
    ../Eden/gluttext/glutbitmap.h
    ../Eden/gluttext/glutstroke.h
)

add_executable(artoolkit6_calib_camera ${SOURCE})

add_dependencies(artoolkit6_calib_camera
    AR6
)

#set_target_properties(artoolkit6_calib_camera PROPERTIES
#    INSTALL_RPATH "\$ORIGIN/../lib"
#)

target_link_libraries(artoolkit6_calib_camera
    AR6
    ${OPENGL_LIBRARIES}
    ${SDL2_LIBRARIES}
    ${JPEG_LIBRARIES}
    ${OPENCV_CALIB3D_LIBRARY} ${OPENCV_FEATURES2D_LIBRARY} ${OPENCV_IMGPROC_LIBRARY} ${OPENCV_FLANN_LIBRARY} ${OPENCV_CORE_LIBRARY}
    ${CURL_LIBRARIES} ${OPENSSL_LIBRARIES}
    ${LIBCONFIG_LIBRARIES}
    pthread
    m
)

get_directory_property(AR6CC_DEFINES DIRECTORY ${CMAKE_SOURCE_DIR} COMPILE_DEFINITIONS)
foreach(d ${AR6CC_DEFINES})
    message(STATUS "Defined: " ${d})
endforeach()

install(TARGETS artoolkit6_calib_camera
    RUNTIME DESTINATION .
)

