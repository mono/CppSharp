using System;

namespace Cxxi
{
	/// <summary>
	/// Represents a field in a C/C++ record declaration.
	/// </summary>
	public class Field : Declaration
	{
		public Field()
		{
		}

		public Field(string name, Type type, AccessSpecifier access)
		{
			Name = name;
			Type = type;
			Access = access;
		}

		public Type Type { get; set; }
		public AccessSpecifier Access { get; set; }
	}
}