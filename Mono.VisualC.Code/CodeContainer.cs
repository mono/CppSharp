using System;
using System.IO;
using System.Collections.Generic;

using System.CodeDom;
using System.CodeDom.Compiler;

namespace Mono.VisualC.Code {

	public abstract class CodeContainer : CodeAtom {

		private LinkedList<CodeAtom> containedAtoms;
		public string IndentString {get; set;}

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

