using System;
using System.IO;
using System.CodeDom;

using Mono.VisualC.Interop;

namespace Mono.VisualC.Code.Atoms {
	public class Field : CodeAtom {

		public string  Name { get; set; }
		public CppType Type { get; set; }

		public Field (string name, CppType type)
		{
			Name = name;
			Type = type;
		}

		internal protected override object InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			// FIXME: eventually this could add CppFields to the interface so they could be
			//  so they could be accessed as properties in the managed api
			if (!decl.IsStruct)
				return null;

			CodeMemberField field = new CodeMemberField { Name = this.Name };
			CodeTypeReference typeRef = TypeReference;

			if (typeRef == null) {
				field.Comments.Add (new CodeCommentStatement ("FIXME: Unknown type \"" + Type.ToString () + "\" for field \"" + Name + ".\" Assuming IntPtr."));
				field.Type = new CodeTypeReference (typeof (IntPtr));
			} else
				field.Type = typeRef;

			decl.Members.Add (field);
			return field;
		}

		public CodeTypeReference TypeReference {
			get {
				if (Type.ElementType == CppTypes.Typename)
					return new CodeTypeReference (Type.ElementTypeName, CodeTypeReferenceOptions.GenericTypeParameter);

				if (Type.Modifiers.Contains (CppModifiers.Pointer) || Type.Modifiers.Contains (CppModifiers.Reference))
					return new CodeTypeReference (typeof (IntPtr));

				// FIXME: Handle arrays (fixed size?)

				Type managedType = Type.ToManagedType ();
				if (managedType != null)
					return new CodeTypeReference (managedType);

				return null;
			}
		}

		public override void Write (TextWriter writer)
		{
			writer.WriteLine ("{0} {1};", Type.ToString (), Name);
		}
	}
}

