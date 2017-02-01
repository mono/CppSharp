using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp
{
    public class DriverOptions
    {
        public DriverOptions()
        {
            OutputDir = Directory.GetCurrentDirectory();

            SystemModule = new Module { OutputNamespace = string.Empty, LibraryName = "Std" };
            Modules = new List<Module> { SystemModule };

            GeneratorKind = GeneratorKind.CSharp;
            OutputInteropIncludes = true;
            CommentPrefix = "///";

            Encoding = Encoding.ASCII;

            StripLibPrefix = true;

            ExplicitlyPatchedVirtualFunctions = new HashSet<string>();
        }

        // General options

        /// <summary>
        /// Set to true to enable quiet output mode.
        /// </summary>
        public bool Quiet;
        public bool OutputDebug;

        /// <summary>
        /// Set to true to enable verbose output mode.
        /// </summary>
        public bool Verbose;

        /// <summary>
        /// Set to true to simulate generating without actually writing
        /// any output to disk. This can be useful to activate while
        /// debugging the parser generator so generator bugs do not get
        /// in the way while iterating.
        /// </summary>
        public bool DryRun;

        public Module SystemModule { get; }
        public List<Module> Modules { get; }

        public Module MainModule
        {
            get
            {
                if (Modules.Count == 1)
                    Modules.Add(new Module());
                return Modules[1];
            }
        }

        // Parser options
        public List<string> Headers => MainModule.Headers;

        // Library options
        public List<string> Libraries => MainModule.Libraries;
        public bool CheckSymbols;

        public string SharedLibraryName
        {
            get { return MainModule.SharedLibraryName; }
            set { MainModule.SharedLibraryName = value; }
        }

        // Generator options
        public GeneratorKind GeneratorKind;

        public string OutputNamespace
        {
            get { return MainModule.OutputNamespace; }
            set { MainModule.OutputNamespace = value; }
        }

        public string OutputDir;

        public string LibraryName
        {
            get { return MainModule.LibraryName; }
            set { MainModule.LibraryName = value; }
        }

        public bool OutputInteropIncludes;
        public bool GenerateFunctionTemplates;
        public bool GenerateInternalImports;
        public bool UseHeaderDirectories;

        /// <summary>
        /// If set to true the CLI generator will use ObjectOverridesPass to create
        /// Equals, GetHashCode and (if the insertion operator &lt;&lt; is overloaded) ToString
        /// methods.
        /// </summary>
        public bool GenerateObjectOverrides;

        //List of include directories that are used but not generated
        public List<string> NoGenIncludeDirs;

        /// <summary>
        /// Whether the generated C# code should be automatically compiled.
        /// </summary>
        public bool CompileCode;

        /// <summary>
        /// Enable this option to enable generation of finalizers.
        /// Works in both CLI and C# backends.
        /// </summary>
        public bool GenerateFinalizers;

        /// <summary>
        /// If this option is off (the default), each header is parsed separately which is much slower
        /// but safer because of a clean state of the preprocessor for each header.
        /// </summary>
        public bool UnityBuild { get; set; }

        public string IncludePrefix;
        public Func<TranslationUnit, string> GenerateName;
        public string CommentPrefix;

        public Encoding Encoding { get; set; }

        public string InlinesLibraryName
        {
            get { return MainModule.InlinesLibraryName; }
            set { MainModule.InlinesLibraryName = value; }
        }

        public string TemplatesLibraryName
        {
            get { return MainModule.TemplatesLibraryName; }
            set { MainModule.TemplatesLibraryName = value; }
        }

        public bool IsCSharpGenerator => GeneratorKind == GeneratorKind.CSharp;

        public bool IsCLIGenerator => GeneratorKind == GeneratorKind.CLI;

        public readonly List<string> DependentNameSpaces = new List<string>();
        public bool MarshalCharAsManagedChar { get; set; }

        /// <summary>
        /// Generates default values of arguments in the C# code.
        /// </summary>
        public bool GenerateDefaultValuesForArguments { get; set; }

        public bool StripLibPrefix { get; set; }

        /// <summary>
        /// C# end only: force patching of the virtual entries of the functions in this list.
        /// </summary>
        public HashSet<string> ExplicitlyPatchedVirtualFunctions { get; }

        public bool DoAllModulesHaveLibraries() =>
            Modules.All(m => m == SystemModule || m.Libraries.Count > 0);
    }

    public class InvalidOptionException : Exception
    {
        public InvalidOptionException(string message) :
            base(message)
        {
        }
    }
}
