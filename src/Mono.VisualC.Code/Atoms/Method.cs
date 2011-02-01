using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.CodeDom;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;
using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Code.Atoms {

	public class Method : CodeContainer {

		public Access Access  { get; set; }
		public bool IsVirtual { get; set; }
		public bool IsStatic  { get; set; }
		public bool IsConst   { get; set; }
		public bool IsConstructor { get; set; }
		public bool IsDestructor { get; set; }

		public CppType RetType { get; set; }

		public IList<NameTypePair<CppType>> Parameters { get; set; }
		public Class Klass { get; set; }

		private string formatted_name;

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
				var method = CreateWrapperMethod (decl.BaseTypes [0].BaseType != typeof (ICppObject).Name);

				if (method == null || CommentedOut)
					return null;

				if (Comment != null)
					method.Comments.Add (new CodeCommentStatement (Comment));

				decl.Members.Add (method);
				return method;

			} else if (decl.IsInterface) {
				CodeTypeMember method = CreateInterfaceMethod ();
				CodeTypeMember member;

				if (CommentedOut) {
					member = new CodeSnippetTypeMember ();
					member.Comments.Add (new CodeCommentStatement (method.CommentOut (current_code_provider)));

				} else
					member = method;

				if (Comment != null)
					member.Comments.Insert (0, new CodeCommentStatement (Comment));

				decl.Members.Add (member);
			}

			return null;
		}

		internal protected override object InsideCodeStatementCollection (CodeStatementCollection stmts)
		{
			List<CodeExpression> arguments = new List<CodeExpression> ();
			var native = new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), "Native");
			var native_ptr = new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), "native_ptr");

			if (!IsStatic)
				arguments.Add (native);

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


			if (IsConstructor) {
				var alloc = new CodeMethodInvokeExpression (impl, "Alloc", new CodeThisReferenceExpression ());
				stmts.Add (new CodeAssignStatement (native_ptr, alloc));
			}

			var invoke = new CodeMethodInvokeExpression (impl, Name, arguments.ToArray ());

			if (RetType.Equals (CppTypes.Void) || IsConstructor)
				stmts.Add (invoke);
			else
				stmts.Add (new CodeMethodReturnStatement (invoke));

			if (IsDestructor)
				stmts.Add (new CodeMethodInvokeExpression (native, "Dispose"));

			return null;
		}

		private CodeMemberMethod CreateInterfaceMethod ()
		{
			var returnType = ReturnTypeReference;
			var method = new CodeMemberMethod {
				Name = this.Name,
				ReturnType = returnType
			};

			if (returnType == null) {
				Comment = "FIXME: Unknown return type \"" + RetType.ToString () + "\" for method \"" + Name + "\"";;
				CommentedOut = true;
				method.ReturnType = new CodeTypeReference (typeof (void));
			}

			if (IsVirtual)	   method.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (VirtualAttribute).Name));
			if (IsConstructor) method.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (ConstructorAttribute).Name));
			if (IsDestructor)  method.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (DestructorAttribute).Name));
			if (IsConst)       method.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (ConstAttribute).Name));

			if (IsStatic)
				method.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (StaticAttribute).Name));
			else
				method.Parameters.Add (new CodeParameterDeclarationExpression (typeof (CppInstancePtr).Name, "this"));

			if (hasMangledInfo)
				method.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (AbiTestAttribute).Name,
				                                                           new CodeAttributeArgument (new CodePrimitiveExpression (Mangled.Name)),
				                                                           new CodeAttributeArgument ("Abi", new CodeTypeOfExpression (Mangled.Type))));
			for (int i = 0; i < Parameters.Count; i++) {
				var param = GenerateParameterDeclaration (Parameters [i]);
				string paramStr = Parameters [i].Type.ToString ();

				if (param == null) {
					Comment = "FIXME: Unknown parameter type \"" + paramStr + "\" to method \"" + Name + "\"";
					CommentedOut = true;
					method.Parameters.Add (new CodeParameterDeclarationExpression (paramStr, Parameters [i].Name));
					continue;
				}

				// FIXME: Only add MangleAs attribute if the managed type chosen would mangle differently by default
				if (!IsVirtual && !paramStr.Equals (string.Empty))
					param.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (MangleAsAttribute).Name, new CodeAttributeArgument (new CodePrimitiveExpression (paramStr))));

				method.Parameters.Add (param);
			}

			return method;
		}

		private CodeMemberMethod CreateWrapperMethod (bool hasBase)
		{
			CodeMemberMethod method;

			if (IsConstructor) {
				var ctor = new CodeConstructor {
					Name = FormattedName,
					Attributes = MemberAttributes.Public
				};
				if (hasBase)
					ctor.BaseConstructorArgs.Add (new CodeFieldReferenceExpression (new CodeFieldReferenceExpression { FieldName = "impl" }, "TypeInfo"));

				method = ctor;
			} else if (IsDestructor)
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
				method.CustomAttributes.Add (new CodeAttributeDeclaration (typeof (OverrideNativeAttribute).Name));

			for (int i = 0; i < Parameters.Count; i++) {
				var param = GenerateParameterDeclaration (Parameters [i]);
				if (param == null)
					return null;

				method.Parameters.Add (param);
			}

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

		private CodeParameterDeclarationExpression GenerateParameterDeclaration (NameTypePair<CppType> param)
		{
			CodeParameterDeclarationExpression paramDecl;

			if (param.Type.ElementType == CppTypes.Typename)
				paramDecl = new CodeParameterDeclarationExpression (param.Type.ElementTypeName, param.Name);

			else {
				Type managedType = param.Type.ToManagedType ();
				CodeTypeReference typeRef = param.Type.TypeReference ();

				if (managedType != null && managedType.IsByRef)
					paramDecl = new CodeParameterDeclarationExpression (managedType.GetElementType (), param.Name) { Direction = FieldDirection.Ref };

				else if (managedType != null && managedType != typeof (ICppObject))
					paramDecl = new CodeParameterDeclarationExpression (managedType, param.Name);

				else if (typeRef != null)
					paramDecl = new CodeParameterDeclarationExpression (typeRef, param.Name);

				else
					return null;

			}

			return paramDecl;
		}

		public string FormattedName {
			get {
				if (formatted_name == null) {
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
					formatted_name = sb.ToString ();
					if (formatted_name == Klass.Name)
						formatted_name += "1";
				}
				return formatted_name;
			}
			set {
				formatted_name = value;
			}
		}

		public override void Write (TextWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}

