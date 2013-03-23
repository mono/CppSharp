using System;
using System.Collections.Generic;

namespace Cxxi
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
            bool hasErrors = false;
            foreach (var header in headers)
            {
                var result = ParseHeader(header);

                // If we have some error, report to end-user.
                if (!options.IgnoreErrors)
                {
                    foreach (var diag in result.Diagnostics)
                    {
                        if (diag.Level == ParserDiagnosticLevel.Error ||
                            diag.Level == ParserDiagnosticLevel.Fatal)
                        {
                            Console.WriteLine(string.Format("{0}({1},{2}): error: {3}",
                                diag.FileName, diag.LineNumber, diag.ColumnNumber,
                                diag.Message));
                            hasErrors = true;
                        }
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
                Defines = options.Defines,
                ToolSetToUse = options.ToolsetToUse
            };

            var result = ClangParser.Parse(parserOptions);
            HeaderParsed(file, result);

            return result;
        }

        public Action<string, ParserResult> HeaderParsed = delegate {};
    }
}
