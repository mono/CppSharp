using System;
using System.Linq;
using System.Text;
using System.CodeDom;

using Mono.VisualC.Interop;

namespace Mono.VisualC.Code {

	public static class CodeDomExtensions {

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

			return new CodeTypeReference (managedType.FullName ?? t.ElementTypeName, tempParm.ToArray ());
		}

		public static CodeTypeReference [] TypeParameterReferences (this CodeTypeDeclaration ctd)
		{
			return ctd.TypeParameters.Cast<CodeTypeParameter> ().Select (p => new CodeTypeReference (p)).ToArray ();
		}

	}
}

