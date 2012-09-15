using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mono.Options;
using Cxxi;

public class Options
{
	public Options()
	{
		IncludeDirs = new List<string>();
		Headers = new List<string>();
	}

	public bool Verbose = false;
	public bool ShowHelpText = false;
	public bool OutputDebug = false;
	public string OutputNamespace;
	public string OutputDir;
	public string Library;
	public List<string> IncludeDirs;
	public List<string> Headers;
}

class Program
{
	Library library;
	Options options;

	static void ShowHelp(OptionSet options)
	{
		var module = Process.GetCurrentProcess().MainModule;
		var exeName = Path.GetFileName(module.FileName);
		Console.WriteLine("Usage: " + exeName + " [options]+ headers");
		Console.WriteLine("Generates C# bindings from C/C++ header files.");
		Console.WriteLine();
		Console.WriteLine("Options:");
		options.WriteOptionDescriptions(Console.Out);
	}

	bool ParseCommandLineOptions(String[] args)
	{
		var set = new OptionSet()
		{
			// Parser options
			{ "C|compiler=", v => new object() },
			{ "D|defines=", v => new object() },
			{ "I|include=", v => options.IncludeDirs.Add(v) },
			// Generator options
			{ "ns|namespace=", v => options.OutputNamespace = v },
			{ "o|outdir=", v => options.OutputDir = v },
			{ "debug", v => options.OutputDebug = true },
			{ "lib|library=", v => options.Library = v },
			// Misc. options
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

	public void GenerateCode(LibraryTransform transform)
	{
		Console.WriteLine("Generating wrapper code...");

		if (library.Modules.Count > 0)
		{
			var gen = new Generator(library, options);

			transform.Preprocess(gen);

			gen.Process();

			transform.Postprocess(gen);

			gen.Generate();
		}
	}

	public void ParseCode()
	{
		var Opts = new ParserOptions();
		Opts.Library = library;
		Opts.Verbose = false;
		Opts.IncludeDirs = options.IncludeDirs;

		Console.WriteLine("Parsing native code...");

		foreach (var file in options.Headers)
		{
			var path = String.Empty;

			try
			{
				path = Path.GetFullPath(file);
			}
			catch (ArgumentException)
			{
				Console.WriteLine("Invalid path '" + file + "'.");
				continue;
			}

			var module = new Module(path);
			Opts.FileName = path;

			if (!ClangParser.Parse(Opts))
			{
				Console.WriteLine("  Could not parse '" + file + "'.");
				continue;
			}

			Console.WriteLine("  Parsed '" + file + "'.");
		}
	}

	public void Run(String[] args)
	{
		options = new Options();

		if (!ParseCommandLineOptions(args))
			return;

		library = new Library(options.OutputNamespace, options.Library);
		
		ParseCode();

		//var transform = new SDLTransforms();
		var transform = new ClangTransforms();

		GenerateCode(transform);
	}

	static void Main(String[] args)
	{
		var program = new Program();
		program.Run(args);

		Console.ReadKey();
	}
}