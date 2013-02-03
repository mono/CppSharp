using System;
using System.Collections.Generic;

namespace Cxxi
{
    public class Parser
    {
        public Library Library { get; private set; }
        private readonly Options options;

        public Parser(Options options)
        {
            this.options = options;
            Library = new Library(options.OutputNamespace, options.LibraryName);
        }

        public void ParseHeaders(IEnumerable<string> headers)
        {
            foreach (var header in headers)
                ParseHeader(header);
        }

        bool ParseHeader(string file)
        {
            var parserOptions = new ParserOptions
            {
                Library = Library,
                FileName = file,
                Verbose = false,
                IncludeDirs = options.IncludeDirs,
                Defines = options.Defines,
                toolSetToUse = options.ToolsetToUse
            };

            var result = ClangParser.Parse(parserOptions);
            HeaderParsed(file, result);

            return result;
        }

        public Action<string, bool> HeaderParsed = delegate {};
    }
}
