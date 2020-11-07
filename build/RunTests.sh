#!/bin/sh
set -e

BUILD_DIR=$(dirname -- $0)

case "$(uname -s)" in
    Darwin|Linux)
        ACTION=gmake
        MONO=mono
        export PATH=$PATH:/Library/Frameworks/Mono.framework/Versions/Current/bin
        ;;

    CYGWIN*|MINGW32*|MSYS*|MINGW*)
        ACTION=vs2019
        ;;
esac

cp $BUILD_DIR/../deps/NUnit/nunit.framework.* $BUILD_DIR/$ACTION/lib/Release_*/
$MONO $BUILD_DIR/../deps/NUnit.Console-3.9.0/nunit3-console.exe -noresult $BUILD_DIR/$ACTION/lib/Release_*/*Tests*.dll
