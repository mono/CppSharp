curl -O -L https://github.com/nunit/nunit-console/releases/download/v3.9/NUnit.Console-3.9.0.zip
BUILD_DIR=$(dirname -- $0)
unzip NUnit.Console-3.9.0.zip -d $BUILD_DIR/../deps/NUnit.Console-3.9.0