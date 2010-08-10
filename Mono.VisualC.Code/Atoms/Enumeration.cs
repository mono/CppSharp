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
			var type = new CodeTypeDeclaration (Name) {
				TypeAttributes = TypeAttributes.Public,
				IsEnum = true
			};

			foreach (Item i in Items)
				type.Members.Add (new CodeMemberField (typeof (int), i.Name) { InitExpression = new CodePrimitiveExpression (i.Value) });

			ns.Types.Add (type);
			return type;
		}

		public override void Write (TextWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}

