#!/usr/bin/env bash
set -e
dir=$(cd "$(dirname "$0")"; pwd)

$dir/napi/test.sh
$dir/quickjs/test.sh
$dir/emscripten/test.sh
