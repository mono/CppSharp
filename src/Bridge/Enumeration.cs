using System;
using System.Collections.Generic;

namespace Cxxi
{
	/// <summary>
	/// Represents a C/C++ enumeration.
	/// </summary>
	public class Enumeration : Declaration
	{
		[Flags]
		public enum EnumModifiers
		{
			Anonymous,
			Scoped,
			Flags
		}

		/// <summary>
		/// Represents a C/C++ enumeration item.
		/// </summary>
		public class Item
		{
			public string Name;
			public long Value;
			public string Expression;
			public string Comment;
			public bool ExplicitValue = true;
		}

		public Enumeration()
		{
			Items = new List<Item>();
			ItemsByName = new Dictionary<string, Item>();
			Type = new BuiltinType(PrimitiveType.Int32);
		}

		public Enumeration AddItem(Item item)
		{
			Items.Add(item);
			ItemsByName[item.Name] = item;
			return this;
		}

		public Enumeration SetFlags()
		{
			Modifiers |= EnumModifiers.Flags;
			return this;
		}

		public BuiltinType Type { get; set; }
		public EnumModifiers Modifiers { get; set; }

		public List<Item> Items;
		public Dictionary<string, Item> ItemsByName;
	}
}