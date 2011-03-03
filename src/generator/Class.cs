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
		decl.IsPartial = true;
		if (BaseClasses.Count > 0)
			decl.BaseTypes.Add (new CodeTypeReference (BaseClasses [0].Name));
		else
			decl.BaseTypes.Add (new CodeTypeReference ("ICppObject"));

		bool hasBase = BaseClasses.Count > 0;

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
		var getclass = new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (libDecl.Name), libFieldName), "GetClass", new CodeTypeReference [] { new CodeTypeReference (iface.Name), new CodeTypeReference (layout.Name), new CodeTypeReference (decl.Name) });
		implField.InitExpression = new CodeMethodInvokeExpression (getclass, new CodeExpression [] { new CodePrimitiveExpression (Name) });
		decl.Members.Add (implField);
		//private static IClass impl = global::CppTests.Libs.Test.GetClass <IClass, _Class, Class>("Class");

		if (!hasBase) {
			var ptrField = new CodeMemberField (new CodeTypeReference ("CppInstancePtr"), "native_ptr");
			ptrField.Attributes = MemberAttributes.Family;
			decl.Members.Add (ptrField);
		}

		var allocCtor = new CodeConstructor () {
			};
		allocCtor.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference ("CppLibrary"), "dummy"));
		allocCtor.Statements.Add (new CodeAssignStatement (new CodeFieldReferenceExpression (null, "native_ptr"), new CodeMethodInvokeExpression (new CodeMethodReferenceExpression (new CodeFieldReferenceExpression (null, "impl"), "Alloc"), new CodeExpression [] { new CodeThisReferenceExpression () })));
		if (hasBase) {
			var implTypeInfo = new CodeFieldReferenceExpression (new CodeFieldReferenceExpression { FieldName = "impl" }, "TypeInfo");
			allocCtor.BaseConstructorArgs.Add (implTypeInfo);
		}
		decl.Members.Add (allocCtor);

		var subclassCtor = new CodeConstructor () {
				Attributes = MemberAttributes.Family
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

		foreach (Property p in Properties) {
			decl.Members.Add (p.GenerateProperty (g));
		}

		return decl;
	}
}
