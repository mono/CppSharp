using System;
using System.IO;
using CppSharp;

namespace Generator.Tests
{
    public class HeaderTestFixture
    {
        protected Driver Driver;
        protected DriverOptions Options;
        protected Library Library;

        private const string TestsDirectory = @"..\..\..\tests\Native";

        public HeaderTestFixture()
        {

        }

        protected void ParseLibrary(string file)
        {
            ParseLibrary(TestsDirectory, file);
        }

        protected void ParseLibrary(string dir, string file)
        {
            Options = new DriverOptions();

            var path = Path.Combine(Directory.GetCurrentDirectory(), dir);
            Options.IncludeDirs.Add(Path.GetFullPath(path));
            Options.Headers.Add(file);

            Driver = new Driver(Options, new TextDiagnosticPrinter());
            if (!Driver.ParseCode())
                throw new Exception("Error parsing the code");

            Library = Driver.Library;
        }
    }
}
