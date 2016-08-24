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
        public Action<IList<SourceFile>, ParserResult> SourcesParsed = delegate {};

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
        private ParserResult ParseSourceFile(SourceFile file)
        {
            var options = file.Options;
            options.ASTContext = ASTContext;
            options.addSourceFiles(file.Path);

            var result = Parser.ClangParser.ParseHeader(options);
            SourcesParsed(new[] { file }, result);

            return result;
        }

        /// <summary>
        /// Parses C++ source files to a translation unit.
        /// </summary>
        private void ParseSourceFiles(IList<SourceFile> files)
        {
            var options = files[0].Options;
            options.ASTContext = ASTContext;

            foreach (var file in files)
                options.addSourceFiles(file.Path);
            using (var result = Parser.ClangParser.ParseHeader(options))
                SourcesParsed(files, result);
        }

        /// <summary>
        /// Parses the project source files.
        /// </summary>
        public void ParseProject(Project project, bool unityBuild)
        {
            // TODO: Search for cached AST trees on disk
            // TODO: Do multi-threaded parsing of source files

            if (unityBuild)
                ParseSourceFiles(project.Sources);
            else
                foreach (var parserResult in project.Sources.Select(s => ParseSourceFile(s)).ToList())
                    parserResult.Dispose();
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
