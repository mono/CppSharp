using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Cxxi;
using Cxxi.Templates;

public partial class Generator
{
	public List<Transformation> Transformations { get; set; }
	
	Library Library;
	Options Options;

	public Generator(Library library, Options options)
	{
		Transformations = new List<Transformation>();

		Library = library;
		Options = options;
	}

	public void Process()
	{
		TransformModule();
		ProcessModules();
	}

	// Generates the binding code.
	public void Generate()
	{
		GenerateModules();
	}

	int UniqueType = 0;

	void CleanupText(ref string debugText)
	{
		// Strip off newlines from the debug text.
		if (String.IsNullOrWhiteSpace(debugText))
			debugText = String.Empty;

		// TODO: Make this transformation in the output.
		debugText = Regex.Replace(debugText, " ( )+", " ");
		debugText = Regex.Replace(debugText, "\n", "");
	}

	void ProcessType(Declaration type)
	{
		// If after all the transformations the type still does
		// not have a name, then generate one.

		if (String.IsNullOrWhiteSpace(type.Name))
			type.Name = String.Format("UnnamedType{0}", UniqueType++);

		CleanupText(ref type.DebugText);
	}

	void ProcessTypes<T>(List<T> types) where T : Declaration
	{
		foreach (T type in types)
			ProcessType(type);
	}

	void ProcessClasses(List<Class> Classes)
	{
		ProcessTypes(Classes);

		foreach (var @class in Classes)
			ProcessTypes(@class.Fields);
	}

	void ProcessFunctions(List<Function> Functions)
	{
		ProcessTypes(Functions);

		foreach (var function in Functions)
		{
			if (function.ReturnType == null)
			{
				// Ignore and warn about unknown types.
				function.Ignore = true;
				
				var s = "Function '{0}' was ignored due to unknown return type...";
				Console.WriteLine( String.Format(s, function.Name) );
			}

			ProcessTypes(function.Parameters);
		}
	}

	void ProcessModules()
	{
		if (String.IsNullOrEmpty(Library.Name))
			Library.Name = "";

		// Process everything in the global namespace for now.
		foreach (var module in Library.Modules)
		{
			ProcessNamespace(module.Global);
		}
	}

	void ProcessNamespace(Namespace @namespace)
	{
		ProcessTypes(@namespace.Enums);
		ProcessFunctions(@namespace.Functions);
		ProcessClasses(@namespace.Classes);
	}

	void TransformModule()
	{
		if (String.IsNullOrEmpty(Library.Name))
			Library.Name = "";

		// Process everything in the global namespace for now.
		foreach (var module in Library.Modules)
		{
			Namespace global = module.Global;

			foreach (Enumeration @enum in global.Enums)
				TransformEnum(@enum);

			foreach (Function function in global.Functions)
				TransformFunction(function);

			foreach (Class @class in global.Classes)
				TransformClass(@class);
		}
	}

	void TransformType(Declaration type)
	{
		foreach (var transform in Transformations)
			transform.ProcessType(type);
	}

	void TransformClass(Class @class)
	{
		TransformType(@class);

		foreach (var field in @class.Fields)
			TransformType(field);
	}

	void TransformFunction(Function function)
	{
		TransformType(function);

		foreach (var param in function.Parameters)
			TransformType(param);
	}


	void TransformEnum(Enumeration @enum)
	{
		TransformType(@enum);

		foreach (var transform in Transformations)
		{
			foreach (var item in @enum.Items)
				transform.ProcessEnumItem(item);
		}

		// If the enumeration only has power of two values, assume it's
		// a flags enum.

		bool isFlags = true;
		bool hasBigRange = false;

		foreach (var item in @enum.Items)
		{
			if (item.Name.Length >= 1 && Char.IsDigit(item.Name[0]))
				item.Name = String.Format("_{0}", item.Name);

			long value = item.Value;
			if (value >= 4)
				hasBigRange = true;
			if (value <= 1 || value.IsPowerOfTwo())
				continue;
			isFlags = false;
		}

		// Only apply this heuristic if there are enough values to have a
		// reasonable chance that it really is a bitfield.

		if (isFlags && hasBigRange)
		{
			@enum.Modifiers |= Enumeration.EnumModifiers.Flags;
		}

		// If we still do not have a valid name, then try to guess one
		// based on the enum value names.

		if (!String.IsNullOrWhiteSpace(@enum.Name))
			return;

		var names = new List<string>();
		
		foreach (var item in @enum.Items)
			names.Add(item.Name);

		var prefix = names.ToArray().CommonPrefix();

		// Try a simple heuristic to make sure we end up with a valid name.
		if (prefix.Length >= 3)
		{
			prefix = prefix.Trim().Trim(new char[] { '_' });
			@enum.Name = prefix;
		}
	}

	void GenerateModules()
	{
		// Process everything in the global namespace for now.
		foreach (var module in Library.Modules)
		{
			if (module.Ignore || !module.HasDeclarations)
				continue;

			// Generate the code from templates.
			var template = new CSharpModule();
			template.Library = Library;
			template.Options = Options;
			template.Module = module;

			if (!Directory.Exists(Options.OutputDir))
				Directory.CreateDirectory(Options.OutputDir);

			var file = Path.GetFileNameWithoutExtension(module.FileName) + ".cs";
			var path = Path.Combine(Options.OutputDir, file);

			// Normalize path.
			path = Path.GetFullPath(path);

			string code = template.TransformText();

			Console.WriteLine("  Generated '" + file + "'.");
			File.WriteAllText(path, code);
		}
	}
}