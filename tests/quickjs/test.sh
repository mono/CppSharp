#!/usr/bin/env bash
set -e
dir=$(cd "$(dirname "$0")"; pwd)
rootdir="$dir/../.."
dotnet_configuration=Release
configuration=debug
platform=x64
jsinterp="$dir/runtime/build/qjs"

cd $dir

if [ "$CI" = "true" ]; then
    red=""
    green=""
    reset=""
else
    red=`tput setaf 1`
    green=`tput setaf 2`
    reset=`tput sgr0`
fi

generate=true

if [ $generate = true ]; then
    echo "${green}Generating bindings${reset}"
    dotnet $rootdir/bin/${dotnet_configuration}_${platform}/CppSharp.CLI.dll \
    $dir/bindings.lua
fi

echo "${green}Building generated binding files${reset}"
premake=$rootdir/build/premake.sh
config=$configuration $premake --cc=clang --file=$dir/premake5.lua gmake2
make -C $dir/gen
echo

echo "${green}Executing JS tests with QuickJS${reset}"
cp $dir/gen/bin/$configuration/libtest.so $dir
#cp $dir/gen/bin/$configuration/libtest.dylib $dir
$jsinterp --std $dir/test.js