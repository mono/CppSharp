//
// Property.cs: Represents a C++ property
//
using System;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;

using Mono.VisualC.Interop;

class Property
{
	public Property (string name, CppType type) {
		Name = name;
		Type = type;
	}

	public string Name {
		get; set;
	}

	public CppType Type {
		get; set;
	}

	public Method GetMethod {
		get; set;
	}

	public Method SetMethod {
		get; set;
	}

	public CodeMemberProperty GenerateProperty (Generator g) {
		var p = new CodeMemberProperty () { Name = Name, Attributes = MemberAttributes.Public|MemberAttributes.Final };
		p.Type = g.CppTypeToCodeDomType (Type);
		if (GetMethod != null) {
			p.GetStatements.Add (new CodeMethodReturnStatement (new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), GetMethod.Name), new CodeExpression [] { new CodeFieldReferenceExpression (null, "Native") })));
		}
		if (SetMethod != null) {
			p.SetStatements.Add (new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), SetMethod.Name), new CodeExpression [] { new CodeFieldReferenceExpression (null, "Native"), new CodeArgumentReferenceExpression ("value") }));
		}
		return p;
	}
}
