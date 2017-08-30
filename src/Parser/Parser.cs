using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Parser;
using ASTContext = CppSharp.Parser.AST.ASTContext;
using NativeLibrary = CppSharp.Parser.AST.NativeLibrary;

namespace CppSharp
{
    public class ClangParser
    {
        /// <summary>
        /// Context with translation units ASTs.
        /// </summary>
        public ASTContext ASTContext { get; private set; }

        /// <summary>
        /// Fired when source files are parsed.
        /// </summary>
        public Action<IEnumerable<string>, ParserResult> SourcesParsed = delegate {};

        /// <summary>
        /// Fired when library files are parsed.
        /// </summary>
        public Action<string, ParserResult> LibraryParsed = delegate {};

        public ClangParser()
        {
            ASTContext = new ASTContext();
        }

        public ClangParser(ASTContext context)
        {
            ASTContext = context;
        }

        /// <summary>
        /// Gets target information for a specific target triple.
        /// </summary>
        public ParserTargetInfo GetTargetInfo(ParserOptions options)
        {
            options.ASTContext = ASTContext;

            return Parser.ClangParser.GetTargetInfo(options);
        }

        /// <summary>
        /// Parses a C++ source file as a translation unit.
        /// </summary>
        public ParserResult ParseSourceFile(string file, ParserOptions options)
        {
            return ParseSourceFiles(new [] { file }, options);
        }

        /// <summary>
        /// Parses a set of C++ source files as a single translation unit.
        /// </summary>
        public ParserResult ParseSourceFiles(IEnumerable<string> files, ParserOptions options)
        {
            options.ASTContext = ASTContext;

            foreach (var file in files)
                options.AddSourceFiles(file);

            var result = Parser.ClangParser.ParseHeader(options);
            SourcesParsed(files, result);

            return result;
        }

        /// <summary>
        /// Parses a library file with symbols.
        /// </summary>
        public ParserResult ParseLibrary(string file, ParserOptions options)
        {
            options.LibraryFile = file;

            var result = Parser.ClangParser.ParseLibrary(options);
            LibraryParsed(file, result);

            return result;
        }

        /// <summary>
        /// Converts a native parser AST to a managed AST.
        /// </summary>
        static public AST.ASTContext ConvertASTContext(ASTContext context)
        {
            var converter = new ASTConverter(context);
            return converter.Convert();
        }

        public static AST.NativeLibrary ConvertLibrary(NativeLibrary library)
        {
            var newLibrary = new AST.NativeLibrary
            {
                FileName = library.FileName,
                ArchType = (ArchType) library.ArchType
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
