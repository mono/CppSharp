//
// Property.cs: Represents a C++ property
//
// Author:
//   Zoltan Varga <vargaz@gmail.com>
//
// Copyright (C) 2011 Novell Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
			p.SetStatements.Add (new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), SetMethod.Name), new CodeExpression [] { new CodeFieldReferenceExpression (null, "Native"), new CodePropertySetValueReferenceExpression () }));
		}
		return p;
	}

	public CodeMemberProperty GenerateInheritedProperty (Generator g, Class baseClass) {
		var p = new CodeMemberProperty () { Name = Name, Attributes = MemberAttributes.Public|MemberAttributes.Final };
		p.Type = g.CppTypeToCodeDomType (Type);
		if (GetMethod != null) {
			p.GetStatements.Add (new CodeMethodReturnStatement (new CodePropertyReferenceExpression (new CodeCastExpression (baseClass.Name, new CodeThisReferenceExpression ()), GetMethod.Name)));
		}
		if (SetMethod != null) {
			p.SetStatements.Add (new CodeAssignStatement (new CodePropertyReferenceExpression (new CodeCastExpression (baseClass.Name, new CodeThisReferenceExpression ()), SetMethod.Name), new CodePropertySetValueReferenceExpression ()));
		}
		return p;
	}
}
