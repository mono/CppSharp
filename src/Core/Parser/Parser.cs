using System;
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
        public Action<SourceFile, ParserResult> SourceParsed = delegate {};

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
        /// Get info about that target
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public ParserTargetInfo GetTargetInfo(ParserOptions options)
        {
            options.ASTContext = ASTContext;

            return Parser.ClangParser.GetTargetInfo(options);
        }

        /// <summary>
        /// Parses a C++ source file to a translation unit.
        /// </summary>
        public ParserResult ParseSourceFile(SourceFile file)
        {
            var options = file.Options;
            options.ASTContext = ASTContext;
            options.FileName = file.Path;

            var result = Parser.ClangParser.ParseHeader(options);
            SourceParsed(file, result);

            return result;
        }

        /// <summary>
        /// Parses the project source files.
        /// </summary>
        public void ParseProject(Project project)
        {
            // TODO: Search for cached AST trees on disk
            // TODO: Do multi-threaded parsing of source files

            foreach (var source in project.Sources)
                ParseSourceFile(source);
        }

        /// <summary>
        /// Parses a library file with symbols.
        /// </summary>
        public ParserResult ParseLibrary(string file, ParserOptions options)
        {
            options.FileName = file;

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
                var symbol = library.getSymbols(i);
                newLibrary.Symbols.Add(symbol);
            }
            for (uint i = 0; i < library.DependenciesCount; i++)
            {
                newLibrary.Dependencies.Add(library.getDependencies(i));
            }

            return newLibrary;
        }
    }
}
