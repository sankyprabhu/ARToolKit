#! /bin/bash

#
# Build ARToolKit Camera Calibration utility for desktop platforms.
#
# Copyright 2016-2017, DAQRI LLC and ARToolKit Contributors.
# Author(s): Philip Lamb, Thorsten Bux, John Wolf, Dan Bell.
#

# Get our location.
OURDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

SDK_VERSION='6.0.2'
SDK_URL_DIR='http://artoolkit-dist.s3.amazonaws.com/artoolkit6/6.0/'

VERSION=`sed -En -e 's/.*VERSION_STRING[[:space:]]+"([0-9]+\.[0-9]+(\.[0-9]+)*)".*/\1/p' ${OURDIR}/version.h`
# If the tiny version number is 0, drop it.
VERSION=`echo -n "${VERSION}" | sed -E -e 's/([0-9]+\.[0-9]+)\.0/\1/'`

function usage {
    echo "Usage: $(basename $0) (macos | linux)... "
    exit 1
}

if [ $# -eq 0 ]; then
    usage
fi

# -e = exit on errors; -x = debug
set -e -x

# Parse parameters
while test $# -gt 0
do
    case "$1" in
        osx) BUILD_MACOS=1
            ;;
        macos) BUILD_MACOS=1
            ;;
        ios) BUILD_IOS=1
            ;;
        linux) BUILD_LINUX=1
            ;;
        --*) echo "bad option $1"
            usage
            ;;
        *) echo "bad argument $1"
            usage
            ;;
    esac
    shift
done


# Set OS-dependent variables.
OS=`uname -s`
ARCH=`uname -m`
TAR='/usr/bin/tar'
if [ "$OS" = "Linux" ]
then
    CPUS=`/usr/bin/nproc`
    TAR='/bin/tar'
elif [ "$OS" = "Darwin" ]
then
    CPUS=`/usr/sbin/sysctl -n hw.ncpu`
elif [ "$OS" = "CYGWIN_NT-6.1" ]
then
    CPUS=`/usr/bin/nproc`
else
    CPUS=1
fi

# Function to allow check for required packages.
function check_package {
	# Variant for distros that use debian packaging.
	if (type dpkg-query >/dev/null 2>&1) ; then
		if ! $(dpkg-query -W -f='${Status}' $1 | grep -q '^install ok installed$') ; then
			echo "Warning: required package '$1' does not appear to be installed. To install it use 'sudo apt-get install $1'."
		fi
	# Variant for distros that use rpm packaging.
	elif (type rpm >/dev/null 2>&1) ; then
		if ! $(rpm -qa | grep -q $1) ; then
			echo "Warning: required package '$1' does not appear to be installed. To install it use 'sudo dnf install $1'."
		fi
	fi
}

function rawurlencode() {
    local string="${1}"
    local strlen=${#string}
    local encoded=""
    local pos c o

    for (( pos=0 ; pos<strlen ; pos++ )); do
        c=${string:$pos:1}
        case "$c" in
            [-_.~a-zA-Z0-9] ) o="${c}" ;;
            * )               printf -v o '%%%02x' "'$c"
        esac
        encoded+="${o}"
    done
    echo -n "${encoded}"
}

if [ "$OS" = "Darwin" ] ; then
# ======================================================================
#  Build platforms hosted by macOS
# ======================================================================

# macOS
if [ $BUILD_MACOS ] ; then
    
    # Fetch the AR6.framework from latest build into a location where Xcode will find it.
    SDK_FILENAME="ARToolKit for macOS v${SDK_VERSION}.dmg"
    curl -f -o "${SDK_FILENAME}" "${SDK_URL_DIR}$(rawurlencode "${SDK_FILENAME}")"
    hdiutil attach "${SDK_FILENAME}" -noautoopen -quiet -mountpoint "SDK"
    rm -rf depends/macOS/Frameworks/AR6.framework
    cp -af SDK/artoolkit6/SDK/Frameworks/AR6.framework depends/macOS/Frameworks
    hdiutil detach "SDK" -quiet -force
    
    # Make the version number available to Xcode.
    sed -E -i.bak "s/@VERSION@/${VERSION}/" macOS/user-config.xcconfig
    
    (cd macOS
    xcodebuild -target "ARToolKit6 Camera Calibration Utility" -configuration Release
    )
fi
# /BUILD_MACOS

# iOS
if [ $BUILD_IOS ] ; then
    
    # Fetch libAR6 from latest build into a location where Xcode will find it.
    SDK_FILENAME="ARToolKit for iOS v${SDK_VERSION}.dmg"
    curl -f -o "${SDK_FILENAME}" "${SDK_URL_DIR}$(rawurlencode "${SDK_FILENAME}")"
    hdiutil attach "${SDK_FILENAME}" -noautoopen -quiet -mountpoint "SDK"
    rm -rf depends/iOS/include/AR6/
    cp -af SDK/artoolkit6/SDK/include/AR6 depends/iOS/include
    rm -f depends/iOS/lib/libAR6.a
    cp -af SDK/artoolkit6/SDK/lib/libAR6.a depends/iOS/lib
    hdiutil detach "SDK" -quiet -force
    
    # Make the version number available to Xcode.
    sed -E -i.bak "s/@VERSION@/${VERSION}/" iOS/user-config.xcconfig
    
    (cd iOS
    xcodebuild -target "ARToolKit6 Camera Calibration Utility" -configuration Release -destination generic/platform=iOS
    )
fi
# /BUILD_MACOS

fi
# /Darwin

if [ "$OS" = "Linux" ] ; then
# ======================================================================
#  Build platforms hosted by Linux
# ======================================================================

# Linux
if [ $BUILD_LINUX ] ; then
    
    #Before we can install the artoolkit6-dev package we need to install the -lib. As -dev depends on -lib
    SDK_FILENAME="artoolkit6-lib_${SDK_VERSION}_amd64.deb"
    curl -f -o "${SDK_FILENAME}" "${SDK_URL_DIR}$(rawurlencode "${SDK_FILENAME}")"
    sudo dpkg -i "${SDK_FILENAME}"

    # Fetch the artoolkit6-dev package and install it.
    SDK_FILENAME="artoolkit6-dev_${SDK_VERSION}_amd64.deb"
    curl -f -o "${SDK_FILENAME}" "${SDK_URL_DIR}$(rawurlencode "${SDK_FILENAME}")"
    sudo dpkg -i "${SDK_FILENAME}"

    (cd Linux
	mkdir -p build
	cd build
	cmake .. -DCMAKE_BUILD_TYPE=Release "-DVERSION=${VERSION}"
    make
	make install
    )

fi
# /BUILD_LINUX

fi
# /Linux

