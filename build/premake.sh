#!/bin/sh

DIR=$( cd "$( dirname "$0" )" && pwd )

case "$(uname -s)" in

   Darwin)
     $DIR/premake5-osx $*
     ;;

   Linux)
     $DIR/premake5-linux $*
     ;;

   CYGWIN*|MINGW32*|MSYS*|MINGW*)
     $DIR/premake5.exe $*
     ;;

   *)
    echo "Unsupported platform"
    exit 1
     ;;
esac
