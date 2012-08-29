using System;
using System.Collections.Generic;

namespace Cxxi
{
	/// <summary>
	/// Represents a C++ method.
	/// </summary>
	public class Method : Function
	{

		public Method()
		{
		}

		public AccessSpecifier Access { get; set; }

		public bool IsVirtual { get; set; }
		public bool IsStatic { get; set; }
		public bool IsConst { get; set; }
		public bool IsArtificial { get; set; }
		public bool IsConstructor { get; set; }
		public bool IsDestructor { get; set; }
		public bool IsCopyCtor { get; set; }
	}
}