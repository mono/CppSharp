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
        --gen=ts -I$dir/.. -I$rootdir/include -o $dir/gen -m tests $dir/../*.h
fi

echo "${green}Building generated binding files${reset}"
#make -C $dir/gen
echo

echo "${green}Typechecking generated binding files with tsc${reset}"
#tsc --noEmit --strict --noImplicitAny --strictNullChecks --strictFunctionTypes --noImplicitThis gen/*.d.ts

# echo "${green}Executing JS tests with QuickJS${reset}"
# cp $dir/gen/bin/$configuration/libtest.so $dir
# #cp $dir/gen/bin/$configuration/libtest.dylib $dir
# $jsinterp --std $dir/test.js