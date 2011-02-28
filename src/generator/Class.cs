//
// Class.cs: Represents a C++ class
//
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
		Methods = new List<Method> ();
		Fields = new List<Field> ();
	}

	public Node Node {
		get; set;
	}

	public string Name {
		get {
			return Node.Name;
		}
	}

	public List<Method> Methods {
		get; set;
	}

	public List<Field> Fields {
		get; set;
	}

	public CodeTypeDeclaration GenerateClass (Generator g, CodeTypeDeclaration libDecl) {
		var decl = new CodeTypeDeclaration (Name);
		decl.IsPartial = true;
		// FIXME: Inheritance
		decl.BaseTypes.Add (new CodeTypeReference ("ICppObject"));

		var layout = new CodeTypeDeclaration ("_" + Name);
		layout.IsStruct = true;
		layout.TypeAttributes = TypeAttributes.NotPublic;
		decl.Members.Add (layout);

		foreach (var f in Fields) {
			CodeMemberField field = new CodeMemberField { Name = f.Name, Type = g.CppTypeToCodeDomType (f.Type) };
			layout.Members.Add (field);
		}

		var iface = new CodeTypeDeclaration ("I" + Name);
		iface.IsInterface = true;
		layout.TypeAttributes = TypeAttributes.NotPublic;
		iface.BaseTypes.Add (new CodeTypeReference ("ICppClassOverridable", new CodeTypeReference [] { new CodeTypeReference (decl.Name) }));
		decl.Members.Add (iface);

		var layoutField = new CodeMemberField (new CodeTypeReference (typeof (Type)), "native_layout");
		layoutField.Attributes = MemberAttributes.Private|MemberAttributes.Static;
		layoutField.InitExpression = new CodeTypeOfExpression (layout.Name);
		decl.Members.Add (layoutField);

		var implField = new CodeMemberField (new CodeTypeReference (iface.Name), "impl");
		implField.Attributes = MemberAttributes.Private|MemberAttributes.Static;
		var getclass = new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (libDecl.Name), "Test"), "GetClass", new CodeTypeReference [] { new CodeTypeReference (iface.Name), new CodeTypeReference (layout.Name), new CodeTypeReference (decl.Name) });
		implField.InitExpression = new CodeMethodInvokeExpression (getclass, new CodeExpression [] { new CodePrimitiveExpression (Name) });
		decl.Members.Add (implField);
		//private static IClass impl = global::CppTests.Libs.Test.GetClass <IClass, _Class, Class>("Class");

		var ptrField = new CodeMemberField (new CodeTypeReference ("CppInstancePtr"), "native_ptr");
		ptrField.Attributes = MemberAttributes.Family;
		decl.Members.Add (ptrField);

		var allocCtor = new CodeConstructor () {
			};
		allocCtor.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference ("CppLibrary"), "dummy"));
		allocCtor.Statements.Add (new CodeAssignStatement (new CodeFieldReferenceExpression (null, "native_ptr"), new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), "Alloc"), new CodeExpression [] { new CodeThisReferenceExpression () })));
		decl.Members.Add (allocCtor);

		var subclassCtor = new CodeConstructor () {
			};
		subclassCtor.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference ("CppTypeInfo"), "subClass"));
		subclassCtor.Statements.Add (new CodeExpressionStatement (new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeArgumentReferenceExpression ("subClass"), "AddBase"), new CodeExpression [] { new CodeFieldReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), "TypeInfo") })));
		decl.Members.Add (subclassCtor);

		var nativeProperty = new CodeMemberProperty () {
				Name = "Native",
				Type = new CodeTypeReference ("CppInstancePtr"),
				Attributes = MemberAttributes.Public|MemberAttributes.Final
		};
		nativeProperty.GetStatements.Add (new CodeMethodReturnStatement (new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), "native_ptr")));
		decl.Members.Add (nativeProperty);

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
			decl.Members.Add (m.GenerateWrapperMethod (g));
		}

		return decl;
	}
}
