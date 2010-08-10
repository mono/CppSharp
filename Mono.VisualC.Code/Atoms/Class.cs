using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using System.CodeDom;
using Mono.VisualC.Interop;
using Mono.VisualC.Code;

namespace Mono.VisualC.Code.Atoms {

	public class Class : CodeContainer {

		// FIXME: This should be moved into Mono.VisualC.Interop and an attribute
		//  for name mangling purposes (MSVC mangles differently depending on defined as class or struct).
		public enum Definition {
			Class,
			Struct
		}
		public struct BaseClass {
			public string Name;
			public Access Access;
			public bool IsVirtual;
		}
		
		public string Name { get; set; }
		public string StaticCppLibrary { get; set; }
		public Definition DefinedAs { get; set; }

		public IList<BaseClass> Bases { get; set; }
		public IList<string> TemplateArguments { get; set; }

		public Class (string name)
		{
			Name = name;
			Bases = new List<BaseClass> ();
			TemplateArguments = new List<string> ();
		}

		internal protected override object InsideCodeNamespace (CodeNamespace ns)
		{
			var wrapper = CreateWrapperClass ();
			ns.Types.Add (wrapper);
			return wrapper;
		}

		private CodeTypeDeclaration CreateWrapperClass ()
		{
			var wrapper = new CodeTypeDeclaration (Name) { TypeAttributes = TypeAttributes.Public };
			foreach (var arg in TemplateArguments)
				wrapper.TypeParameters.Add (arg);

			var iface = CreateInterface (wrapper);
			wrapper.Members.Add (iface);

			var native = CreateNativeLayout ();
			wrapper.Members.Add (native);

			// FIXME: For now, we'll have the managed wrapper extend from the first public base class
			string managedBase = Bases.Where (b => b.Access == Access.Public).Select (b => b.Name).FirstOrDefault ();
			bool hasOverrides = true;

			if (managedBase == null) {
				managedBase = typeof (ICppObject).Name;
				hasOverrides = false;

				// Add Native property
				var nativeField = new CodeMemberField (typeof (CppInstancePtr).Name, "native_ptr") { Attributes = MemberAttributes.Family };
				var nativeProperty = new CodeMemberProperty {
					Name = "Native",
					Type = new CodeTypeReference (typeof (CppInstancePtr).Name),
					HasSet = false,
					Attributes = MemberAttributes.Public | MemberAttributes.Final
				};
				nativeProperty.GetStatements.Add (new CodeMethodReturnStatement (new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), nativeField.Name)));

				wrapper.Members.Add (nativeField);
				wrapper.Members.Add (nativeProperty);
			}
			wrapper.BaseTypes.Add (managedBase);

			// add static impl field
			var implField = new CodeMemberField (iface.TypeReference (), "impl") { Attributes = MemberAttributes.Static | MemberAttributes.Private };
			if (StaticCppLibrary != null) {
				CodeTypeReference [] types = new CodeTypeReference [] {
					iface.TypeReference (),
					native.TypeReference (),
					wrapper.TypeReference ()
				};
				var getClassMethod = new CodeMethodReferenceExpression (new CodeTypeReferenceExpression (StaticCppLibrary), "GetClass", types);
				implField.InitExpression = new CodeMethodInvokeExpression (getClassMethod, new CodePrimitiveExpression (Name));
			}
			wrapper.Members.Add (implField);

			CodeMemberMethod dispose = null;
			foreach (var atom in Atoms) {
				Method method = atom as Method;
				if (method != null && method.IsDestructor)
					dispose = (CodeMemberMethod)method.InsideCodeTypeDeclaration (wrapper);
				else
					atom.Visit (wrapper);
			}

			if (dispose == null)
				wrapper.Members.Add (CreateDestructorlessDispose ());
			else if (hasOverrides)
				dispose.Attributes |= MemberAttributes.Override;

			return wrapper;
		}

		private CodeTypeDeclaration CreateInterface (CodeTypeDeclaration wrapper)
		{
			var iface = new CodeTypeDeclaration ("I" + Name) {
				TypeAttributes = TypeAttributes.Interface | TypeAttributes.NestedPublic,
				Attributes = MemberAttributes.Public,
				IsInterface = true
			};

			foreach (var arg in TemplateArguments)
				iface.TypeParameters.Add (arg);

			iface.BaseTypes.Add (new CodeTypeReference (typeof (ICppClassOverridable<>).Name, wrapper.TypeReference ()));

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

		private CodeMemberMethod CreateDestructorlessDispose ()
		{
			var dispose = new CodeMemberMethod {
				Name = "Dispose",
				Attributes = MemberAttributes.Public
			};

			var warning = new CodeCommentStatement ("FIXME: Check for inline destructor for this class.");
			dispose.Statements.Add (warning);

			var native = new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), "Native");
			dispose.Statements.Add (new CodeMethodInvokeExpression (native, "Dispose"));

			return dispose;
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
					
					writer.Write (baseClass.Name);
					
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

