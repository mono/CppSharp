#!/bin/sh
set -e
DIR=$( cd "$( dirname "$0" )" && pwd )

case $(uname -s) in 
  CYGWIN*|MINGW32*|MSYS*|MINGW*)
    ACTION=vs2019
    ;;
  *)
    ACTION=gmake
    MONO=mono
    export PATH=$PATH:/Library/Frameworks/Mono.framework/Versions/Current/bin 
    ;;
esac

$DIR/InstallNugets.sh
OUT_DIR=$(find $DIR/$ACTION/lib/* -type d -maxdepth 0)
cp $DIR/../deps/NUnit/nunit.framework.* $OUT_DIR
$MONO $DIR/../deps/NUnit.Console-3.9.0/nunit3-console.exe --result=$OUT_DIR/TestResult.xml $OUT_DIR/*Tests*.dll
