﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators;
#if !OLD_PARSER
using CppSharp.Parser;
using CppAbi = CppSharp.Parser.AST.CppAbi;
#endif

namespace CppSharp
{
    public class DriverOptions : ParserOptions
    {
        static public bool IsUnixPlatform
        {
            get
            {
                var platform = Environment.OSVersion.Platform;
                return platform == PlatformID.Unix || platform == PlatformID.MacOSX;
            }
        }
        public DriverOptions()
        {
            Headers = new List<string>();
            Libraries = new List<string>();

            Abi = IsUnixPlatform ? CppAbi.Itanium : CppAbi.Microsoft;
            MicrosoftMode = !IsUnixPlatform;

            OutputDir = Directory.GetCurrentDirectory();
            Libraries = new List<string>();
            CheckSymbols = false;

            GeneratorKind = GeneratorKind.CSharp;
            GenerateLibraryNamespace = true;
            GeneratePartialClasses = true;
            GenerateClassMarshals = false;
            OutputInteropIncludes = true;
            MaxIndent = 80;
            CommentPrefix = "///";

            Encoding = Encoding.ASCII;

            CodeFiles = new List<string>();

            Is32Bit = true;
#if IS_64_BIT
            Is32Bit = false;
#endif
        }

        // General options
        public bool Quiet;
        public bool ShowHelpText;
        public bool OutputDebug;

        // Parser options
        public List<string> Headers;
        public bool IgnoreParseWarnings;
        public bool IgnoreParseErrors;

        public bool IsItaniumAbi { get { return Abi == CppAbi.Itanium; } }
        public bool IsMicrosoftAbi { get { return Abi == CppAbi.Microsoft; } }

        // Library options
        public List<string> Libraries;
        public bool CheckSymbols;

        private string sharedLibraryName;
        public string SharedLibraryName
        {
            get
            {
                if (string.IsNullOrEmpty(sharedLibraryName))
                    return LibraryName;
                return sharedLibraryName;
            }
            set { sharedLibraryName = value; }
        }

        // Generator options
        public GeneratorKind GeneratorKind;
        public string OutputNamespace;
        public string OutputDir;
        public string LibraryName;
        public bool OutputInteropIncludes;
        public bool GenerateLibraryNamespace;
        public bool GenerateFunctionTemplates;
        public bool GeneratePartialClasses;
        public bool GenerateVirtualTables;
        public bool GenerateAbstractImpls;
        public bool GenerateInterfacesForMultipleInheritance;
        public bool GenerateInternalImports;
        public bool GenerateClassMarshals;
        public bool GenerateInlines;
        public bool GenerateCopyConstructors;
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

        //List of include directories that are used but not generated
        public List<string> NoGenIncludeDirs;
        public string NoGenIncludePrefix = "";

        /// <summary>
        /// Wether the generated C# code should be automatically compiled.
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

        private string inlinesLibraryName;
        public string InlinesLibraryName
        {
            get
            {
                if (string.IsNullOrEmpty(inlinesLibraryName))
                {
                    return string.Format("{0}-inlines", OutputNamespace);
                }
                return inlinesLibraryName;
            }
            set { inlinesLibraryName = value; }
        }

        public bool IsCSharpGenerator
        {
            get { return GeneratorKind == GeneratorKind.CSharp; }
        }

        public bool IsCLIGenerator
        {
            get { return GeneratorKind == GeneratorKind.CLI; }
        }

        public bool Is32Bit { get; set; }

        public List<string> CodeFiles { get; private set; }
        public readonly List<string> DependentNameSpaces = new List<string>();
        public bool MarshalCharAsManagedChar { get; set; }
    }

    public class InvalidOptionException : Exception
    {
    }
}
