wget https://nuget.org/nuget.exe
mono nuget.exe install NUnit -Version 2.6.4 -OutputDirectory ../deps
mono nuget.exe install NUnit.Runners -Version 2.6.4 -OutputDirectory ../deps
cp deps/NUnit.2.6.4/lib/nunit.framework.* deps/NUnit/ 
