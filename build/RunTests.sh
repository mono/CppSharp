#!/bin/sh
set -e


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

DIR=$( cd "$( dirname "$0" )" && pwd )
OUT_DIR=$(find $DIR/$ACTION/lib/* -type d -maxdepth 0)
cp $DIR/../deps/NUnit/nunit.framework.* $OUT_DIR
$MONO $DIR/../deps/NUnit.Console-3.9.0/nunit3-console.exe --result=$OUT_DIR/TestResult.xml $OUT_DIR/*Tests*.dll
