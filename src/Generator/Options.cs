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

            SystemModule = new Module("Std") { OutputNamespace = string.Empty };
            Modules = new List<Module> { SystemModule };

            GeneratorKind = GeneratorKind.CSharp;
            OutputInteropIncludes = true;

            Encoding = Encoding.ASCII;

            StripLibPrefix = true;

            ExplicitlyPatchedVirtualFunctions = new HashSet<string>();
        }

        #region General options

        /// <summary>
        /// Set to true to enable quiet output mode.
        /// </summary>
        public bool Quiet;

        /// <summary>
        /// Set to true to enable verbose output mode.
        /// </summary>
        public bool Verbose;

        /// <summary>
        /// Set to true to simulate generating without actually writing
        /// any output to disk.
        /// </summary>
        public bool DryRun;

        /// <summary>
        /// Whether the generated code should be automatically compiled.
        /// </summary>
        public bool CompileCode;

        #endregion

        #region Parser options

        /// <summary>
        /// If this option is off (the default), each header is parsed separately
        /// which is much slower but safer because of a clean state of the preprocessor
        /// for each header.
        /// </summary>
        public bool UnityBuild { get; set; }

        #endregion

        #region Module options

        public Module SystemModule { get; }
        public List<Module> Modules { get; }

        [Obsolete("Do not use.")]
        public Module MainModule
        {
            get
            {
                if (Modules.Count == 1)
                    AddModule("Main");
                return Modules[1];
            }
        }

        [Obsolete("Use Modules and Module.Headers instead.")]
        public List<string> Headers => MainModule.Headers;

        [Obsolete("Use Modules and Module.Libraries instead.")]
        public List<string> Libraries => MainModule.Libraries;

        [Obsolete("Use Modules and Module.SharedLibraryName instead.")]
        public string SharedLibraryName
        {
            get { return MainModule.SharedLibraryName; }
            set { MainModule.SharedLibraryName = value; }
        }

        [Obsolete("Use Modules and Module.OutputNamespace instead.")]
        public string OutputNamespace
        {
            get { return MainModule.OutputNamespace; }
            set { MainModule.OutputNamespace = value; }
        }

        [Obsolete("Use Modules and Module.LibraryName instead.")]
        public string LibraryName
        {
            get { return MainModule.LibraryName; }
            set { MainModule.LibraryName = value; }
        }

        [Obsolete("Use Modules and Module.SymbolsLibraryName instead.")]
        public string SymbolsLibraryName
        {
            get { return MainModule.SymbolsLibraryName; }
            set { MainModule.SymbolsLibraryName = value; }
        }

        public Module AddModule(string libraryName)
        {
            var module = new Module(libraryName);
            Modules.Add(module);
            return module;
        }

        public bool DoAllModulesHaveLibraries() =>
            Modules.All(m => m == SystemModule || m.Libraries.Count > 0);

        #endregion

        public CompilationOptions Compilation;

        #region Generator options

        public GeneratorKind GeneratorKind;

        public bool CheckSymbols;

        public string OutputDir;

        public bool OutputInteropIncludes;
        public bool GenerateFunctionTemplates;
        public bool GenerateInternalImports;
        public bool GenerateSequentialLayout { get; set; }
        public bool UseHeaderDirectories;

        /// <summary>
        /// If set to true, the generated code will be generated with extra
        /// debug output.
        /// </summary>
        public bool GenerateDebugOutput;

        /// <summary>
        /// If set to true the CLI generator will use ObjectOverridesPass to create
        /// Equals, GetHashCode and (if the insertion operator &lt;&lt; is overloaded)
        /// ToString methods.
        /// </summary>
        public bool GenerateObjectOverrides;

        //List of include directories that are used but not generated
        public List<string> NoGenIncludeDirs;

        /// <summary>
        /// Enable this option to enable generation of finalizers.
        /// Works in both CLI and C# backends.
        /// </summary>
        public bool GenerateFinalizers;

        public string IncludePrefix;
        public Func<TranslationUnit, string> GenerateName;

        /// <summary>
        /// Set this option to the kind of comments that you want generated
        /// in the source code. This overrides the default kind set by the
        /// target generator.
        /// </summary>
        public CommentKind? CommentKind;

        public Encoding Encoding { get; set; }

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

        #endregion
    }

    public class InvalidOptionException : Exception
    {
        public InvalidOptionException(string message) :
            base(message)
        {
        }
    }
}
