//
// Class.cs: Represents a C++ class
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
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
using System.Reflection;
using System.Linq;
using System.CodeDom;
using System.CodeDom.Compiler;

class Class
{
	public Class (Node n) {
		Node = n;
		BaseClasses = new List<Class> ();
		Methods = new List<Method> ();
		Fields = new List<Field> ();
		Properties = new List<Property> ();
	}

	public Node Node {
		get; set;
	}

	public string Name {
		get {
			return Node.Name;
		}
	}

	public List<Class> BaseClasses {
		get; set;
	}

	public List<Method> Methods {
		get; set;
	}

	public List<Field> Fields {
		get; set;
	}

	public List<Property> Properties {
		get; set;
	}

	public bool Disable {
		get; set;
	}

	public CodeTypeDeclaration GenerateClass (Generator g, CodeTypeDeclaration libDecl, string libFieldName) {
		var decl = new CodeTypeDeclaration (Name);
		var hasBase = BaseClasses.Count > 0;
		decl.IsPartial = true;

		if (hasBase)
			decl.BaseTypes.Add (new CodeTypeReference (BaseClasses [0].Name));
		else
			decl.BaseTypes.Add (new CodeTypeReference ("ICppObject"));

		var layout = new CodeTypeDeclaration ("_" + Name) {
			IsStruct = true,
			TypeAttributes = TypeAttributes.NotPublic
		};
		decl.Members.Add (layout);

		foreach (var f in Fields) {
			var field = new CodeMemberField {
				Name = f.Name,
				Type = g.CppTypeToCodeDomType (f.Type),
				Attributes = MemberAttributes.Public
			};
			layout.Members.Add (field);
		}

		var iface = new CodeTypeDeclaration ("I" + Name);
		iface.IsInterface = true;
		iface.BaseTypes.Add (new CodeTypeReference ("ICppClassOverridable", new CodeTypeReference [] { new CodeTypeReference (decl.Name) }));
		decl.Members.Add (iface);

		var implField = new CodeMemberField (new CodeTypeReference (iface.Name), "impl") {
			Attributes = MemberAttributes.Private | MemberAttributes.Static
		};
		var getclass = new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (libDecl.Name), libFieldName), "GetClass", new CodeTypeReference [] { new CodeTypeReference (iface.Name), new CodeTypeReference (layout.Name), new CodeTypeReference (decl.Name) });
		implField.InitExpression = new CodeMethodInvokeExpression (getclass, new CodeExpression [] { new CodePrimitiveExpression (Name) });
		decl.Members.Add (implField);
		//private static IClass impl = global::CppTests.Libs.Test.GetClass <IClass, _Class, Class>("Class");

		if (!hasBase) {
			var ptrField = new CodeMemberField (new CodeTypeReference ("CppInstancePtr"), "native_ptr") {
				Attributes = MemberAttributes.Family
			};
			decl.Members.Add (ptrField);
		}

		var nativeCtor = new CodeConstructor () {
			Attributes = MemberAttributes.Public
		};
		nativeCtor.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference ("CppInstancePtr"), "native"));
		nativeCtor.Statements.Add (new CodeAssignStatement (new CodeFieldReferenceExpression (null, "native_ptr"), new CodeArgumentReferenceExpression ("native")));
		if (hasBase) {
			var implTypeInfo = new CodeFieldReferenceExpression (new CodeFieldReferenceExpression { FieldName = "impl" }, "TypeInfo");
			nativeCtor.BaseConstructorArgs.Add (implTypeInfo);
		}
		decl.Members.Add (nativeCtor);

		var subclassCtor = new CodeConstructor () {
				Attributes = MemberAttributes.Public
		};
		subclassCtor.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference ("CppTypeInfo"), "subClass"));
		subclassCtor.Statements.Add (new CodeExpressionStatement (new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeArgumentReferenceExpression ("subClass"), "AddBase"), new CodeExpression [] { new CodeFieldReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), "TypeInfo") })));
		if (hasBase) {
			var implTypeInfo = new CodeFieldReferenceExpression (new CodeFieldReferenceExpression { FieldName = "impl" }, "TypeInfo");
			subclassCtor.BaseConstructorArgs.Add (implTypeInfo);
		}
		decl.Members.Add (subclassCtor);

		if (!hasBase) {
			var nativeProperty = new CodeMemberProperty () {
				Name = "Native",
				Type = new CodeTypeReference ("CppInstancePtr"),
				Attributes = MemberAttributes.Public|MemberAttributes.Final
			};
			nativeProperty.GetStatements.Add (new CodeMethodReturnStatement (new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), "native_ptr")));
			decl.Members.Add (nativeProperty);
		}

		var disposeMethod = new CodeMemberMethod () {
				Name = "Dispose",
				Attributes = MemberAttributes.Public
		};
		if (Methods.Any (m => m.IsDestructor))
			disposeMethod.Statements.Add (new CodeExpressionStatement (new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), "Destruct"), new CodeExpression [] { new CodeFieldReferenceExpression (null, "Native") })));
		disposeMethod.Statements.Add (new CodeExpressionStatement (new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (null, "Native"), "Dispose"))));
		decl.Members.Add (disposeMethod);

		foreach (Method m in Methods) {
			iface.Members.Add (m.GenerateIFaceMethod (g));

			if (m.GenWrapperMethod) {
				var cm = m.GenerateWrapperMethod (g);
				if (m.IsConstructor && hasBase) {
					var implTypeInfo = new CodeFieldReferenceExpression (new CodeFieldReferenceExpression { FieldName = "impl" }, "TypeInfo");
					(cm as CodeConstructor).BaseConstructorArgs.Add (implTypeInfo);
				}
				decl.Members.Add (cm);
			}
		}

		// Add all members inherited from non-primary bases
		// With the exception of virtual methods that have been overridden, these methods must be called
		//  thru a cast to the base class that performs a this ptr adjustment
		foreach (var baseClass in BaseClasses.Skip (1)) {
			foreach (var method in baseClass.Methods) {
				if (method.IsConstructor || (method.IsVirtual && Methods.Any (m => m.Node.CheckValue ("overrides", method.Node.Id))))
					continue;

				if (method.GenWrapperMethod)
					decl.Members.Add (method.GenerateInheritedWrapperMethod (g, baseClass));
			}
			foreach (var prop in baseClass.Properties) {
				decl.Members.Add (prop.GenerateInheritedProperty (g, baseClass));
			}

			// generate implicit cast to base class
			// 1. Create field to cache base casts
			decl.Members.Add (new CodeMemberField (baseClass.Name, baseClass.Name + "_base"));

			// 2. Add op_Implicit
			// FIXME: Can't figure out language-neutral way to do this with codedom.. C# only for now
			decl.Members.Add (new CodeSnippetTypeMember (string.Format ("public static implicit operator {0}({1} subClass) {{\n\t\t\tif (subClass.{2} == null)\n\t\t\t\tsubClass.{2} = impl.TypeInfo.Cast<{0}>(subClass);\n\t\t\treturn subClass.{2};\n\t\t}}\n\t\t", baseClass.Name, Name, baseClass.Name + "_base")));

		}

		foreach (Property p in Properties) {
			decl.Members.Add (p.GenerateProperty (g));
		}

		return decl;
	}
}
