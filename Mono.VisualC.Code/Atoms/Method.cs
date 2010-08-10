using System;
using System.IO;
using System.Collections.Generic;
using System.CodeDom;

using Mono.VisualC.Interop;

namespace Mono.VisualC.Code.Atoms {

	public class Method : CodeContainer {
		public struct Parameter {
			public string Name;
			public string Type;
		}

		public string Name { get; set; }
		public Access Access  { get; set; }
		public bool IsVirtual { get; set; }
		public bool IsStatic  { get; set; }
		
		public string RetType { get; set; }

		public IEnumerable<Parameter> Parameters { get; set; }
		
		public Method (string name)
		{
			Name = name;
		}

		internal protected override CodeObject InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			CodeMemberMethod method = null;

			if (decl.IsInterface) {
				method = new CodeMemberMethod ();
				method.Name = Name;
				Type managedReturn = new CppType (RetType).ToManagedType ();
				if (managedReturn != null)
					method.ReturnType = new CodeTypeReference (managedReturn);
				else
					method.ReturnType = new CodeTypeReference (RetType);

				if (IsVirtual)
					method.CustomAttributes.Add (new CodeAttributeDeclaration ("Virtual"));

				if (IsStatic)
					method.CustomAttributes.Add (new CodeAttributeDeclaration ("Static"));
				else
					method.Parameters.Add (new CodeParameterDeclarationExpression (typeof (CppInstancePtr), "this"));

				foreach (var param in Parameters) {
					CodeParameterDeclarationExpression paramDecl;
					Type managedType = new CppType (param.Type).ToManagedType ();
					if (managedType != null)
						paramDecl = new CodeParameterDeclarationExpression (managedType, param.Name);
					else
						paramDecl = new CodeParameterDeclarationExpression (param.Type, param.Name);

					if (!IsVirtual)
						paramDecl.CustomAttributes.Add (new CodeAttributeDeclaration ("MangleAs", new CodeAttributeArgument (new CodePrimitiveExpression (param.Type))));

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

