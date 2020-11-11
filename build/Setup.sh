#!/bin/sh
set -e
DIR=$( cd "$( dirname "$0" )" && pwd )

case $(uname -s) in 
  CYGWIN*|MINGW32*|MSYS*|MINGW*)
    ;;
  *)
    $DIR/InstallMono.sh
    ;;
esac

$DIR/premake.sh --file=$DIR/scripts/LLVM.lua download_llvm --arch=$PLATFORM
