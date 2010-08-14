using System;
using System.IO;
using System.Linq;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;

using Mono.VisualC.Interop;

namespace Mono.VisualC.Code {

	internal delegate void CodeGenMethod<T> (T codeObject, TextWriter writer, CodeGeneratorOptions cgo);
	public static class CodeDomExtensions {

		public static CodeComment CommentOut (this CodeTypeMember code, CodeDomProvider provider)
		{
			// FIXME: Not implemented ini mono
			//return CommentOut (provider.GenerateCodeFromMember, code);
			return new CodeComment ();
		}
		public static CodeComment CommentOut (this CodeStatement code, CodeDomProvider provider)
		{
			return CommentOut (provider.GenerateCodeFromStatement, code);
		}
		public static CodeComment CommentOut (this CodeExpression code, CodeDomProvider provider)
		{
			return CommentOut (provider.GenerateCodeFromExpression, code);
		}

		private static CodeComment CommentOut<T> (CodeGenMethod<T> method, T codeObject)
		{
			StringWriter output = new StringWriter ();
			CodeGeneratorOptions opts = new CodeGeneratorOptions ();

			method (codeObject, output, opts);

			return new CodeComment (output.ToString ());
		}

		public static CodeTypeReference TypeReference (this CodeTypeDeclaration ctd)
		{
			return new CodeTypeReference (ctd.Name, ctd.TypeParameterReferences ());
		}

		public static CodeTypeReference TypeReference (this CppType t)
		{
			return t.TypeReference (false);
		}

		public static CodeTypeReference TypeReference (this CppType t, bool useManagedType)
		{
			var tempParm = from m in t.Modifiers.OfType<CppModifiers.TemplateModifier> ()
			               from p in m.Types
			               select p.TypeReference (true);

			Type managedType = useManagedType? t.ToManagedType () : null;
			if (managedType == typeof (ICppObject))
				managedType = null;

			return new CodeTypeReference (managedType != null ? managedType.FullName : t.ElementTypeName, tempParm.ToArray ());
		}

		public static CodeTypeReference [] TypeParameterReferences (this CodeTypeDeclaration ctd)
		{
			return ctd.TypeParameters.Cast<CodeTypeParameter> ().Select (p => new CodeTypeReference (p)).ToArray ();
		}

	}
}

