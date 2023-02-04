#!/usr/bin/env bash
set -e
dir=$(cd "$(dirname "$0")"; pwd)
rootdir="$dir/../.."
dotnet_configuration=Release
configuration=debug
platform=x64
jsinterp="$rootdir/deps/quickjs/qjs-debug"

red=`tput setaf 1`
green=`tput setaf 2`
reset=`tput sgr0`

generate=true

if [ $generate = true ]; then
    echo "${green}Generating bindings${reset}"
    dotnet $rootdir/bin/${dotnet_configuration}_${platform}/CppSharp.CLI.dll \
        --gen=qjs -I$dir/.. -I$rootdir/include -o $dir/gen -m tests $dir/../*.h
fi

echo "${green}Building generated binding files${reset}"
premake=$rootdir/build/premake.sh
config=$configuration $premake --file=$dir/premake5.lua gmake
make -C $dir/gen
echo

echo "${green}Executing JS tests with QuickJS${reset}"
cp $dir/gen/bin/$configuration/libtest.so $dir
#cp $dir/gen/bin/$configuration/libtest.dylib $dir
$jsinterp --std $dir/test.js