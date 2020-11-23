#!/bin/bash
set -e
DIR=$( cd "$( dirname "$0" )" && pwd )
$DIR/build.sh test "$@"
