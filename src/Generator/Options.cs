using System;
using System.Collections.Generic;
using System.IO;
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
            GenerateClassMarshals = false;
            OutputInteropIncludes = true;
            MaxIndent = 80;
            CommentPrefix = "///";

            Encoding = Encoding.ASCII;

            StripLibPrefix = true;

            ExplicitlyPatchedVirtualFunctions = new HashSet<string>();
        }

        // General options
        public bool Quiet;
        public bool ShowHelpText;
        public bool OutputDebug;

        /// <summary>
        /// Set to true to simulate generating without actually writing
        /// any output to disk. This can be useful to activate while
        /// debugging the parser generator so generator bugs do not get
        /// in the way while iterating.
        /// </summary>
        public bool DryRun;

        public Module SystemModule { get; private set; }
        public List<Module> Modules { get; private set; }

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
        public List<string> Headers { get { return MainModule.Headers; } }
        public bool IgnoreParseWarnings;
        public bool IgnoreParseErrors;

        // Library options
        public List<string> Libraries { get { return MainModule.Libraries; } }
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
        public bool GenerateClassMarshals;
        public bool GenerateInlines;
        public bool UseHeaderDirectories;

        /// <summary>
        /// If set to true the generator will use GetterSetterToPropertyPass to
        /// convert matching getter/setter pairs to properties.
        /// </summary>
        public bool GenerateProperties;

        /// <summary>
        /// If set to true the generator will use GetterSetterToPropertyAdvancedPass to
        /// convert matching getter/setter pairs to properties. This pass has slightly
        /// different semantics from GetterSetterToPropertyPass, it will more agressively
        /// try to match for matching properties.
        /// </summary>
        public bool GeneratePropertiesAdvanced;

        /// <summary>
        /// If set to true the generator will use ConstructorToConversionOperatorPass to
        /// create implicit and explicit conversion operators out of single argument
        /// constructors.
        /// </summary>
        public bool GenerateConversionOperators;

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
        public int MaxIndent;
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

        public bool IsCSharpGenerator
        {
            get { return GeneratorKind == GeneratorKind.CSharp; }
        }

        public bool IsCLIGenerator
        {
            get { return GeneratorKind == GeneratorKind.CLI; }
        }

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
        public HashSet<string> ExplicitlyPatchedVirtualFunctions { get; private set; }
    }

    public class InvalidOptionException : Exception
    {
        public InvalidOptionException(string message) :
            base(message)
        {
        }
    }
}
