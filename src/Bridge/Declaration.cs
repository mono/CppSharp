namespace Cxxi
{
	/// <summary>
	/// Represents a C++ declaration.
	/// </summary>
	public class Declaration
	{
		// Name of the type.
		public string Name;

		// Doxygen-style brief comment.
		public string BriefComment;

		// Namespace the type is declared in.
		//public Namespace Namespace;

		// Wether the type should be ignored.
		public bool Ignore;

		// Contains a debug text of the type declaration.
		public string DebugText;

		public Declaration()
		{
		}

		public Declaration(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	/// <summary>
	/// Represents a type definition in C++.
	/// </summary>
	public class Typedef : Declaration
	{
		/// Type defined.
		public Type Type;
	}

	/// <summary>
	/// Represents a C preprocessor macro definition.
	/// </summary>
	public class MacroDefine : Declaration
	{
		// Contains the macro definition text.
		public string Expression;

		public MacroDefine()
		{
		}
	}
}