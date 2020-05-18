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

        #region Module options

        public Module SystemModule { get; }
        public List<Module> Modules { get; }

        public Module AddModule(string libraryName)
        {
            var module = new Module(libraryName);
            Modules.Add(module);
            return module;
        }

        public bool DoAllModulesHaveLibraries() =>
            Modules.All(m => m == SystemModule || m.Libraries.Count > 0);

        #endregion

        public CompilationOptions Compilation = new CompilationOptions();

        #region Generator options

        public GeneratorKind GeneratorKind;

        public bool CheckSymbols;

        public string OutputDir;

        public bool OutputInteropIncludes;
        public bool GenerateFunctionTemplates;
        /// <summary>
        /// C# only: gets or sets a value indicating whether to generate class templates.
        /// Still experimental - do not use in production.
        /// </summary>
        /// <value>
        ///   <c>true</c> to generate class templates; otherwise, <c>false</c>.
        /// </value>
        public bool GenerateClassTemplates { get; set; }
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

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public bool IsCSharpGenerator => GeneratorKind == GeneratorKind.CSharp;

        public bool IsCppGenerator => GeneratorKind == GeneratorKind.CPlusPlus;

        public bool IsCLIGenerator => GeneratorKind == GeneratorKind.CLI;

        public readonly List<string> DependentNameSpaces = new List<string>();
        public bool MarshalCharAsManagedChar { get; set; }

        /// <summary>
        /// Generates a single C# file.
        /// </summary>
        public bool GenerateSingleCSharpFile { get; set; } = true;

        /// <summary>
        /// Generates default values of arguments in the C# code.
        /// </summary>
        public bool GenerateDefaultValuesForArguments { get; set; }

        /// <summary>
        /// If set to false, then deprecated C/C++ declarations (those that have
        /// the `__attribute__((deprecated))` or equivalent attribute) will not be
        /// generated.
        /// </summary>
        public bool GenerateDeprecatedDeclarations { get; set; } = true;

        public bool StripLibPrefix { get; set; }

        /// <summary>
        /// C# end only: force patching of the virtual entries of the functions in this list.
        /// </summary>
        public HashSet<string> ExplicitlyPatchedVirtualFunctions { get; }

        public bool UsePropertyDetectionHeuristics { get; set; } = true;

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
