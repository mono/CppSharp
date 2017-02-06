set -e
BUILD_DIR=$(dirname -- $0)
MONO_PATH=$BUILD_DIR/../deps/NUnit.Runners.2.6.4/tools \
mono $BUILD_DIR/../deps/NUnit.Runners.2.6.4/tools/nunit-console.exe -nologo -noresult $BUILD_DIR/gmake/lib/Release_*/*Tests.*.dll
