#!/usr/bin/env bash
set -e

dir=$(cd "$(dirname "$0")"; pwd)
rootdir="$dir/../.."
dotnet_configuration=DebugOpt
make_configuration=debug
platform=x64
jsinterp="$dir/runtime/build/qjs"

usage() {
  cat <<EOF
Usage: $(basename $0) [--with-node=NODE] [--make-config CONFIG] [--dotnet-config CONFIG]
EOF
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --with-node=*)
      jsinterp="${1#*=}"
      shift
      ;;
    --with-node)
      jsinterp="$2"
      shift 2
      ;;
    --make-config|--make-configuration)
      make_configuration="$2"
      shift 2
      ;;
    --dotnet-config|--dotnet-configuration)
      dotnet_configuration="$2"
      shift 2
      ;;
    -h|--help)
      usage
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage
      ;;
  esac
done

if [ "$CI" = "true" ]; then
    red=""
    green=""
    reset=""
else
    red=`tput setaf 1`
    green=`tput setaf 2`
    reset=`tput sgr0`
fi

# 1) Generate
generate=true
if [ $generate = true ]; then
    echo "${green}Generating bindings with .NET configuration $dotnet_configuration${reset}"
    dotnet "$rootdir/bin/${dotnet_configuration}/CppSharp.CLI.dll" \
        --gen=ts -I$dir/.. -I$rootdir/include -o $dir/gen -m tests $dir/../*.h
fi

# 2) Type-checking
echo "${green}Typechecking generated binding files with tsc${reset}"
#tsc --noEmit --strict --noImplicitAny --strictNullChecks --strictFunctionTypes --noImplicitThis gen/*.d.ts
