wget https://nuget.org/nuget.exe
BUILD_DIR=$(dirname -- $0)
mono nuget.exe install NUnit.ConsoleRunner -Version 3.6.0 -OutputDirectory $BUILD_DIR/../deps