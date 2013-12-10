using System;

#if !OLD_PARSER
using CppSharp.Parser;
using CppSharp.Parser.AST;
#else
using CppSharp.AST;
#endif

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
        /// Parses a C++ source file to a translation unit.
        /// </summary>
        public ParserResult ParseSourceFile(SourceFile file, ParserOptions options)
        {
            options.ASTContext = ASTContext;
            options.FileName = file.Path;

            var result = Parser.ClangParser.ParseHeader(options);
            SourceParsed(file, result);

            return result;
        }

        /// <summary>
        /// Parses the project source files.
        /// </summary>
        public void ParseProject(Project project, ParserOptions options)
        {
            // TODO: Search for cached AST trees on disk
            // TODO: Do multi-threaded parsing of source files

            foreach (var source in project.Sources)
                ParseSourceFile(source, options);
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

#if !OLD_PARSER
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
            var newLibrary = new AST.NativeLibrary { FileName = library.FileName };

            foreach (var symbol in library.Symbols)
                newLibrary.Symbols.Add(symbol);

            return newLibrary;
        }
#endif
    }
}
