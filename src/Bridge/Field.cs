namespace Cxxi
{
	/// <summary>
	/// Represents a field in a C/C++ record declaration.
	/// </summary>
	public class Field : Declaration
	{
		public Type Type;
		public AccessSpecifier Access;
		public uint Offset = 0;

		public Field()
		{
		}

		public Field(string name, Type type, AccessSpecifier access)
		{
			Name = name;
			Type = type;
			Access = access;
		}
	}
}