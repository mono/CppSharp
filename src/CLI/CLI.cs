using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CppSharp
{
    class CLI
    {
        private static Options _options = new Options();

        static void AddIncludeDirs(String dir)
        {
            _options.IncludeDirs.Add(dir);
        }

        static bool ParseCommandLineArgs(string[] args)
        {
            var showHelp = false;

            var optionSet = new Mono.Options.OptionSet()
            {
                { "I=", "the {PATH} of a folder to search for include files", i => { AddIncludeDirs(i); } },
                { "l=", "{LIBRARY} that that contains the symbols of the generated code", l => _options.Libraries.Add(l) },
                { "L=", "the {PATH} of a folder to search for additional libraries", l => _options.LibraryDirs.Add(l) },
                { "D:", "additional define with (optional) value to add to be used while parsing the given header files", (n, v) => AddDefine(n, v) },

                { "o=|output=", "the {PATH} for the generated bindings file (doesn't need the extension since it will depend on the generator)", v => HandleOutputArg(v) },
                { "on=|outputnamespace=", "the {NAMESPACE} that will be used for the generated code", on => _options.OutputNamespace = on },

                { "iln=|inputlibraryname=", "the {NAME} of the shared library that contains the symbols of the generated code", iln => _options.InputLibraryName = iln },

                { "g=|gen=|generator=", "the {TYPE} of generated code: 'chsarp' or 'cli' ('cli' supported only for Windows)", g => { GetGeneratorKind(g); } },
                { "p=|platform=", "the {PLATFORM} that the generated code will target: 'win', 'osx' or 'linux'", p => { GetDestinationPlatform(p); } },
                { "a=|arch=", "the {ARCHITECTURE} that the generated code will target: 'x86' or 'x64'", a => { GetDestinationArchitecture(a); } },

                { "c++11", "enables GCC C++ 11 compilation (valid only for Linux platform)", cpp11 => { _options.Cpp11ABI = (cpp11 != null); } },
                { "cs|checksymbols", "enable the symbol check for the generated code", cs => { _options.CheckSymbols = (cs != null); } },
                { "ub|unitybuild", "enable unity build", ub => { _options.UnityBuild = (ub != null); } },

                { "h|help", "shows the help", hl => { showHelp = (hl != null); } },
            };

            List<String> additionalArguments = null;

            try
            {
                additionalArguments = optionSet.Parse(args);
            }
            catch (Mono.Options.OptionException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
                        
            if (showHelp || additionalArguments != null && additionalArguments.Count == 0)
            {
                ShowHelp(optionSet);
                return false;
            }

            foreach(String s in additionalArguments)
                HandleAdditionalArgument(s);

            return true;
        }

        static void ShowHelp(Mono.Options.OptionSet options)
        {
            Console.WriteLine("Usage: {0} [options]+", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Generates target language bindings to interop with unmanaged code.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Useful informations:");
            Console.WriteLine(" - the options 'iln' (same as 'inputlibraryname') and 'l' have a similar meaning. Both of them are used to tell");
            Console.WriteLine("   the generator which library has to be used to P/Invoke the functions from your native code.");
            Console.WriteLine("   The difference is that if you want to generate the bindings for more than one library within a single managed");
            Console.WriteLine("   file you need to use the 'l' option to specify the names of all the libraries that contain the symbols to be loaded");
            Console.WriteLine("   and you MUST set the 'cs' ('checksymbols') flag to let the generator automatically understand which library");
            Console.WriteLine("   to use to P/Invoke. This can be also used if you plan to generate the bindings for only one library.");
            Console.WriteLine("   If you specify the 'iln' (or 'inputlibraryname') options, this option's value will be used for all the P/Invokes");
            Console.WriteLine("   that the generator will create.");
            Console.WriteLine(" - If you specify the 'unitybuild' option then the generator will output a file for each given header file that will");
            Console.WriteLine("   contain only the bindings for that header file.");
        }

        static void HandleOutputArg(String arg)
        {
            try
            {
                String dir = System.IO.Path.GetDirectoryName(arg);
                String file = System.IO.Path.GetFileNameWithoutExtension(arg);

                _options.OutputDir = dir;
                _options.OutputFileName = file;
            }
            catch(Exception e)
            {
                Console.WriteLine("Output error: " + e.Message);
                Environment.Exit(0);
            }
        }

        static void AddDefine(String name, String value)
        {
            if (name == null)
                throw new Mono.Options.OptionException("Invalid definition name for option -D.", "-D");
            _options.Defines.Add(name, value);
        }

        static void HandleAdditionalArgument(String args)
        {
            if (System.IO.Directory.Exists(args))
                GetFilesFromPath(args);
            else if (System.IO.File.Exists(args))
                _options.HeaderFiles.Add(args);
        }

        static void GetFilesFromPath(String path)
        {
            path = path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

            String searchPattern = String.Empty;
            int lastSeparatorPosition = path.LastIndexOf(System.IO.Path.AltDirectorySeparatorChar);

            if (lastSeparatorPosition >= 0)
            {
                if (path.IndexOf('*', lastSeparatorPosition) >= lastSeparatorPosition || path.IndexOf('?', lastSeparatorPosition) >= lastSeparatorPosition)
                {
                    searchPattern = path.Substring(lastSeparatorPosition + 1);
                    path = path.Substring(0, lastSeparatorPosition);
                }
            }

            try
            {
                if (searchPattern != String.Empty)
                {
                    String[] files = System.IO.Directory.GetFiles(path, searchPattern);

                    foreach (String s in files)
                        _options.HeaderFiles.Add(s);
                }
                else
                {
                    String[] files = System.IO.Directory.GetFiles(path);

                    foreach (String s in files)
                        _options.HeaderFiles.Add(s);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Source files path error: " + ex.Message);
                Environment.Exit(0);
            }
        }

        static void GetGeneratorKind(String generator)
        {
            switch (generator.ToLower())
            {
                case "csharp":
                    _options.Kind = CppSharp.Generators.GeneratorKind.CSharp;
                    return;
                case "cli":
                    _options.Kind = CppSharp.Generators.GeneratorKind.CLI;
                    return;
            }

            throw new NotSupportedException("Unknown generator kind: " + generator);
        }

        static void GetDestinationPlatform(String platform)
        {
            switch (platform.ToLower())
            {
                case "win":
                    _options.Platform = TargetPlatform.Windows;
                    return;
                case "osx":
                    _options.Platform = TargetPlatform.MacOS;
                    return;
                case "linux":
                    _options.Platform = TargetPlatform.Linux;
                    return;
            }

            throw new NotSupportedException("Unknown target platform: " + platform);
        }

        static void GetDestinationArchitecture(String architecture)
        {
            switch (architecture.ToLower())
            {
                case "x86":
                    _options.Architecture = TargetArchitecture.x86;
                    return;
                case "x64":
                    _options.Architecture = TargetArchitecture.x64;
                    return;
            }

            throw new NotSupportedException("Unknown target architecture: " + architecture);
        }

        static void Main(string[] args)
        {
            try
            {
                if (ParseCommandLineArgs(args) == false)
                    return;

                Generator gen = new Generator(_options);
                gen.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}