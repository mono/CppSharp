using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

public enum FilterMode {
	Include,
	Exclude,
	External
}

public enum ImplementationType {
	@class,
	@struct
}

public struct Filter {

	public string TypeName { get; set; }
	public FilterMode Mode { get; set; }
	public ImplementationType ImplType { get; set; }

	public static Dictionary<string, Filter> Load (XDocument doc, out FilterMode @default)
	{
		string value;
		@default = (value = (string)doc.Root.Attribute ("default")) != null ? (FilterMode)Enum.Parse (typeof (FilterMode), value) : FilterMode.Include;

		var rules = from rule in doc.Root.Elements ()
		            let mode = (FilterMode)Enum.Parse (typeof (FilterMode), rule.Name.LocalName)
		            let impl = (value = (string)rule.Attribute ("implementation")) != null ? (ImplementationType)Enum.Parse (typeof (ImplementationType), value) : ImplementationType.@class
		            select new Filter { TypeName = rule.Value, Mode = mode, ImplType = impl };


		return rules.ToDictionary<Filter,string> (r => r.TypeName);
	}
}