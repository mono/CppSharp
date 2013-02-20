using System;
using System.IO;
using System.Reflection;
using Mono.Options;

namespace Cxxi
{
    public class Program
    {
        static void ShowHelp(OptionSet options)
        {
            var module = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            var exeName = Path.GetFileName(module.FileName);
            Console.WriteLine("Usage: " + exeName + " [options]+ headers");
            Console.WriteLine("Generates .NET bindings from C/C++ header files.");
            Console.WriteLine();
            Console.WriteLine("Options:");


            Console.WriteLine(@"
  --vs, --visualstudio=VALUE     - Visual studio version to use for 
                                      system include folders.
        Valid values:
            0  - autoprobe. (Default)
            11 - 2012, 10 - 2010, 9  - 2008, 8  - 2005
        Number can be find out from VSx0COMNTOOLS environment variable.

  -D,   --defines=VALUE          - Specify define for preprocessor.
                                   (E.g. WIN32, DEBUG)
  -I,   --include=VALUE          - Add include path
  --ns, --namespace=VALUE        - Namespace where C# wrappers will reside
                             
  -o,   --outdir=VALUE           - Output folder
        --debug
        --lib, --library=VALUE 
  -t,   --template=VALUE       
  -a,   --assembly=VALUE       
  
  -v,   --verbose              
  -h, -?, --help             
");
        }

        static bool ParseCommandLineOptions(String[] args, DriverOptions options)
        {
            var set = new OptionSet()
                {
                    // Parser options
                    { "D|defines=", v => options.Defines.Add(v) },
                    { "I|include=", v => options.IncludeDirs.Add(v) },
                    // Generator options
                    { "ns|namespace=", v => options.OutputNamespace = v },
                    { "o|outdir=", v => options.OutputDir = v },
                    { "debug", v => options.OutputDebug = true },
                    { "lib|library=", v => options.LibraryName = v },
                    { "t|template=", v => options.Template = v },
                    { "a|assembly=", v => options.Assembly = v },
                    // Misc. options
                    { "vs|visualstudio=", v => Int32.TryParse(v, out options.ToolsetToUse) },
                    { "v|verbose",  v => { options.Verbose = true; } },
                    { "h|?|help",   v => options.ShowHelpText = v != null },
                };

            if (args.Length == 0 || options.ShowHelpText)
            {
                ShowHelp(set);
                return false;
            }

            try
            {
                options.Headers = set.Parse(args);
            }
            catch (OptionException)
            {
                Console.WriteLine("Error parsing the command line.");
                ShowHelp(set);
                return false;
            }

            return true;
        }

        static bool ParseLibraryAssembly(string path, out ILibrary library)
        {
            library = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);

                var assembly = Assembly.LoadFile(fullPath);
                var types = assembly.FindDerivedTypes(typeof(ILibrary));

                foreach (var type in types)
                {
                    var attrs = type.GetCustomAttributes(typeof(LibraryTransformAttribute), true);
                    if (attrs == null) continue;

                    Console.WriteLine("Found library transform: {0}", type.Name);
                    library = (ILibrary)Activator.CreateInstance(type);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: assembly '{0}' could not be loaded: {1}", path, ex.Message);
                return false;
            }

            return true;
        }

        public static void Main(String[] args)
        {
            var options = new DriverOptions();

            if (!ParseCommandLineOptions(args, options))
                return;

            ILibrary library = null;
            if (!ParseLibraryAssembly(options.Assembly, out library))
                return;

            var driver = new Driver(options, library);
            driver.Setup();
            driver.ParseCode();
            driver.ProcessCode();
            driver.GenerateCode();
        }

        public static void Run(ILibrary library)
        {
            var options = new DriverOptions();

            var driver = new Driver(options, library);
            driver.Setup();
            driver.ParseCode();
            driver.ProcessCode();
            driver.GenerateCode();
        }
    }
}
