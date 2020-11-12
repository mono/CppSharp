BUILD_DIR=$(dirname -- $0)
NUNIT=NUnit.Console-3.9.0

if [ -e $BUILD_DIR/../deps/$NUNIT/nunit3-console.exe ]; then
  exit 0
fi

curl -O -L https://github.com/nunit/nunit-console/releases/download/v3.9/$NUNIT.zip
unzip $NUNIT.zip -d $BUILD_DIR/../deps/$NUNIT