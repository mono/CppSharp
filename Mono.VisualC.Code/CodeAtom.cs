using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using System.CodeDom;
using System.CodeDom.Compiler;

namespace Mono.VisualC.Code {

	public abstract class CodeAtom {

		public string Comment { get; set; }
		public bool CommentedOut { get; set; }

		protected CodeDomProvider current_code_provider;

		internal protected virtual void Visit (object obj)
		{
			Visit (obj, current_code_provider);
		}

		internal protected virtual void Visit (object obj, CodeDomProvider provider)
		{
			current_code_provider = provider;
			object result = obj;

			while (result != null) {
				if (result is CodeCompileUnit)         { result = InsideCodeCompileUnit (result as CodeCompileUnit); continue; }
				if (result is CodeNamespace)           { result = InsideCodeNamespace (result as CodeNamespace); continue; }
				if (result is CodeTypeDeclaration)     { result = InsideCodeTypeDeclaration (result as CodeTypeDeclaration); continue; }
				if (result is CodeMemberMethod)        { result = InsideCodeStatementCollection (((CodeMemberMethod)result).Statements); continue; }
				if (result is CodeStatementCollection) { result = InsideCodeStatementCollection (result as CodeStatementCollection); continue; }
				break;
			}

			current_code_provider = null;
		}

		internal protected virtual object InsideCodeCompileUnit (CodeCompileUnit ccu)
		{
			return null;
		}

		internal protected virtual object InsideCodeNamespace (CodeNamespace ns)
		{
			return null;
		}

		internal protected virtual object InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			return null;
		}

		internal protected virtual object InsideCodeStatementCollection (CodeStatementCollection stmts)
		{
			return null;
		}
		public abstract void Write (TextWriter writer);
	}
}

