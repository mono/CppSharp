using System;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;

using Mono.VisualC.Interop;

public class Parameter
{
	public Parameter (String name, CppType type) {
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
