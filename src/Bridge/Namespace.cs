using System;
using System.Collections.Generic;

namespace Cxxi
{
	/// <summary>
	/// Represents a C++ namespace.
	/// </summary>
	public class Namespace
	{
		public string Name { get; set; }
		public Namespace Parent { get; set; }
		public bool IsAnonymous { get; set; }

		public List<Namespace> Namespaces;
		public List<Enumeration> Enums;
		public List<Function> Functions;
		public List<Class> Classes;

		public Namespace()
			: this(null, String.Empty)
		{
		}

		public Namespace(Namespace parent, string name, bool isAnonymous = false)
		{
			Name = name;
			Parent = parent;
			IsAnonymous = isAnonymous;

			Namespaces = new List<Namespace>();
			Enums = new List<Enumeration>();
			Functions = new List<Function>();
			Classes = new List<Class>();
		}

		public Namespace FindNamespace(string name)
		{
			return Namespaces.Find(e => e.Name.Equals(name));
		}

		public Enumeration FindEnum(string name)
		{
			return Enums.Find(e => e.Name.Equals(name));
		}

		public Function FindFunction(string name)
		{
			return Functions.Find(e => e.Name.Equals(name));
		}

		public Class FindClass(string name, bool create = false)
		{
			Class @class = Classes.Find(e => e.Name.Equals(name));

			if (@class == null && create)
			{
				@class = new Class();
				@class.Name = name;

				Classes.Add(@class);
			}

			return @class;
		}

		public T FindType<T>(string name) where T : Declaration
		{
			var type = FindEnum(name)
				?? FindFunction(name) ?? (Declaration)FindClass(name);

			return type as T;
		}

		public Enumeration FindEnumWithItem(string name)
		{
			return Enums.Find(e => e.ItemsByName.ContainsKey(name));
		}

		public bool HasDeclarations
		{
			get
			{
				Predicate<Declaration> pred = (t => !t.Ignore);
				return Enums.Exists(pred) || HasFunctions
					|| Classes.Exists(pred) || Namespaces.Exists(n => n.HasDeclarations);
			}
		}

		public bool HasFunctions
		{
			get
			{
				Predicate<Declaration> pred = (t => !t.Ignore);
				return Functions.Exists(pred) || Namespaces.Exists(n => n.HasFunctions);
			}
		}
	}
}