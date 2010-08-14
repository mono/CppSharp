using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.CodeDom;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

namespace Mono.VisualC.Code.Atoms {

	public class Method : CodeContainer {

		public string Name { get; set; }
		public Access Access  { get; set; }
		public bool IsVirtual { get; set; }
		public bool IsStatic  { get; set; }
		public bool IsConst   { get; set; }
		public bool IsConstructor { get; set; }
		public bool IsDestructor { get; set; }

		public CppType RetType { get; set; }
		public IList<NameTypePair<CppType>> Parameters { get; set; }

		// for testing:
		// FIXME: make this Nullable, auto implemented property and remove bool field once this won't cause gmcs to crash
		public NameTypePair<Type> Mangled {
			get {
				if (!hasMangledInfo) throw new InvalidOperationException ("No mangle info present.");
				return mangled;
			}
			set {
				mangled = value;
				hasMangledInfo = true;
			}

		}
		private NameTypePair<Type> mangled;
		private bool hasMangledInfo = false;


		public Method (string name)
		{
			Name = name;
			Parameters = new List<NameTypePair<CppType>> ();
		}

		internal protected override object InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			if (decl.IsClass) {
				// FIXME: add commented methods to wrappers too? for now, I say let's reduce code clutter.
				if (CommentedOut)
					return null;

				var method = CreateWrapperMethod ();
				decl.Members.Add (method);
				return method;

			} else if (decl.IsInterface) {
				CodeTypeMember member;

				if (CommentedOut) {
					member = new CodeSnippetTypeMember ();
					member.Comments.Add (new CodeCommentStatement (Comment));
					member.Comments.Add (new CodeCommentStatement (CreateInterfaceMethod ().CommentOut (current_code_provider)));

				} else
					member = CreateInterfaceMethod ();

				decl.Members.Add (member);
			}

			return null;
		}

