using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Parser;
using ASTContext = CppSharp.Parser.AST.ASTContext;
using NativeLibrary = CppSharp.Parser.AST.NativeLibrary;

namespace CppSharp
{
    public static class ClangParser
    {
        /// <summary>
        /// Fired when source files are parsed.
        /// </summary>
        public static Action<IEnumerable<string>, ParserResult> SourcesParsed = delegate { };

        /// <summary>
        /// Fired when library files are parsed.
        /// </summary>
        public static Action<string, ParserResult> LibraryParsed = delegate { };

        /// <summary>
        /// Parses a C++ source file as a translation unit.
        /// </summary>
        public static ParserResult ParseSourceFile(string file, ParserOptions options)
        {
            return ParseSourceFiles(new[] { file }, options);
        }

        /// <summary>
        /// Parses a set of C++ source files as a single translation unit.
        /// </summary>
        public static ParserResult ParseSourceFiles(IEnumerable<string> files, ParserOptions options)
        {
            options.ASTContext = new ASTContext();

            foreach (var file in files)
                options.AddSourceFiles(file);

            var result = Parser.ClangParser.ParseHeader(options);
            SourcesParsed(files, result);

            return result;
        }

        /// <summary>
        /// Parses a library file with symbols.
        /// </summary>
        public static ParserResult ParseLibrary(LinkerOptions options)
        {
            var result = Parser.ClangParser.ParseLibrary(options);
            for (uint i = 0; i < options.LibrariesCount; i++)
                LibraryParsed(options.GetLibraries(i), result);

            return result;
        }

        /// <summary>
        /// Converts a native parser AST to a managed AST.
        /// </summary>
        public static AST.ASTContext ConvertASTContext(ASTContext context)
        {
            var converter = new ASTConverter(context);
            return converter.Convert();
        }

        public static AST.NativeLibrary ConvertLibrary(NativeLibrary library)
        {
            var newLibrary = new AST.NativeLibrary
            {
                FileName = library.FileName,
                ArchType = (ArchType)library.ArchType
            };

            for (uint i = 0; i < library.SymbolsCount; ++i)
            {
                var symbol = library.GetSymbols(i);
                newLibrary.Symbols.Add(symbol);
            }
            for (uint i = 0; i < library.DependenciesCount; i++)
            {
                newLibrary.Dependencies.Add(library.GetDependencies(i));
            }

            return newLibrary;
        }
    }
}
