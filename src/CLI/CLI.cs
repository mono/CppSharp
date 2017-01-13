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
        private static List<string> _assemblies;

        static void AddIncludeDirs(String dir)
        {
            _options.IncludeDirs.Add(dir);
        }

        static void ParseCommandLineArgs(string[] args)
        {
            var showHelp = args.Length == 0;

            var optionSet = new Mono.Options.OptionSet()
            {
                { "h|header=", "the path to an header file to generate source from", h => _options.HeaderFiles.Add(h) },
                { "pa|path=", "the path of a folder whose files will generate code (can append a filter at the end like '<path>/*.hpp'", pa => { GetFilesFromPath(pa); } },
                { "inc|includedir=", "the path of a folder to search for include files", i => { AddIncludeDirs(i); } },
                { "l|library=", "the path of a library that includes the definitions for the generated source code", l => _options.Libraries.Add(l) },
                { "ld|librarydir=", "the path of a folder to search for additional libraries", l => _options.LibraryDirs.Add(l) },
                { "d|define=", "a define to add for the parse of the given header files", d => _options.Defines.Add(d) },
                { "od|outputdir=", "the path for the destination folder that will contain the generated code", od => _options.OutputDir = od },
                { "on|outputnamespace=", "the namespace that will be used for the generated code", on => _options.OutputNamespace = on },
                { "iln|inputlibraryname=", "the name of the shared library that contains the actual definitions (without extension)", iln => _options.InputLibraryName = iln },
                { "isln|inputsharedlibraryname=", "the full name of the shared library that contains the actual definitions (with extension)", isln => _options.InputSharedLibraryName = isln },
                { "gen|generator=", "the type of generated code: 'chsarp' or 'cli' ('cli' supported only for Windows)", g => { GetGeneratorKind(g); } },
                { "p|platform=", "the platform that the generated code will target: 'win', 'osx', 'linux'", p => { GetDestinationPlatform(p); } },
                { "a|arch=", "the architecture that the generated code will target: 'x86', 'x64'", a => { GetDestinationArchitecture(a); } },
                { "c++11", "enables GCC C++ 11 compilation (valid only for Linux platform)", cpp11 => { _options.Cpp11ABI = (cpp11 != null); } },
                { "cs|checksymbols", "enable the symbol check for the generated code", cs => { _options.CheckSymbols = (cs != null); } },
                { "ub|unitybuild", "enable unity build", ub => { _options.UnityBuild = (ub != null); } },
                { "help", "shows the help", hl => { showHelp = (hl != null); } }
            };
            
            try
            {
                _assemblies = optionSet.Parse(args);
            }
            catch (Mono.Options.OptionException e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(0);
            }

            if (showHelp)
            {
                // Print usage and exit.
                Console.WriteLine("{0} [options]+", AppDomain.CurrentDomain.FriendlyName);
                Console.WriteLine("Generates target language bindings for interop with unmanaged code.");
                Console.WriteLine();
                optionSet.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }

            if (_assemblies == null)
            {
                Console.WriteLine("Invalid arguments.");
                Environment.Exit(0);
            }
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
                    break;
                case "cli":
                    _options.Kind = CppSharp.Generators.GeneratorKind.CLI;
                    break;
            }

            throw new NotSupportedException("Unknown generator kind: " + generator);
        }

        static void GetDestinationPlatform(String platform)
        {
            switch (platform.ToLower())
            {
                case "win":
                    _options.Platform = TargetPlatform.Windows;
                    break;
                case "osx":
                    _options.Platform = TargetPlatform.MacOS;
                    break;
                case "linux":
                    _options.Platform = TargetPlatform.Linux;
                    break;
            }

            throw new NotSupportedException("Unknown target platform: " + platform);
        }

        static void GetDestinationArchitecture(String architecture)
        {
            switch (architecture.ToLower())
            {
                case "x86":
                    _options.Architecture = TargetArchitecture.x86;
                    break;
                case "x64":
                    _options.Architecture = TargetArchitecture.x64; 
                    break;
            }

            throw new NotSupportedException("Unknown target architecture: " + architecture);
        }

        static void Main(string[] args)
        {
            ParseCommandLineArgs(args);

            Generator gen = new Generator(_options);

            try
            {
                gen.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }
    }
}