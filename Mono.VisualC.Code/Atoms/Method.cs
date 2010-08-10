using System;
using System.IO;
using System.Collections.Generic;
using System.CodeDom;

using Mono.VisualC.Interop;

namespace Mono.VisualC.Code.Atoms {

	public class Method : CodeContainer {
		public struct Parameter {
			public string Name;
			public CppType Type;
		}

		public string Name { get; set; }
		public Access Access  { get; set; }
		public bool IsVirtual { get; set; }
		public bool IsStatic  { get; set; }

		public CppType RetType { get; set; }

		public IEnumerable<Parameter> Parameters { get; set; }
		
		public Method (string name)
		{
			Name = name;
		}

		internal protected override CodeObject InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			CodeMemberMethod method = null;

			if (decl.IsInterface) {
				method = new CodeMemberMethod {	Name = this.Name };
				Type managedType = RetType.ToManagedType ();

				if (managedType != null && managedType.IsByRef)
					method.ReturnType = new CodeTypeReference (typeof (IntPtr));
				else if (managedType != null && managedType != typeof (ICppObject))
					method.ReturnType = new CodeTypeReference (managedType);
				else
					method.ReturnType = new CodeTypeReference (RetType.ElementTypeName);

				if (IsVirtual)
					method.CustomAttributes.Add (new CodeAttributeDeclaration ("Virtual"));

				if (IsStatic)
					method.CustomAttributes.Add (new CodeAttributeDeclaration ("Static"));
				else
					method.Parameters.Add (new CodeParameterDeclarationExpression (typeof (CppInstancePtr).Name, "this"));

				foreach (var param in Parameters) {

					CodeParameterDeclarationExpression paramDecl;
					managedType = param.Type.ToManagedType ();

					// BOO work around bug in Codedom dealing with byref types!
					if (managedType != null && managedType.IsByRef)
						paramDecl = new CodeParameterDeclarationExpression (managedType.GetElementType (), param.Name) { Direction = FieldDirection.Ref };
					else if (managedType != null && managedType != typeof (ICppObject))
						paramDecl = new CodeParameterDeclarationExpression (managedType, param.Name);
					else
						paramDecl = new CodeParameterDeclarationExpression (param.Type.ElementTypeName, param.Name);

					// FIXME: Only add MangleAs attribute if the managed type chosen would mangle differently by default
					if (!IsVirtual)
						paramDecl.CustomAttributes.Add (new CodeAttributeDeclaration ("MangleAs", new CodeAttributeArgument (new CodePrimitiveExpression (param.Type.ToString ()))));

					method.Parameters.Add (paramDecl);
				}
			}

			// FIXME: add wrapper method
			if (method != null)
				decl.Members.Add (method);
			return method;
		}

		public override void Write (TextWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}

