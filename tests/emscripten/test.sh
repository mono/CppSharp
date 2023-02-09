#!/usr/bin/env bash
set -e
dir=$(cd "$(dirname "$0")"; pwd)
rootdir="$dir/../.."
dotnet_configuration=Release
configuration=debug
platform=x64
jsinterp=node

red=`tput setaf 1`
green=`tput setaf 2`
reset=`tput sgr0`

generate=true

if [ $generate = true ]; then
    echo "${green}Generating bindings${reset}"
    dotnet $rootdir/bin/${dotnet_configuration}_${platform}/CppSharp.CLI.dll \
        --gen=emscripten --platform=emscripten --arch=wasm32 \
        -I$dir/.. -I$rootdir/include -o $dir/gen -m tests $dir/../*.h
fi

echo "${green}Building generated binding files${reset}"
premake=$rootdir/build/premake.sh
config=$configuration $premake --file=$dir/premake5.lua gmake
emmake make -C $dir/gen
echo

echo "${green}Executing JS tests with Node${reset}"
$jsinterp $dir/test.mjs