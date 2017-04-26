set -e
BUILD_DIR=$(dirname -- $0)
MONO_PATH=$BUILD_DIR/../deps/NUnit.ConsoleRunner.3.6.1/tools \
cp $BUILD_DIR/../deps/NUnit/nunit.framework.* $BUILD_DIR/gmake/lib/Release_*/
mono $BUILD_DIR/../deps/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe -noresult $BUILD_DIR/gmake/lib/Release_*/*Tests*.dll
