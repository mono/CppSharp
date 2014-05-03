﻿using System;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Types;
#if !OLD_PARSER
using CppAbi = CppSharp.Parser.AST.CppAbi;
#endif

namespace CppSharp
{
    /// <summary>
    /// Generates C# and C++/CLI bindings for the CppSharp.CppParser project.
    /// </summary>
    class ParserGen : ILibrary
    {
        internal readonly GeneratorKind Kind;
        internal readonly string Triple;
        internal readonly CppAbi Abi;

        public ParserGen(GeneratorKind kind, string triple, CppAbi abi)
        {
            Kind = kind;
            Triple = triple;
            Abi = abi;
        }

        static string GetSourceDirectory()
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "src");

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception("Could not find sources directory");
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.TargetTriple = Triple;
            options.Abi = Abi;
            options.LibraryName = "CppSharp.CppParser.dll";
            options.GeneratorKind = Kind;
            options.Headers.Add("AST.h");
            options.Headers.Add("CppParser.h");
            options.Libraries.Add("CppSharp.CppParser.lib");

            if (Triple.Contains("apple"))
                SetupMacOptions(options);

            if (Triple.Contains("linux"))
                SetupLinuxOptions(options);

            var basePath = Path.Combine(GetSourceDirectory(), "CppParser");
            Console.WriteLine("basePath: {0}", basePath);

#if OLD_PARSER
            options.IncludeDirs.Add(basePath);
            options.LibraryDirs.Add(".");

#else
            options.addIncludeDirs(basePath);
            options.addLibraryDirs(".");
#endif

            options.OutputDir = "../../../../src/CppParser/Bindings/";
            options.OutputDir += Kind.ToString();

            if (Kind == GeneratorKind.CSharp)
                options.OutputDir += "/" + options.TargetTriple;

            options.GenerateLibraryNamespace = false;
            options.CheckSymbols = false;
            options.Verbose = true;
        }

        private static void SetupLinuxOptions(DriverOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            const string LINUX_INCLUDE_BASE_DIR = @"C:\work\linux_c++_headers";
            const string LINUX_INCLUDE_PATH = LINUX_INCLUDE_BASE_DIR + @"\usr\include";
            const string LINUX_LIBSTDC_INCLUDE_PATH = LINUX_INCLUDE_BASE_DIR + @"\usr\include\c++\4.7";
            const string LINUX_LIBSTDC_PLATFORM_SPECIFIC_INCLUDE_PATH = LINUX_INCLUDE_BASE_DIR + @"\usr\include\x86_64-linux-gnu\c++\4.7";
            const string LINUX_PLATFORM_SPECIFIC_INCLUDE_PATH = LINUX_INCLUDE_BASE_DIR + @"\usr\include\x86_64-linux-gnu";
            //const string LINUX_LIBC6_INCLUDE_PATH = LINUX_INCLUDE_BASE_DIR + @"\usr\include\libc6";
#if OLD_PARSER
            options.SystemIncludeDirs.Add(LINUX_INCLUDE_PATH);
            options.SystemIncludeDirs.Add(LINUX_LIBSTDC_INCLUDE_PATH);
            options.SystemIncludeDirs.Add(LINUX_LIBSTDC_PLATFORM_SPECIFIC_INCLUDE_PATH);
            options.SystemIncludeDirs.Add(LINUX_PLATFORM_SPECIFIC_INCLUDE_PATH);
            //options.SystemIncludeDirs.Add(LINUX_LIBC6_INCLUDE_PATH);
#else
            options.addSystemIncludeDirs(LINUX_INCLUDE_PATH);
            options.addSystemIncludeDirs(LINUX_LIBSTDC_INCLUDE_PATH);
            options.addSystemIncludeDirs(LINUX_LIBSTDC_PLATFORM_SPECIFIC_INCLUDE_PATH);
            options.addSystemIncludeDirs(LINUX_PLATFORM_SPECIFIC_INCLUDE_PATH);
            //options.addSystemIncludeDirs(LINUX_LIBC6_INCLUDE_PATH);
#endif
        }

        private static void SetupMacOptions(DriverOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            const string MAC_INCLUDE_PATH = @"C:\Development\CppSharp\build\vs2012\lib\Release_x32\";
#if OLD_PARSER
            options.SystemIncludeDirs.Add(MAC_INCLUDE_PATH + @"include");
            options.SystemIncludeDirs.Add(MAC_INCLUDE_PATH + @"lib\libcxx\include");
            options.SystemIncludeDirs.Add(MAC_INCLUDE_PATH + @"lib\clang\4.2\include");
            options.Arguments.Add("-stdlib=libc++");
#else
            options.addSystemIncludeDirs(MAC_INCLUDE_PATH + @"include");
            options.addSystemIncludeDirs(MAC_INCLUDE_PATH + @"lib\libcxx\include");
            options.addSystemIncludeDirs(MAC_INCLUDE_PATH + @"lib\clang\4.2\include");
            options.addArguments("-stdlib=libc++");
#endif
        }

        public void SetupPasses(Driver driver)
        {
            driver.AddTranslationUnitPass(new CheckMacroPass());
            driver.AddTranslationUnitPass(new IgnoreStdFieldsPass());
            driver.AddTranslationUnitPass(new GetterSetterToPropertyPass());
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("CppSharp::ParserOptions");
            ctx.SetClassAsValueType("CppSharp::ParserDiagnostic");
            ctx.SetClassAsValueType("CppSharp::ParserResult");

            ctx.RenameNamespace("CppSharp::CppParser", "Parser");
        }

        public void Postprocess(Driver driver, ASTContext lib)
        {
        }

        public static void Main(string[] args)
        {
            // ConsoleDriver.Run(new ParserGen(GeneratorKind.CLI, "i686-pc-win32",
            //     CppAbi.Microsoft));
            // Console.WriteLine();

            // Console.WriteLine("Generating the C# parser bindings...");
            // ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-pc-win32",
            //     CppAbi.Microsoft));

            // Testing Linux bindings
            //Console.WriteLine("Generating the C++/CLI parser bindings...");
            //ConsoleDriver.Run(new ParserGen(GeneratorKind.CLI, "i686-pc-linux",
            //    CppAbi.Microsoft));
            //Console.WriteLine();

            Console.WriteLine("Generating the C# parser bindings...");
            ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-pc-linux-gnu",
                CppAbi.Itanium));

            // Uncoment the following lines to enable generation of Mac parser bindings.
            // This is disabled by default for now since it requires a non-trivial
            // environment setup: a copy of the Mac SDK native headers and a recent checkout
            // of libcxx since the one provided by the Mac SDK is not compatible with a recent
            // Clang frontend that we use to parse it.

            //ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-apple-darwin12.4.0",
            //    CppAbi.Itanium));
        }
    }

    public class IgnoreStdFieldsPass : TranslationUnitPass
    {
        public override bool VisitFieldDecl(Field field)
        {
            if (field.Ignore)
                return false;

            if (!IsStdType(field.QualifiedType)) return false;

            field.ExplicityIgnored = true;
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (function.Ignore)
                return false;

            if (function.Parameters.Any(param => IsStdType(param.QualifiedType)))
            {
                function.ExplicityIgnored = true;
                return false;
            }

            return true;
        }

        private bool IsStdType(QualifiedType type)
        {
            var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
            var typeName = type.Visit(typePrinter);

            return typeName.Contains("std::");
        }
    }
}
