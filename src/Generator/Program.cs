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

	public void ParseNativeHeaders()
	{
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

            var Opts = new ParserOptions();
            Opts.FileName = path;
            Opts.Library = library;
            Opts.Verbose = false;

			if (!ClangParser.Parse(Opts))
			{
				Console.WriteLine("  Could not parse '" + file + "'.");
				continue;
			}

			Console.WriteLine("  Parsed '" + file + "'.");
		}
	}

	void TransformSDL(Generator g)
	{
		g.IgnoreEnumWithMatchingItem("SDL_FALSE");
		g.IgnoreEnumWithMatchingItem("DUMMY_ENUM_VALUE");
		g.IgnoreEnumWithMatchingItem("SDL_ENOMEM");

		g.SetNameOfEnumWithMatchingItem("SDL_SCANCODE_UNKNOWN", "ScanCode");
		g.SetNameOfEnumWithMatchingItem("SDLK_UNKNOWN", "Key");
		g.SetNameOfEnumWithMatchingItem("KMOD_NONE", "KeyModifier");
		g.SetNameOfEnumWithMatchingItem("SDL_LOG_CATEGORY_CUSTOM", "LogCategory");

		g.GenerateEnumFromMacros("InitFlags", "SDL_INIT_(.*)").SetFlags();
		g.GenerateEnumFromMacros("Endianness", "SDL_(.*)_ENDIAN");
		g.GenerateEnumFromMacros("KeyState", "SDL_RELEASED", "SDL_PRESSED");

		g.GenerateEnumFromMacros("AlphaState", "SDL_ALPHA_(.*)");

		g.GenerateEnumFromMacros("HatState", "SDL_HAT_(.*)");

		g.IgnoreModuleWithName("SDL_atomic*");
		g.IgnoreModuleWithName("SDL_endian*");
		g.IgnoreModuleWithName("SDL_main*");
		g.IgnoreModuleWithName("SDL_mutex*");
		g.IgnoreModuleWithName("SDL_stdinc*");

		g.RemovePrefix("SDL_");
		g.RemovePrefix("SCANCODE_");
		g.RemovePrefix("SDLK_");
		g.RemovePrefix("KMOD_");
		g.RemovePrefix("LOG_CATEGORY_");

		g.Process();

		g.FindEnum("PIXELTYPE").Name = "PixelType";
		g.FindEnum("BITMAPORDER").Name = "BitmapOrder";
		g.FindEnum("PACKEDORDER").Name = "PackedOrder";
		g.FindEnum("ARRAYORDER").Name = "ArrayOrder";
		g.FindEnum("PACKEDLAYOUT").Name = "PackedLayout";
		g.FindEnum("PIXELFORMAT").Name = "PixelFormat";
		g.FindEnum("assert_state").Name = "AssertState";

		//gen.FindEnum("LOG_CATEGORY").Name = "LogCategory";
	}

	public void Run(String[] args)
	{
		options = new Options();

		if (!ParseCommandLineOptions(args))
			return;

		library = new Library(options.OutputNamespace);
		
		ParseNativeHeaders();
		GenerateCode();
	}

	static void Main(String[] args)
	{
		var program = new Program();
		program.Run(args);

		Console.ReadKey();
	}
}