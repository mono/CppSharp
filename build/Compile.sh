#!/usr/bin/env bash
set -e

CUR_DIR=$(cd "$(dirname "$0")"; pwd)
DEBUG=false

case $(uname -s) in 
  CYGWIN*|MINGW32*|MSYS*|MINGW*)
  $CUR_DIR/premake5.exe --file=$CUR_DIR/premake5.lua vs$VS_VERSION --arch=$PLATFORM
  MSBuild.exe $CUR_DIR/vs$VS_VERSION/CppSharp.sln //p:Configuration=Release //verbosity:minimal
  exit 0
  ;;
esac

for i in "$@"
do
case $i in
    -debug|--debug)
    DEBUG=true
    ;;
    *)
    # unknown option
    ;;
esac
done

MONO=mono
if [ "$(uname)" == "Darwin" ]; then
  MONO_PATH=/Library/Frameworks/Mono.framework/Versions/Current/bin/
  MONO="$MONO_PATH$MONO"
fi

MONO_VERSION_OUTPUT="$($MONO --version)"
if [[ $MONO_VERSION_OUTPUT == *"amd64"* ]]; then
	BUILD_CONF=release_x64;
else
	BUILD_CONF=release_x86;
fi

export PATH=$PATH:$MONO_PATH

$CUR_DIR/premake.sh --file=$CUR_DIR/premake5.lua gmake2 "$@"

if $DEBUG; then
	BUILD_CONF=debug_x64;
fi
config=$BUILD_CONF make -C $CUR_DIR/gmake/

BUILD_CONF_DIR="$(tr '[:lower:]' '[:upper:]' <<< ${BUILD_CONF:0:1})${BUILD_CONF:1}"
BUILD_DIR=$CUR_DIR/gmake/lib/$BUILD_CONF_DIR
