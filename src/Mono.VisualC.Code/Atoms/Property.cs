using System;
using System.IO;
using System.CodeDom;

namespace Mono.VisualC.Code.Atoms {
	public class Property : CodeAtom {

		public string Name { get; set; }
		public Method Getter { get; set; }
		public Method Setter { get; set; }

		public Property (string name)
		{
			Name = name;
		}

		internal protected override object InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			// if getter is omitted, just output the setter like a method
			if (decl.IsInterface || Getter == null) {
				if (Getter != null)
					Getter.Visit (decl);
				if (Setter != null)
					Setter.Visit (decl);
				return null;
			} else if (!decl.IsClass)
				return null;

			var prop = new CodeMemberProperty {
				Name = this.Name,
				// FIXME: For now, no properties will be virtual... change this at some point?
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				Type = Getter.ReturnTypeReference
			};

			Getter.Visit (prop.GetStatements);

			if (Setter != null)
				Setter.Visit (prop.SetStatements);

			decl.Members.Add (prop);
			return prop;
		}

		public override void Write (TextWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}

