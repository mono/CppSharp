using System;
using System.IO;
using System.CodeDom;

namespace Mono.VisualC.Code.Atoms {
	public class Namespace : CodeContainer {

		public Namespace (string name)
		{
			Name = name;
		}

		internal protected override object InsideCodeCompileUnit (CodeCompileUnit ccu)
		{
			CreateNamespace (ccu, Name);
			return null;
		}

		internal protected override object InsideCodeNamespace (CodeNamespace ns)
		{
			CodeCompileUnit ccu = ns.UserData ["CodeCompileUnit"] as CodeCompileUnit;
			if (ccu == null)
				throw new NotSupportedException ("Invalid CodeNamespace");

			CreateNamespace (ccu, ns.Name + "." + Name);
			return null;
		}

		private void CreateNamespace (CodeCompileUnit ccu, string name)
		{
			CodeNamespace ns = new CodeNamespace (name);
			ns.Imports.Add (new CodeNamespaceImport ("Mono.VisualC.Interop"));

			ns.UserData ["CodeCompileUnit"] = ccu;

			foreach (var atom in Atoms)
				atom.Visit (ns);

			ccu.Namespaces.Add (ns);
		}

		public override void Write (TextWriter writer)
		{
			writer.WriteLine ("namespace {0} {{", Name);
			base.Write (writer);
			writer.WriteLine ("}");
		}
	}
}

