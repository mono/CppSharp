using System;
using System.Collections.Generic;
using System.IO;
using Mono.Options;

namespace CppSharp
{
    class CLI
    {
        private static OptionSet optionSet = new OptionSet();
        private static Options options = new Options();
        
        static bool ParseCommandLineArgs(string[] args, List<String> messages, ref bool helpShown)
        {
            var showHelp = false;

            optionSet.Add("I=", "the {PATH} of a folder to search for include files", (i) => { AddIncludeDirs(i, messages); });
            optionSet.Add("l=", "{LIBRARY} that that contains the symbols of the generated code", l => options.Libraries.Add(l) );
            optionSet.Add("L=", "the {PATH} of a folder to search for additional libraries", l => options.LibraryDirs.Add(l) );
            optionSet.Add("D:", "additional define with (optional) value to add to be used while parsing the given header files", (n, v) => AddDefine(n, v, messages) );

            optionSet.Add("o=|output=", "the {PATH} for the generated bindings file (doesn't need the extension since it will depend on the generator)", v => HandleOutputArg(v, messages) );
            optionSet.Add("on=|outputnamespace=", "the {NAMESPACE} that will be used for the generated code", on => options.OutputNamespace = on );

            optionSet.Add("iln=|inputlibraryname=", "the {NAME} of the shared library that contains the symbols of the generated code", iln => options.InputLibraryName = iln );

            optionSet.Add("g=|gen=|generator=", "the {TYPE} of generated code: 'chsarp' or 'cli' ('cli' supported only for Windows)", g => { GetGeneratorKind(g, messages); } );
            optionSet.Add("p=|platform=", "the {PLATFORM} that the generated code will target: 'win', 'osx' or 'linux'", p => { GetDestinationPlatform(p, messages); } );
            optionSet.Add("a=|arch=", "the {ARCHITECTURE} that the generated code will target: 'x86' or 'x64'", a => { GetDestinationArchitecture(a, messages); } );

            optionSet.Add("c++11", "enables GCC C++ 11 compilation (valid only for Linux platform)", cpp11 => { options.Cpp11ABI = (cpp11 != null); } );
            optionSet.Add("cs|checksymbols", "enable the symbol check for the generated code", cs => { options.CheckSymbols = (cs != null); } );
            optionSet.Add("ub|unitybuild", "enable unity build", ub => { options.UnityBuild = (ub != null); } );

            optionSet.Add("h|help", "shows the help", hl => { showHelp = (hl != null); });

            List<String> additionalArguments = null;

            try
            {
                additionalArguments = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
                        
            if (showHelp || additionalArguments != null && additionalArguments.Count == 0)
            {
                helpShown = true;
                ShowHelp();
                return false;
            }

            foreach(String s in additionalArguments)
                HandleAdditionalArgument(s, messages);

            return true;
        }

        static void ShowHelp()
        {
            String appName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            appName = Path.GetFileName(appName);

            Console.WriteLine();
            Console.WriteLine("Usage: {0} [OPTIONS]+ [FILES]+", appName);
            Console.WriteLine("Generates target language bindings to interop with unmanaged code.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Useful informations:");
            Console.WriteLine(" - to specify a file to generate bindings from you just have to add their path without any option flag, just like you");
            Console.WriteLine("   would do with GCC compiler. You can specify a path to a single file (local or absolute) or a path to a folder with");
            Console.WriteLine("   a search query.");
            Console.WriteLine("   e.g.: '{0} [OPTIONS]+ my_header.h' will generate the bindings for the file my_header.h", appName);
            Console.WriteLine("   e.g.: '{0} [OPTIONS]+ include/*.h' will generate the bindings for all the '.h' files inside the include folder", appName);
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

        static void AddIncludeDirs(String dir, List<String> messages)
        {
            if (Directory.Exists(dir))
                options.IncludeDirs.Add(dir);
            else
                messages.Add(String.Format("Directory {0} doesn't exist. Not adding as include directory.", dir));
        }

        static void HandleOutputArg(String arg, List<String> messages)
        {
            try
            {
                String dir = Path.GetDirectoryName(arg);
                String file = Path.GetFileNameWithoutExtension(arg);

                options.OutputDir = dir;
                options.OutputFileName = file;
            }
            catch(Exception e)
            {
                messages.Add(e.Message);

                options.OutputDir = "";
                options.OutputFileName = "";
            }
        }

        static void AddDefine(String name, String value, List<String> messages)
        {
            if (name == null)
                messages.Add("Invalid definition name for option -D.");
            else
                options.Defines.Add(name, value);
        }

        static void HandleAdditionalArgument(String args, List<String> messages)
        {
            if (Path.IsPathRooted(args) == false)
                args = Path.Combine(Directory.GetCurrentDirectory(), args);

            try
            {
                bool searchQuery = args.IndexOf('*') >= 0 || args.IndexOf('?') >= 0;
                bool isDirectory = searchQuery || (File.GetAttributes(args) & FileAttributes.Directory) == FileAttributes.Directory;

                if (isDirectory)
                    GetFilesFromPath(args, messages);
                else if (File.Exists(args))
                    options.HeaderFiles.Add(args);
                else
                {
                    messages.Add(String.Format("File {0} doesn't exist. Not adding to the list of files to generate bindings from.", args));
                }
            }
            catch(Exception)
            {
                messages.Add(String.Format("Error while looking for files inside path {0}. Ignoring.", args));
            }
        }

        static void GetFilesFromPath(String path, List<String> messages)
        {
            path = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            String searchPattern = String.Empty;
            int lastSeparatorPosition = path.LastIndexOf(Path.AltDirectorySeparatorChar);

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
                    String[] files = Directory.GetFiles(path, searchPattern);

                    foreach (String s in files)
                        options.HeaderFiles.Add(s);
                }
                else
                {
                    String[] files = Directory.GetFiles(path);

                    foreach (String s in files)
                        options.HeaderFiles.Add(s);
                }
            }
            catch (Exception)
            {
                messages.Add(String.Format("Error while looking for files inside path {0}. Ignoring.", path));
            }
        }

