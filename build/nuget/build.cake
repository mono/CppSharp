///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
   // Executed BEFORE the first task.
   Information("Running tasks...");
});

Teardown(ctx =>
{
   // Executed AFTER the last task.
   Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Build")
.Does(() => {

    if (!IsRunningOnWindows())
        throw new NotImplementedException("TODO Implement other platforms");

    // TODO update this when there's a 64-bit solution

    var settings = new MSBuildSettings();
    settings.SetConfiguration("Release");
    settings.SetPlatformTarget(PlatformTarget.x86);
    MSBuild("../vs2017/CppSharp.sln", settings);

});

Task("Package")
.Does(() => {

    var scratchDir = new DirectoryPath("temp");

    // clear previous session
    if (DirectoryExists(scratchDir))
        DeleteDirectory(scratchDir, true);

    var files = new Dictionary<string,string>()
    {
        { "CppSharp.dll", "lib" },
        { "CppSharp.AST.dll", "lib" },
        { "CppSharp.Generator.dll", "lib" },
        { "CppSharp.Parser.dll", "lib" },
        { "CppSharp.Parser.CLI.dll", "lib" },
        { "CppSharp.Runtime.dll", "lib" },

        { "CppSharp.CLI.exe", "tools"},

        { "CppSharp.CppParser.dll", "output"},
        { "clang", "output/lib/clang"},
    };

    var path = new DirectoryPath("../vs2017/lib/Release_x86/");
   
    CreateDirectory(scratchDir);
    
    foreach (var file in files)
    {
        var tgt = scratchDir.Combine(file.Value);
        CreateDirectory(tgt);

        var isDirPath = path.Combine(file.Key);
        var isDir = DirectoryExists(isDirPath);
        if (isDir)
        {
            var src = path.Combine(file.Key);
            CopyDirectory(src, tgt);
        }
        else
        {
            var src = path.CombineWithFilePath(file.Key);
            CopyFileToDirectory(src, tgt);
        }
    }

    var nuspec = "cppsharp.nuspec";

    var settings = new NuGetPackSettings()
    {
        OutputDirectory = ".",
        BasePath = scratchDir,
        NoPackageAnalysis = false,
    };

    NuGetPack(nuspec, settings);

    // clear current session
    DeleteDirectory(scratchDir, true);
});

Task("CppSharpBuildAndPackage")
.IsDependentOn("Build")
.IsDependentOn("Package")
.Does(() => {

});

Task("CppSharpPackageOnly")
.IsDependentOn("Package")
.Does(() => {

});

Task("Default")
.IsDependentOn("CppSharpPackageOnly")
.Does(() => {

});


RunTarget(target);