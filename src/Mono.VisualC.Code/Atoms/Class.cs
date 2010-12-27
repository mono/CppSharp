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
			ns.Types.Add (CreateWrapperClass ());
			return null;
		}

		internal protected override object InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			if (!decl.IsClass)
				return null;

			decl.Members.Add (CreateWrapperClass ());
			return null;
		}

		public CodeTypeDeclaration CreateWrapperClass ()
		{
			var wrapper = new CodeTypeDeclaration (Name) {
				Attributes = MemberAttributes.Public,
				TypeAttributes = TypeAttributes.Public
			};
			foreach (var arg in TemplateArguments)
				wrapper.TypeParameters.Add (arg);

			if (Atoms.Count == 0) {
				wrapper.Comments.Add (new CodeCommentStatement ("FIXME: This type is a stub."));
				return wrapper;
			}

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
				var getClassMethod = new CodeMethodReferenceExpression (new CodeTypeReferenceExpression (new CodeTypeReference (StaticCppLibrary, CodeTypeReferenceOptions.GlobalReference)), "GetClass", types);
				implField.InitExpression = new CodeMethodInvokeExpression (getClassMethod, new CodePrimitiveExpression (Name));
			}
			wrapper.Members.Add (implField);

			// always add native subclass ctor
			wrapper.Members.Add (CreateNativeSubclassConstructor (hasOverrides));

			CodeMemberMethod dispose = null;
			foreach (var atom in Atoms) {
				Method method = atom as Method;
				if (method != null && method.IsDestructor) {
					dispose = (CodeMemberMethod)method.InsideCodeTypeDeclaration (wrapper);
					atom.Visit (dispose);
				} else
					atom.Visit (wrapper);
			}

			if (dispose == null) {
				dispose = CreateDestructorlessDispose ();
				wrapper.Members.Add (dispose);
			}

			if (hasOverrides)
				dispose.Attributes |= MemberAttributes.Override;

			return wrapper;
		}

		public CodeTypeDeclaration CreateInterface ()
		{
			return CreateInterface (null);
		}
		public CodeTypeDeclaration CreateInterface (CodeTypeDeclaration wrapper)
		{
			var iface = new CodeTypeDeclaration ("I" + Name) {
				TypeAttributes = TypeAttributes.Interface | (wrapper != null? TypeAttributes.NestedPublic : TypeAttributes.Public),
				Attributes = MemberAttributes.Public,
				IsInterface = true
			};

			foreach (var arg in TemplateArguments)
				iface.TypeParameters.Add (arg);

			if (wrapper != null)
				iface.BaseTypes.Add (new CodeTypeReference (typeof (ICppClassOverridable<>).Name, wrapper.TypeReference ()));
			else
				iface.BaseTypes.Add (new CodeTypeReference (typeof (ICppClass).Name));

			foreach (var atom in Atoms)
				atom.Visit (iface);

			return iface;
		}

		public CodeTypeDeclaration CreateNativeLayout ()
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

		private CodeConstructor CreateNativeSubclassConstructor (bool callBase)
		{
			var ctor = new CodeConstructor {
				Name = this.Name,
				Attributes = MemberAttributes.Assembly
			};
			ctor.Parameters.Add (new CodeParameterDeclarationExpression (typeof (CppTypeInfo).Name, "subClass"));

			// FIXME: Again, will this always work?
			var implTypeInfo = new CodeFieldReferenceExpression (new CodeFieldReferenceExpression { FieldName = "impl" }, "TypeInfo");

			if (callBase)
				ctor.BaseConstructorArgs.Add (implTypeInfo);

			var addBase = new CodeMethodInvokeExpression (new CodeArgumentReferenceExpression ("subClass"), "AddBase", implTypeInfo);
			ctor.Statements.Add (addBase);

			return ctor;
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

