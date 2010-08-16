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

		// FIXME: Handle fixed size arrays? Can't really see a good way to do that yet.
		public CodeTypeReference TypeReference {
			get {

				if (Type.Modifiers.Count > 0) {
					CppModifiers lastModifier = Type.Modifiers [Type.Modifiers.Count - 1];
					if (lastModifier == CppModifiers.Pointer || lastModifier == CppModifiers.Reference)
						return new CodeTypeReference (typeof (IntPtr));
				}

				if (Type.ElementType == CppTypes.Enum || Type.ElementType == CppTypes.Union)
					return Type.TypeReference ();

				if (Type.ElementType == CppTypes.Typename)
					return new CodeTypeReference (Type.ElementTypeName, CodeTypeReferenceOptions.GenericTypeParameter);

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

