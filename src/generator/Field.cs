//
// Field.cs: Represents a field of a C++ class
//

using System;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;

using Mono.VisualC.Interop;

class Field
{
	public Field (string name, CppType type) {
		Name = name;
		Type = type;
	}

	public string Name {
		get; set;
	}

	public CppType Type {
		get; set;
	}
}
