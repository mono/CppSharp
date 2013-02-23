using System;
using System.IO;
using Cxxi;
using Cxxi.Types;

namespace Generator.Tests
{
    public class HeaderTestFixture
    {
        protected Library library;
        protected TypeMapDatabase database;

        private const string TestsDirectory = @"..\..\..\tests\Native";

        protected void ParseLibrary(string file)
        {
            ParseLibrary(TestsDirectory, file);
        }

        protected void ParseLibrary(string dir, string file)
        {
            database = new TypeMapDatabase();
            database.SetupTypeMaps();

            var options = new DriverOptions();

            var path = Path.Combine(Directory.GetCurrentDirectory(), dir);
            options.IncludeDirs.Add(path);

            var parser = new Parser(options);
            var result = parser.ParseHeader(file);

            if (result.Kind != ParserResultKind.Success)
                throw new Exception("Could not parse file: " + file);

            library = result.Library;

            foreach (var diag in result.Diagnostics)
                Console.WriteLine(diag.Message);
        }
    }
}
