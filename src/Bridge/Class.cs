using System;
using System.Collections.Generic;

namespace Cxxi
{
	// A C++ access specifier.
	public enum AccessSpecifier
	{
		Private,
		Protected,
		Public
	}

	// Represents a base class of a C++ class.
	public class BaseClassSpecifier
	{
		BaseClassSpecifier(Class @class, AccessSpecifier access,
			bool isVirtual = false)
		{
			Class = @class;
			Access = access;
			IsVirtual = isVirtual;
		}

		public Class Class { get; set; }
		public AccessSpecifier Access { get; set; }
		public bool IsVirtual { get; set; }
	}

	// Represents a C++ virtual function table.
	public class VFTable
	{

	}

	// Represents a C++ virtual base table.
	public class VBTable
	{

	}

	// Represents ABI-specific layout details for a class.
	public class ClassLayout
	{
		public CppAbi ABI { get; set; }
		public bool HasOwnVFTable { get; set; }
		public VFTable VirtualFunctions { get; set; }
		public VBTable VirtualBases { get; set; }
	}

	// Represents a C++ record declaration.
	public class Class : Declaration
	{

		public Class()
		{
			Bases = new List<BaseClassSpecifier>();
			Fields = new List<Field>();
			Properties = new List<Property>();
			Methods = new List<Method>();
			NestedClasses = new List<Class>();
			NestedEnums = new List<Enumeration>();
			IsAbstract = false;
		}

		public List<BaseClassSpecifier> Bases;
		public List<Class> NestedClasses;
		public List<Enumeration> NestedEnums;
		public List<Field> Fields;
		public List<Property> Properties;
		public List<Method> Methods;

		public bool HasBase
		{
			get { return Bases.Count > 0; }
		}

		// True if the record is a POD (Plain Old Data) type.
		public bool IsPOD;

		// ABI-specific class layout.
		public List<ClassLayout> Layouts { get; set; }

		// True if class only provides pure virtual methods.
		public bool IsAbstract { get; set; }

		public string TemplateName { get; set; }
		public string TemplateClassName { get; set; }
	}
}