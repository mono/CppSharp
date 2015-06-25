using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Types;
using CppAbi = CppSharp.Parser.AST.CppAbi;
namespace CppSharp.Parser.Bootstrap
{
	class ASTGenerator : TextGenerator {

		public ASTGenerator(Driver driver, ASTContext ctx) {
			this.typePrinter = new CppTypePrinter (driver.TypeDatabase, false);
		}

		CppTypePrinter typePrinter{ get; set; }


		public void WriteExprClass(Class exprClass) {
			
			var name = exprClass.Name;
			var fields = exprClass.Fields;

			WriteClassStart ("public", exprClass);

			WriteFunctionStart ("public", name, fields); 
			//TODO: StatementClass
			fields.ForEach (f => WriteLine ("this.{0} = {1};", f.Name, f.Name));
			WriteFunctionEnd ();
			fields.ForEach (WriteAccessors);

			NewLine ();

			WriteFunctionStart("public override T", "Visit<T>", new[]{"IExpressionVisitor<T> visitor"});
			WriteLine ("return visitor.VisitExpression(this);");
			WriteFunctionEnd ();

			WriteClassEnd ();
		}

		public void WriteClassStart (string modifiers, Class exprClass) {
			WriteLine ("{0} class {1} : {2}", modifiers, exprClass.Name, exprClass.BaseClass.Name);
			WriteStartBraceIndent ();
		}

		public void WriteFunctionStart(string modifiers, string name, IEnumerable<Field> fields) {
			WriteFunctionStart(modifiers, name, fields.Select (f => typeOfField(f) + " " + f.Name));
		}

		public void WriteFunctionStart(string modifiers, string name, IEnumerable<String> args) {
			WriteLine ("{0} {1} ({2})", modifiers, name, String.Join (", ", args));
			WriteStartBraceIndent ();
		}

		public void WriteClassEnd () { 
			WriteCloseBraceIndent ();
			NewLine ();
		}

		public void WriteFunctionEnd () { 
			WriteCloseBraceIndent ();
			NewLine ();
		}

		public void WriteAccessors (Field field) {
			WriteLine ("public {0} {1} {{ get; set; }} ", typeOfField(field), field.Name);
		}

		public String typeOfField(Field f) {
			return f.Type.Visit(typePrinter);
		}
	}

			/*
			Class @class;
			f.Type.TryGetClass(out @class);
			return @class;
			*/
}
