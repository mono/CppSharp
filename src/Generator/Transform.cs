using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cxxi
{
	/// <summary>
	/// Used to massage the library types into something more .NET friendly.
	/// </summary>
	public interface LibraryTransform
	{
		/// <summary>
		/// Do transformations that should happen before processing here.
		/// </summary>
		void Preprocess(Generator g);

		/// <summary>
		/// Do transformations that should happen after processing here.
		/// </summary>
		void Postprocess(Generator g);
	}

	/// <summary>
	/// Used to provide different types of code transformation on a module
	/// declarations and types before the code generation process is started.
	/// </summary>
	public abstract class ModuleTransform
	{
		/// <summary>
		/// Processes a declaration.
		/// </summary>
		public virtual bool ProcessDeclaration(Declaration declaration)
		{
			return false;
		}

		/// <summary>
		/// Processes an enum item.
		/// </summary>
		public virtual bool ProcessEnumItem(Enumeration.Item item)
		{
			return false;
		}
	}

	/// <summary>
	/// Renames a declaration based on a regular expression pattern.
	/// </summary>
	public class RenameTransform : ModuleTransform
	{
		public string Pattern;
		public string Replacement;

		public RenameTransform(string pattern, string replacement)
		{
			Pattern = pattern;
			Replacement = replacement;
		}

		public override bool ProcessDeclaration(Declaration type)
		{
			return Rename(ref type.Name);
		}

		public override bool ProcessEnumItem(Enumeration.Item item)
		{
			return Rename(ref item.Name);
		}

		bool Rename(ref string name)
		{
			string replace = Regex.Replace(name, Pattern, Replacement);

			if (!name.Equals(replace))
			{
				name = replace;
				return true;
			}

			return false;
		}
	}

	public partial class Generator
	{
		#region Transform Operations

		public void RemovePrefix(string prefix)
		{
			Transformations.Add(new RenameTransform(prefix, String.Empty));
		}

		public void RemoveType(Declaration type)
		{

		}

		#endregion

		#region Enum Helpers

		public Enumeration FindEnum(string name)
		{
			foreach (var module in Library.Modules)
			{
				var @enum = module.FindEnum(name);
				if (@enum != null)
					return @enum;
			}

			return null;
		}

		public void IgnoreEnumWithMatchingItem(string Pattern)
		{
			Enumeration @enum = GetEnumWithMatchingItem(Pattern);
			if (@enum != null)
				@enum.Ignore = true;
		}

		public void SetNameOfEnumWithMatchingItem(string Pattern, string Name)
		{
			Enumeration @enum = GetEnumWithMatchingItem(Pattern);
			if (@enum != null)
				@enum.Name = Name;
		}

		public void SetNameOfEnumWithName(string enumName, string name)
		{
			Enumeration @enum = FindEnum(enumName);
			if (@enum != null)
				@enum.Name = name;
		}

		public Enumeration GetEnumWithMatchingItem(string Pattern)
		{
			foreach (var module in Library.Modules)
			{
				Enumeration @enum = module.FindEnumWithItem(Pattern);
				if (@enum == null) continue;
				return @enum;
			}

			return null;
		}

		public Enumeration.Item GenerateEnumItemFromMacro(MacroDefine macro)
		{
			var item = new Enumeration.Item();
			item.Name = macro.Name;
			item.Expression = macro.Expression;
			item.Value = ParseMacroExpression(macro.Expression);

			return item;
		}

		public Enumeration GenerateEnumFromMacros(string name, params string[] macros)
		{
			Enumeration @enum = new Enumeration();
			@enum.Name = name;

			var pattern = String.Join("|", macros);
			var regex = new Regex(pattern);

			foreach (var module in Library.Modules)
			{
				foreach (var macro in module.Macros)
				{
					var match = regex.Match(macro.Name);
					if (!match.Success) continue;

					var item = GenerateEnumItemFromMacro(macro);
					@enum.AddItem(item);
				}

				if (@enum.Items.Count > 0)
				{
					module.Enums.Add(@enum);
					break;
				}
			}

			return @enum;
		}

		#endregion

		#region Class Helpers

		public Class FindClass(string name)
		{
			foreach (var module in Library.Modules)
			{
				var @class = module.FindClass(name);
				if (@class != null)
					return @class;
			}

			return null;
		}

		public void SetNameOfClassWithName(string className, string name)
		{
			Class @class = FindClass(className);
			if (@class != null)
				@class.Name = name;
		}

		#endregion

		#region Function Helpers

		public Function FindFunction(string name)
		{
			foreach (var module in Library.Modules)
			{
				var function = module.FindFunction(name);
				if (function != null)
					return function;
			}

			return null;
		}

		public void IgnoreFunctionWithName(string name)
		{
			Function function = FindFunction(name);
			if (function != null)
				function.Ignore = true;
		}

		#endregion

		#region Module Helpers

		public Module IgnoreModuleWithName(string Pattern)
		{
			Module module = Library.Modules.Find(
				m => Regex.Match(m.FilePath, Pattern).Success);

			if (module != null)
				module.Ignore = true;

			return module;
		}

		#endregion

		static bool ParseToNumber(string num, out long val)
		{
			if (num.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
			{
				num = num.Substring(2);

				return long.TryParse(num, NumberStyles.HexNumber,
						CultureInfo.CurrentCulture, out val);
			}

			return long.TryParse(num, out val);
		}

		static long ParseMacroExpression(string Expression)
		{
			long val;
			if (ParseToNumber(Expression, out val))
				return val;
			// TODO: Handle string expressions
			return 0;
		}

	}
}