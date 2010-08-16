using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using System.CodeDom;

namespace Mono.VisualC.Code.Atoms {
	public class Enumeration : CodeAtom {

		public struct Item {
			public string Name;
			public int Value;
		}

		public string Name { get; set; }
		public IList<Item> Items { get; set; }

		public Enumeration (string name)
		{
			Name = name;
			Items = new List<Item> ();
		}

		internal protected override object InsideCodeNamespace (CodeNamespace ns)
		{
			ns.Types.Add (CreateEnumType ());
			return null;
		}

		internal protected override object InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			if (!decl.IsClass)
				return null;

			decl.Members.Add (CreateEnumType ());
			return null;
		}

		public CodeTypeDeclaration CreateEnumType ()
		{
			var type = new CodeTypeDeclaration (Name) {
				Attributes = MemberAttributes.Public,
				TypeAttributes = TypeAttributes.Public,
				IsEnum = true
			};

			foreach (Item i in Items)
				type.Members.Add (new CodeMemberField (typeof (int), i.Name) { InitExpression = new CodePrimitiveExpression (i.Value) });

			return type;
		}

		public override void Write (TextWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}

