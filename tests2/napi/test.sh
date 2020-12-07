#!/usr/bin/env bash
set -e
dir=$(cd "$(dirname "$0")"; pwd)
rootdir="$dir/../.."
configuration=Release
platform=x64

red=`tput setaf 1`
green=`tput setaf 2`
reset=`tput sgr0`

echo "${green}Generating bindings${reset}"
dotnet $rootdir/bin/${configuration}_${platform}/CppSharp.CLI.dll \
 --gen=napi -I$dir/.. -o $dir/gen -m tests $dir/../*.h

echo "${green}Building generated binding files${reset}"
premake=$rootdir/build/premake.sh
$premake --file=$dir/premake5.lua gmake
make -C $dir/gen
echo

echo "${green}Executing JS tests with Node${reset}"
node $dir/test.js
