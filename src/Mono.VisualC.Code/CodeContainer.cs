using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using System.CodeDom;
using System.CodeDom.Compiler;

using Mono.VisualC.Code.Atoms;

namespace Mono.VisualC.Code {

	public abstract class CodeContainer : CodeAtom {

		private LinkedList<CodeAtom> containedAtoms;
		public string IndentString {get; set;}
		public string Name { get; set; }

		public CodeContainer (string indentString)
		{
			containedAtoms = new LinkedList<CodeAtom> ();
			IndentString = indentString;
		}

		public CodeContainer() : this("\t")
		{
		}

		public virtual LinkedList<CodeAtom> Atoms {
			get { return containedAtoms; }
		}


		// Convenience method
		public virtual void AddToNamespace (string name, CodeAtom atom)
		{
			Namespace ns = Atoms.OfType<Namespace> ().Where (n => n.Name == name).SingleOrDefault ();
			if (ns == null) {
				ns = new Namespace (name);
				Atoms.AddLast (ns);
			}

			ns.Atoms.AddLast (atom);
		}

		public override void Write (TextWriter writer)
		{
			IndentedTextWriter itw = new IndentedTextWriter (writer, IndentString);
			itw.Indent = 1;
			foreach (CodeAtom atom in containedAtoms) {
				atom.Write (itw);
			}
		}

	}
}