		internal protected override object InsideCodeStatementCollection (CodeStatementCollection stmts)
		{
			List<CodeExpression> arguments = new List<CodeExpression> ();
			if (!IsStatic)
				arguments.Add (new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), "Native"));

			foreach (var param in Parameters) {
				// FIXME: handle typenames better
				if (param.Type.ElementType != CppTypes.Typename) {
					Type managedType = param.Type.ToManagedType ();
					if (managedType != null && managedType.IsByRef) {
						arguments.Add (new CodeDirectionExpression (FieldDirection.Ref, new CodeArgumentReferenceExpression (param.Name)));
						continue;
					}
				}
				arguments.Add (new CodeArgumentReferenceExpression (param.Name));
			}


			// FIXME: Does just specifying the field name work for all code generators?
			var impl = new CodeFieldReferenceExpression { FieldName = "impl" };
			var invoke = new CodeMethodInvokeExpression (impl, Name, arguments.ToArray ());

			if (RetType.Equals (CppTypes.Void))
				stmts.Add (invoke);
			else
				stmts.Add (new CodeMethodReturnStatement (invoke));

			return null;
		}

		private CodeMemberMethod CreateInterfaceMethod ()
		{
			var method = new CodeMemberMethod {
				Name = this.Name,
				ReturnType = this.ReturnTypeReference
			};

			if (IsVirtual)	   method.CustomAttributes.Add (new CodeAttributeDeclaration ("Virtual"));
			if (IsConstructor) method.CustomAttributes.Add (new CodeAttributeDeclaration ("Constructor"));
			if (IsDestructor)  method.CustomAttributes.Add (new CodeAttributeDeclaration ("Destructor"));
			if (IsConst)       method.CustomAttributes.Add (new CodeAttributeDeclaration ("Const"));

			if (IsStatic)
				method.CustomAttributes.Add (new CodeAttributeDeclaration ("Static"));
			else
				method.Parameters.Add (new CodeParameterDeclarationExpression (typeof (CppInstancePtr).Name, "this"));

			if (hasMangledInfo)
				method.CustomAttributes.Add (new CodeAttributeDeclaration ("ValidateBindings",
				                                                           new CodeAttributeArgument (new CodePrimitiveExpression (Mangled.Name)),
				                                                           new CodeAttributeArgument ("Abi", new CodeTypeOfExpression (Mangled.Type))));
			foreach (var param in GetParameterDeclarations (true))
				method.Parameters.Add (param);

			return method;
		}

		private CodeMemberMethod CreateWrapperMethod ()
		{
			CodeMemberMethod method;

			if (IsConstructor)
				method = new CodeConstructor {
					Name = FormattedName,
					Attributes = MemberAttributes.Public
				};
			else if (IsDestructor)
				method = new CodeMemberMethod {
					Name = "Dispose",
					Attributes = MemberAttributes.Public
				};
			else
				method = new CodeMemberMethod {
					Name = FormattedName,
					Attributes = MemberAttributes.Public,
					ReturnType = ReturnTypeReference
				};

			if (IsStatic)
				method.Attributes |= MemberAttributes.Static;
			else if (!IsVirtual && !IsDestructor)
				// I'm only making methods that are virtual in C++ virtual in managed code.
				//  I think it is the right thing to do because the consumer of the API might not
				//  get the intended effect if the managed method is overridden and the native method is not.
				method.Attributes |= MemberAttributes.Final;
			else if (IsVirtual && !IsDestructor)
				method.CustomAttributes.Add (new CodeAttributeDeclaration ("OverrideNative"));

			foreach (var param in GetParameterDeclarations (false))
				method.Parameters.Add (param);

			return method;
		}

		public CodeTypeReference ReturnTypeReference {
			get {
				CodeTypeReference returnType;
	
				if (RetType.ElementType == CppTypes.Typename)
					returnType = new CodeTypeReference (RetType.ElementTypeName, CodeTypeReferenceOptions.GenericTypeParameter);
				else {
					Type managedType = RetType.ToManagedType ();
					if (managedType != null && managedType.IsByRef)
						returnType = new CodeTypeReference (typeof (IntPtr));
					else if (managedType != null && managedType != typeof (ICppObject))
						returnType = new CodeTypeReference (managedType);
					else
						returnType = RetType.TypeReference ();
				}

				return returnType;
			}
		}

		public IEnumerable<CodeParameterDeclarationExpression> GetParameterDeclarations ()
		{
			return GetParameterDeclarations (false);
		}

		private IEnumerable<CodeParameterDeclarationExpression> GetParameterDeclarations (bool includeMangleAttribute)
		{
			foreach (var param in Parameters) {
				CodeParameterDeclarationExpression paramDecl;

				if (param.Type.ElementType == CppTypes.Typename)
					paramDecl = new CodeParameterDeclarationExpression (param.Type.ElementTypeName, param.Name);
				else {
					Type managedType = param.Type.ToManagedType ();
					if (managedType != null && managedType.IsByRef)
						paramDecl = new CodeParameterDeclarationExpression (managedType.GetElementType (), param.Name) { Direction = FieldDirection.Ref };
					else if (managedType != null && managedType != typeof (ICppObject))
						paramDecl = new CodeParameterDeclarationExpression (managedType, param.Name);
					else
						paramDecl = new CodeParameterDeclarationExpression (param.Type.TypeReference (), param.Name);
				}

				// FIXME: Only add MangleAs attribute if the managed type chosen would mangle differently by default
				string paramStr = param.Type.ToString ();
				if (includeMangleAttribute && !IsVirtual && !paramStr.Equals (string.Empty))
					paramDecl.CustomAttributes.Add (new CodeAttributeDeclaration ("MangleAs", new CodeAttributeArgument (new CodePrimitiveExpression (paramStr))));

				yield return paramDecl;
			}
		}

		public string FormattedName {
			get {
				string upper = Name.ToUpper ();
				StringBuilder sb = new StringBuilder (Name.Length);
	
				for (int i = 0; i < Name.Length; i++) {
					if (i == 0)
						sb.Append (upper [0]);
					else if (Name [i] == '_')
						sb.Append (upper [++i]);
					else
						sb.Append (Name [i]);
				}
				return sb.ToString ();
			}
		}

		public override void Write (TextWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}

