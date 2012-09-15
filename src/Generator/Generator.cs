using Cxxi.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Cxxi
{
	public partial class Generator
	{
		public List<ModuleTransform> Transformations { get; set; }

		Library Library;
		Options Options;

		public Generator(Library library, Options options)
		{
			Transformations = new List<ModuleTransform>();

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

		int UniqueName = 0;

		void CleanupText(ref string debugText)
		{
			// Strip off newlines from the debug text.
			if (String.IsNullOrWhiteSpace(debugText))
				debugText = String.Empty;

			// TODO: Make this transformation in the output.
			debugText = Regex.Replace(debugText, " ( )+", " ");
			debugText = Regex.Replace(debugText, "\n", "");
		}

		void ProcessDeclaration(Declaration decl)
		{
			// If after all the transformations the type still does
			// not have a name, then generate one.

			if (string.IsNullOrWhiteSpace(decl.Name))
				decl.Name = string.Format("Unnamed{0}", UniqueName++);

			CleanupText(ref decl.DebugText);
		}

		void ProcessDeclarations<T>(List<T> decls) where T : Declaration
		{
			foreach (T decl in decls)
				ProcessDeclaration(decl);
		}

		void ProcessClasses(List<Class> classes)
		{
			ProcessDeclarations(classes);

			foreach (var @class in classes)
				ProcessDeclarations(@class.Fields);
		}

		void ProcessTypedefs(Namespace @namespace, List<Typedef> typedefs)
		{
			ProcessDeclarations(typedefs);

			foreach (var typedef in typedefs)
			{
				var @class = @namespace.FindClass(typedef.Name);

				// Clang will walk the typedef'd tag type and the typedef decl,
				// so we ignore the class and process just the typedef.

				if (@class != null)
					typedef.Ignore = true;
			}
		}

		void ProcessFunctions(List<Function> Functions)
		{
			ProcessDeclarations(Functions);

			foreach (var function in Functions)
			{
				if (function.ReturnType == null)
				{
					// Ignore and warn about unknown types.
					function.Ignore = true;

					var s = "Function '{0}' was ignored due to unknown return type...";
					Console.WriteLine(String.Format(s, function.Name));
				}

				foreach (var param in function.Parameters)
				{
					ProcessDeclaration(param);
				}
			}
		}

		void ProcessModules()
		{
			if (string.IsNullOrEmpty(Library.Name))
				Library.Name = "";

			// Process everything in the global namespace for now.
			foreach (var module in Library.Modules)
			{
				ProcessNamespace(module);
			}
		}

		void ProcessNamespace(Namespace @namespace)
		{
			ProcessDeclarations(@namespace.Enums);
			ProcessFunctions(@namespace.Functions);
			ProcessClasses(@namespace.Classes);
			ProcessTypedefs(@namespace, @namespace.Typedefs);
		}

		void TransformModule()
		{
			if (string.IsNullOrEmpty(Library.Name))
				Library.Name = string.Empty;

			// Process everything in the global namespace for now.
			foreach (var module in Library.Modules)
			{
				foreach (Enumeration @enum in module.Enums)
					TransformEnum(@enum);

				foreach (Function function in module.Functions)
					TransformFunction(function);

				foreach (Class @class in module.Classes)
					TransformClass(@class);

				foreach (Typedef typedef in module.Typedefs)
					TransformTypedef(typedef);
			}
		}

		void TransformDeclaration(Declaration decl)
		{
			foreach (var transform in Transformations)
				transform.ProcessDeclaration(decl);
		}

		void TransformTypedef(Typedef typedef)
		{
			foreach (var transform in Transformations)
				transform.ProcessDeclaration(typedef);
		}

		void TransformClass(Class @class)
		{
			TransformDeclaration(@class);

			foreach (var field in @class.Fields)
				TransformDeclaration(field);
		}

		void TransformFunction(Function function)
		{
			TransformDeclaration(function);

			foreach (var param in function.Parameters)
				TransformDeclaration(param);
		}


		void TransformEnum(Enumeration @enum)
		{
			TransformDeclaration(@enum);

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
}