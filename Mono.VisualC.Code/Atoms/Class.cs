using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using System.CodeDom;
using Mono.VisualC.Interop;

namespace Mono.VisualC.Code.Atoms {

	public class Class : CodeContainer {

		// FIXME: This should be moved into Mono.VisualC.Interop and an attribute
		//  for name mangling purposes (MSVC mangles differently depending on defined as class or struct).
		public enum Definition {
			Class,
			Struct
		}
		public struct BaseClass {
			public Class Class;
			public Access Access;
			public bool IsVirtual;
		}
		
		public string Name { get; set; }
		public string StaticCppLibrary { get; set; }

		public IEnumerable<BaseClass> Bases { get; set; }
		public Definition DefinedAs { get; set; }
		
		public Class (string name)
		{
			Name = name;
			Bases = Enumerable.Empty<BaseClass> ();
		}

		internal protected override CodeObject InsideCodeNamespace (CodeNamespace ns)
		{
			var wrapper = new CodeTypeDeclaration (Name) { TypeAttributes = TypeAttributes.Public };
			var iface = CreateInterface ();
			var native = CreateNativeLayout ();
			
			wrapper.Members.Add (iface);
			wrapper.Members.Add (native);

			// FIXME: For now, we'll have the managed wrapper extend from the first public base class
			string managedBase = Bases.Where (b => b.Access == Access.Public).Select (b => b.Class.Name).FirstOrDefault ();
			if (managedBase == null) {

				managedBase = typeof (ICppObject).Name;

				// Add Native property
				var nativeField = new CodeMemberField (typeof (CppInstancePtr), "native_ptr") { Attributes = MemberAttributes.Family };
				var nativeProperty = new CodeMemberProperty {
					Name = "Native",
					Type = new CodeTypeReference (typeof (CppInstancePtr)),
					HasSet = false,
					Attributes = MemberAttributes.Public
				};
				nativeProperty.GetStatements.Add (new CodeMethodReturnStatement (new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), nativeField.Name)));

				wrapper.Members.Add (nativeField);
				wrapper.Members.Add (nativeProperty);
			}
			wrapper.BaseTypes.Add (managedBase);

			// add static impl field
			var implField = new CodeMemberField (iface.Name, "impl") { Attributes = MemberAttributes.Static | MemberAttributes.Private };
			if (StaticCppLibrary != null) {
				CodeTypeReference [] types = new CodeTypeReference [] {
					new CodeTypeReference (iface.Name),
					new CodeTypeReference (native.Name),
					new CodeTypeReference (wrapper.Name)
				};
				var getClassMethod = new CodeMethodReferenceExpression (new CodeTypeReferenceExpression (StaticCppLibrary), "GetClass", types);
				implField.InitExpression = new CodeMethodInvokeExpression (getClassMethod, new CodePrimitiveExpression (Name));
			}
			wrapper.Members.Add (implField);

			foreach (var atom in Atoms)
				atom.Visit (wrapper);

			ns.Types.Add (wrapper);
			return wrapper;
		}

		private CodeTypeDeclaration CreateInterface ()
		{
			var iface = new CodeTypeDeclaration ("I" + Name) {
				TypeAttributes = TypeAttributes.Interface | TypeAttributes.NestedPublic,
				Attributes = MemberAttributes.Public,
				IsInterface = true
			};

			iface.BaseTypes.Add (new CodeTypeReference (typeof (ICppClassOverridable<>).Name, new CodeTypeReference (Name)));

			foreach (var atom in Atoms)
				atom.Visit (iface);

			return iface;
		}

		private CodeTypeDeclaration CreateNativeLayout ()
		{
			var native = new CodeTypeDeclaration ("_" + Name) {
				TypeAttributes = TypeAttributes.NestedPrivate | TypeAttributes.SequentialLayout,
				Attributes = MemberAttributes.Private,
				IsStruct = true
			};

			foreach (var atom in Atoms)
				atom.Visit (native);

			return native;
		}

		public override void Write (TextWriter writer)
		{
			string declarator = (DefinedAs == Definition.Class? "class" : "struct");
			writer.Write ("{0} {1}", declarator, Name);
			
			var bce = Bases.GetEnumerator ();
			
			if (bce.MoveNext ()) {
				writer.Write (" : ");
				
				while (true) {
					var baseClass = bce.Current;
					
					if (baseClass.IsVirtual) writer.Write ("virtual ");
					switch (baseClass.Access) {
					case Access.Public: writer.Write ("public "); break;
					case Access.Protected: writer.Write ("protected "); break;
					case Access.Private: writer.Write ("private "); break;
					}
					
					writer.Write (baseClass.Class.Name);
					
					if (!bce.MoveNext ())
						break;
					
					writer.Write (", ");
				}
			}
			
			writer.WriteLine (" {");
			base.Write (writer);
			writer.WriteLine ("};");
		}
	}
}

