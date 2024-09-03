#!/usr/bin/env bash
set -e
dir=$(cd "$(dirname "$0")"; pwd)
rootdir="$dir/../.."

cd $dir

if [ ! -d runtime ]; then
    git clone https://github.com/quickjs-ng/quickjs.git runtime
    git -C runtime reset --hard 0e5e9c2c49db15ab9579edeb4d90e610c8b8463f
fi

if [ ! -f runtime/build/qjs ]; then
    make -C runtime/
fi
