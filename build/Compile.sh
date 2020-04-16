#!/usr/bin/env bash
set -e

CUR_DIR=$(cd "$(dirname "$0")"; pwd)
DEBUG=false

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

if [ "$(uname)" == "Darwin" ]; then
	PREMAKE=$CUR_DIR/premake5-osx;
elif [ "$(expr substr $(uname -s) 1 5)" == "Linux" ]; then
	PREMAKE=$CUR_DIR/premake5-linux-64;
fi

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

$PREMAKE --file=$CUR_DIR/premake5.lua gmake "$@"

if $DEBUG; then
	BUILD_CONF=debug_x64;
fi
config=$BUILD_CONF make -C $CUR_DIR/gmake/

BUILD_CONF_DIR="$(tr '[:lower:]' '[:upper:]' <<< ${BUILD_CONF:0:1})${BUILD_CONF:1}"
BUILD_DIR=$CUR_DIR/gmake/lib/$BUILD_CONF_DIR
