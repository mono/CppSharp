using System;
using System.Collections.Generic;

namespace CppSharp
{
    public class Parser
    {
        public Library Library { get; private set; }
        private readonly DriverOptions options;

        public Parser(DriverOptions options)
        {
            this.options = options;
            Library = new Library(options.OutputNamespace, options.LibraryName);
        }

        public bool ParseHeaders(IEnumerable<string> headers)
        {
            var hasErrors = false;

            foreach (var header in headers)
            {
                var result = ParseHeader(header);

                // If we have some error, report to end-user.
                if (!options.IgnoreParseErrors)
                {
                    foreach (var diag in result.Diagnostics)
                    {
                        if (diag.Level == ParserDiagnosticLevel.Error ||
                            diag.Level == ParserDiagnosticLevel.Fatal)
                            hasErrors = true;
                    }
                }
            }

            return !hasErrors;
        }

        public ParserResult ParseHeader(string file)
        {
            var parserOptions = new ParserOptions
            {
                Library = Library,
                FileName = file,
                Verbose = false,
                IncludeDirs = options.IncludeDirs,
                SystemIncludeDirs = options.SystemIncludeDirs,
                Defines = options.Defines,
                NoStandardIncludes = options.NoStandardIncludes,
                NoBuiltinIncludes = options.NoBuiltinIncludes,
                MicrosoftMode = options.MicrosoftMode,
                ToolSetToUse = options.ToolsetToUse,
                TargetTriple = options.TargetTriple
            };

            var result = ClangParser.ParseHeader(parserOptions);
            OnHeaderParsed(file, result);

            return result;
        }

        public bool ParseLibraries(IEnumerable<string> libraries)
        {
            var hasErrors = false;

            foreach (var lib in libraries)
            {
                var result = ParseLibrary(lib);

                // If we have some error, report to end-user.
                if (!options.IgnoreParseErrors)
                {
                    foreach (var diag in result.Diagnostics)
                    {
                        if (diag.Level == ParserDiagnosticLevel.Error ||
                            diag.Level == ParserDiagnosticLevel.Fatal)
                            hasErrors = true;
                    }
                }
            }

            return !hasErrors;
        }

        public ParserResult ParseLibrary(string file)
        {
            var parserOptions = new ParserOptions
            {
                Library = Library,
                FileName = file,
                Verbose = false,
                LibraryDirs = options.LibraryDirs,
                ToolSetToUse = options.ToolsetToUse
            };

            var result = ClangParser.ParseLibrary(parserOptions);
            OnLibraryParsed(file, result);

            return result;
        }

        public Action<string, ParserResult> OnHeaderParsed = delegate {};
        public Action<string, ParserResult> OnLibraryParsed = delegate { };

    }
}