        static void GetGeneratorKind(String generator, List<String> messages)
        {
            switch (generator.ToLower())
            {
                case "csharp":
                    options.Kind = CppSharp.Generators.GeneratorKind.CSharp;
                    return;
                case "cli":
                    options.Kind = CppSharp.Generators.GeneratorKind.CLI;
                    return;
            }

            messages.Add(String.Format("Unknown generator kind: {0}. Defaulting to {1}", generator, options.Kind.ToString()));
        }

        static void GetDestinationPlatform(String platform, List<String> messages)
        {
            switch (platform.ToLower())
            {
                case "win":
                    options.Platform = TargetPlatform.Windows;
                    return;
                case "osx":
                    options.Platform = TargetPlatform.MacOS;
                    return;
                case "linux":
                    options.Platform = TargetPlatform.Linux;
                    return;
            }

            messages.Add(String.Format("Unknown target platform: {0}. Defaulting to {1}", platform, options.Platform.ToString()));
        }

        static void GetDestinationArchitecture(String architecture, List<String> messages)
        {
            switch (architecture.ToLower())
            {
                case "x86":
                    options.Architecture = TargetArchitecture.x86;
                    return;
                case "x64":
                    options.Architecture = TargetArchitecture.x64;
                    return;
            }

            messages.Add(String.Format("Unknown target architecture: {0}. Defaulting to {1}", architecture, options.Architecture.ToString()));
        }

        static void PrintMessages(List<String> messages)
        {
            foreach (String m in messages)
                Console.WriteLine(m);
        }

        static void Main(string[] args)
        {
            List<String> messages = new List<String>();
            bool helpShown = false;

            try
            {
                if (ParseCommandLineArgs(args, messages, ref helpShown) == false)
                {
                    PrintMessages(messages);

                    // Don't need to show the help since if ParseCommandLineArgs returns false the help has already been shown
                    return;
                }

                Generator gen = new Generator(options);

                bool validOptions = gen.ValidateOptions(messages);

                PrintMessages(messages);

                if (validOptions)
                    gen.Run();
                else if (helpShown == false)
                    ShowHelp();
            }
            catch (Exception ex)
            {
                PrintMessages(messages);
                Console.WriteLine(ex.Message);
            }
        }
    }
}