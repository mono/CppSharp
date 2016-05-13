using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;
using CppAbi = CppSharp.Parser.AST.CppAbi;

namespace CppSharp
{
    public class DriverOptions : ParserOptions
    {
        public static bool IsUnixPlatform
        {
            get
            {
                var platform = Environment.OSVersion.Platform;
                return platform == PlatformID.Unix || platform == PlatformID.MacOSX;
            }
        }

        public DriverOptions()
        {
            Abi = IsUnixPlatform ? CppAbi.Itanium : CppAbi.Microsoft;
            MicrosoftMode = !IsUnixPlatform;

            OutputDir = Directory.GetCurrentDirectory();

            Module = new Module();

            GeneratorKind = GeneratorKind.CSharp;
            GenerateLibraryNamespace = true;
            GeneratePartialClasses = true;
            GenerateClassMarshals = false;
            OutputInteropIncludes = true;
            MaxIndent = 80;
            CommentPrefix = "///";

            Encoding = Encoding.ASCII;

            CodeFiles = new List<string>();

            StripLibPrefix = true;

            ExplicitlyPatchedVirtualFunctions = new List<string>();
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

        // Parser options
        public List<string> Headers { get { return Module.Headers; } }
        public bool IgnoreParseWarnings;
        public bool IgnoreParseErrors;

        public Module Module { get; set; }

        public bool IsItaniumLikeAbi { get { return Abi != CppAbi.Microsoft; } }
        public bool IsMicrosoftAbi { get { return Abi == CppAbi.Microsoft; } }

        // Library options
        public List<string> Libraries { get { return Module.Headers; } }
        public bool CheckSymbols;

        public string SharedLibraryName
        {
            get { return Module.SharedLibraryName; }
            set { Module.SharedLibraryName = value; }
        }

        // Generator options
        public GeneratorKind GeneratorKind;

        public string OutputNamespace
        {
            get { return Module.OutputNamespace; }
            set { Module.OutputNamespace = value; }
        }

        public string OutputDir;

        public string LibraryName
        {
            get { return Module.LibraryName; }
            set { Module.LibraryName = value; }
        }

        public bool OutputInteropIncludes;
        public bool GenerateLibraryNamespace;
        public bool GenerateFunctionTemplates;
        public bool GeneratePartialClasses;
        public bool GenerateInterfacesForMultipleInheritance;
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
        /// Equals, GetHashCode and (if the insertion operator << is overloaded) ToString
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

        public string IncludePrefix;
        public bool WriteOnlyWhenChanged;
        public Func<TranslationUnit, string> GenerateName;
        public int MaxIndent;
        public string CommentPrefix;

        public Encoding Encoding { get; set; }

        public string InlinesLibraryName
        {
            get { return Module.InlinesLibraryName; }
            set { Module.InlinesLibraryName = value; }
        }

        public string TemplatesLibraryName
        {
            get { return Module.TemplatesLibraryName; }
            set { Module.TemplatesLibraryName = value; }
        }

        public bool IsCSharpGenerator
        {
            get { return GeneratorKind == GeneratorKind.CSharp; }
        }

        public bool IsCLIGenerator
        {
            get { return GeneratorKind == GeneratorKind.CLI; }
        }

        public List<string> CodeFiles { get; private set; }
        public readonly List<string> DependentNameSpaces = new List<string>();
        public bool MarshalCharAsManagedChar { get; set; }
        /// <summary>
        /// Generates a single C# file.
        /// </summary>
        public bool GenerateSingleCSharpFile { get; set; }

        /// <summary>
        /// Generates default values of arguments in the C# code.
        /// </summary>
        public bool GenerateDefaultValuesForArguments { get; set; }

        public bool StripLibPrefix { get; set; }

        /// <summary>
        /// C# end only: force patching of the virtual entries of the functions in this list.
        /// </summary>
        public List<string> ExplicitlyPatchedVirtualFunctions { get; private set; }
    }

    public class InvalidOptionException : Exception
    {
    }
}
