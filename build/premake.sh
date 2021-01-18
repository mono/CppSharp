#!/bin/sh

DIR=$( cd "$( dirname "$0" )" && pwd )

case "$(uname -s)" in

   Darwin|Linux)
     "$DIR/premake/premake5" "$@"
     ;;

   CYGWIN*|MINGW32*|MSYS*|MINGW*)
     "$DIR/premake/premake5.exe" "$@"
     ;;

   *)
    echo "Unsupported platform"
    exit 1
     ;;
esac
