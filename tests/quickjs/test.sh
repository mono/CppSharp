#!/usr/bin/env bash
set -e
dir=$(cd "$(dirname "$0")"; pwd)
rootdir="$dir/../.."
dotnet_configuration=Release
configuration=debug
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
    dotnet $rootdir/bin/${dotnet_configuration}/CppSharp.CLI.dll \
    $dir/bindings.lua
fi

echo "${green}Building generated binding files${reset}"
premake=$rootdir/build/premake.sh
config=$configuration $premake --file=$dir/premake5.lua gmake2
verbose=true make -C $dir/gen
echo

echo "${green}Executing JS tests with QuickJS${reset}"
cp $dir/gen/bin/$configuration/libtest.so $dir
$jsinterp --std $dir/test.js