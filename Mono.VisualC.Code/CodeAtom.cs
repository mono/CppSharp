using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using System.CodeDom;

namespace Mono.VisualC.Code {

	public abstract class CodeAtom {

		internal protected virtual void Visit (CodeObject obj)
		{
			CodeObject result = obj;

			while (result != null) {
				if (result is CodeCompileUnit)      { result = InsideCodeCompileUnit (result as CodeCompileUnit); continue; }
				if (result is CodeNamespace)        { result = InsideCodeNamespace (result as CodeNamespace); continue; }
				if (result is CodeTypeDeclaration)  { result = InsideCodeTypeDeclaration (result as CodeTypeDeclaration); continue; }

				break;
			}
		}

		internal protected virtual CodeObject InsideCodeCompileUnit (CodeCompileUnit ccu)
		{
			return null;
		}

		internal protected virtual CodeObject InsideCodeNamespace (CodeNamespace ns)
		{
			return null;
		}

		internal protected virtual CodeObject InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			return null;
		}

		public abstract void Write (TextWriter writer);
	}
}

