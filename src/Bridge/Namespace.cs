using System;
using System.Collections.Generic;

namespace Cxxi
{
	/// <summary>
	/// Represents a C++ namespace.
	/// </summary>
	public class Namespace
	{
		public Namespace()
			: this(null, String.Empty)
		{
		}

		public Namespace(Namespace parent, string name, bool isAnonymous = false)
		{
			Name = name;
			Parent = parent;
			IsAnonymous = isAnonymous;

			Enums = new List<Enumeration>();
			Functions = new List<Function>();
			Classes = new List<Class>();
		}

		public Enumeration FindEnum(string Name)
		{
			return FindEnumWithName(Name);
		}

		public Function FindFunction(string Name)
		{
			return Functions.Find(e => e.Name.Equals(Name));
		}

		public Class FindClass(string Name)
		{
			return Classes.Find(e => e.Name.Equals(Name));
		}

		public T FindType<T>(string Name) where T : Declaration
		{
			var type = FindEnumWithName(Name)
				?? FindFunction(Name) ?? (Declaration)FindClass(Name);

			return type as T;
		}

		public Enumeration FindEnumWithName(string Name)
		{
			return Enums.Find(e => e.Name.Equals(Name));
		}

		public Enumeration FindEnumWithItem(string Name)
		{
			return Enums.Find(e => e.ItemsByName.ContainsKey(Name));
		}

		public bool HasDeclarations
		{
			get
			{
				Predicate<Declaration> pred = (t => !t.Ignore);
				return Enums.Exists(pred) || HasFunctions || Classes.Exists(pred);
			}
		}

		public bool HasFunctions
		{
			get
			{
				Predicate<Declaration> pred = (t => !t.Ignore);
				return Functions.Exists(pred);
			}
		}

		public string Name { get; set; }
		public Namespace Parent { get; set; }
		public bool IsAnonymous { get; set; }

		public List<Enumeration> Enums;
		public List<Function> Functions;
		public List<Class> Classes;
	}
}