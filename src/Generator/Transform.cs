using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;
using Cxxi;

public abstract class Transformation
{
	public virtual bool ProcessType(Declaration type)
	{
		return false;
	}

	public virtual bool ProcessEnumItem(Enumeration.Item item)
	{
		return false;
	}
}

public class RenameTransform : Transformation
{
	public string Pattern;
	public string Replacement;

	public RenameTransform(string pattern, string replacement)
	{
		Pattern = pattern;
		Replacement = replacement;
	}

	public override bool ProcessType(Declaration type)
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
	public void RemovePrefix(string prefix)
	{
		Transformations.Add(new RenameTransform(prefix, String.Empty));
	}

	public void RemoveType(Declaration type)
	{

	}

	public Enumeration FindEnum(string name)
	{
		foreach (var module in Library.Modules)
		{
			var @enum = module.Global.FindEnumWithName(name);
			if (@enum != null)
				return @enum;
		}

		return null;
	}

	public Enumeration GetEnumWithMatchingItem(string Pattern)
	{
		foreach (var module in Library.Modules)
		{
			Enumeration @enum = module.Global.FindEnumWithItem(Pattern);
			if (@enum == null) continue;
			return @enum;
		}

		return null;
	}

	public Module IgnoreModuleWithName(string Pattern)
	{
		Module module = Library.Modules.Find(
			m => Regex.Match(m.FilePath, Pattern).Success);

		if (module != null)
			module.Ignore = true;

		return module;
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

	public Enumeration.Item GenerateEnumItemFromMacro(MacroDefine macro)
	{
		var item = new Enumeration.Item();
		item.Name = macro.Name;
		item.Expression = macro.Expression;
		item.Value = ParseMacroExpression(macro.Expression);
		
		return item;
	}

	public Enumeration GenerateEnumFromMacros(string Name, params string[] Macros)
	{
		Enumeration @enum = new Enumeration();
		@enum.Name = Name;

		var pattern = String.Join("|", Macros);
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
				module.Global.Enums.Add(@enum);
				break;
			}
		}

		return @enum;
	}
}