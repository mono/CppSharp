using System;
using System.Collections.Generic;
using System.IO;
using CppSharp;
using MoonSharp.Interpreter;

class LuaContext
{
    public Script script;
    public Options options;
    public List<string> errorMessages;

    public string CurrentModule;

    public LuaContext(Options options, List<string> errorMessages)
    {
        this.options = options;
        this.errorMessages = errorMessages;

        script = new Script(CoreModules.Basic | CoreModules.String |
                            CoreModules.Table | CoreModules.TableIterators);

        script.Globals["generator"] = (string kind) =>
        {
            CLI.GetGeneratorKind(kind, errorMessages);
        };

        script.Globals["platform"] = (string platform) =>
        {
            CLI.GetDestinationPlatform(platform, errorMessages);
        };

        script.Globals["architecture"] = (string arch) =>
        {
            CLI.GetDestinationArchitecture(arch, errorMessages);
        };

        script.Globals["output"] = script.Globals["location"] = (string dir) =>
        {
            options.OutputDir = dir;
        };

        script.Globals["includedirs"] = (List<string> dirs) =>
        {
            foreach (var dir in dirs)
            {
                options.IncludeDirs.Add(dir);
            }
        };

        script.Globals["module"] = (string name) =>
        {
            CurrentModule = name;
            options.OutputFileName = name;
        };

        script.Globals["namespace"] = (string name) =>
        {
            options.OutputNamespace = name;
        };

        script.Globals["headers"] = (List<string> files) =>
        {
            foreach (var file in files)
            {
                options.HeaderFiles.Add(file);
            }
        };
    }

    public DynValue LoadFile(string luaFile)
    {
        var code = script.LoadFile(luaFile);

        try
        {
            return code.Function.Call();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error running {Path.GetFileName(luaFile)}:\n{ex.Message}");
            return null;
        }
    }
}