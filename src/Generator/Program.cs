using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mono.Options;
using Cxxi;

public class Options
{
	public bool Verbose = false;
	public string IncludeDirs;
	public string OutputDir;
	public bool ShowHelpText = false;
	public bool OutputDebug = false;
	public string OutputNamespace;
	public List<string> Headers;
}

class Program
{
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
			{ "I|include=", v => options.IncludeDirs = v },
			// Generator options
			{ "ns|namespace=", v => options.OutputNamespace = v },
			{ "o|outdir=", v => options.OutputDir = v },
			{ "debug", v => options.OutputDebug = true },
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
		catch (OptionException ex)
		{
			Console.WriteLine("Error parsing the command line.");
			ShowHelp(set);
			return false;
		}

		return true;
	}

	Library library;
	Options options;

	public void GenerateCode()
	{
		Console.WriteLine("Generating wrapper code...");

		if (library.Modules.Count > 0)
		{
			var gen = new Generator(library, options);
			TransformSDL(gen);
			gen.Generate();
		}
	}

	public void ParseCode()
	{
		var Opts = new ParserOptions();
		Opts.Library = library;
		Opts.Verbose = false;

		Console.WriteLine("Parsing native code...");

		foreach (var file in options.Headers)
		{
			var path = String.Empty;

			try
			{
				path = Path.GetFullPath(file);
			}
			catch (ArgumentException ex)
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

		library = new Library(options.OutputNamespace);
		
		ParseCode();
		GenerateCode();
	}

	static void Main(String[] args)
	{
		var program = new Program();
		program.Run(args);

		Console.ReadKey();
	}
}