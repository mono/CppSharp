
using System;
using System.Reflection;
using System.Linq;
using System.IO;

using System.CodeDom;

namespace Mono.VisualC.Code.Atoms {

	// FIXME: support conditional compilation
	public class PoundDefine<T> : CodeAtom {
		public string Name {get; set;}
		public T Value {get; set;}
		private bool value_set = false;

		public PoundDefine(string name) {
			Name = name;
		}
		public PoundDefine (string name, T value) {
			Name = name;
			Value = value;
			value_set = true;
		}
		public override void Write (TextWriter writer) {
			if (value_set)
				writer.WriteLine ("#define {0} {1}", Name, Value);
			else
				writer.WriteLine ("#define {0}", Name);
		}

		internal protected override object InsideCodeNamespace (CodeNamespace ns)
		{
			if (!value_set)
				return null;

			// create a new type Constants if it does not exist
			CodeTypeDeclaration constants = (from type in ns.Types.Cast<CodeTypeDeclaration> ()
			                                 where type.Name.Equals ("Constants") select type).SingleOrDefault ();
			if (constants == null) {
				constants = new CodeTypeDeclaration ("Constants");
				constants.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				ns.Types.Add (constants);
			}

			return constants;
		}

		internal protected override object InsideCodeTypeDeclaration (CodeTypeDeclaration decl)
		{
			if (!value_set)
				return null;

			var constant = new CodeMemberField (typeof (T), Name);
			constant.Attributes = MemberAttributes.Public | MemberAttributes.Const;
			constant.InitExpression = new CodePrimitiveExpression (Value);
			decl.Members.Add (constant);

			return constant;
		}
	}

	public class PoundIfDef : CodeContainer {
		public enum Condition {
			Defined,
			NotDefined
		}
		public Condition IfCondition {get; set;}
		public string Name {get; set;}
		public PoundIfDef (Condition condition, string name) {
			IfCondition = condition;
			Name = name;
		}

		public override void Write (TextWriter writer) {
			string directive = (IfCondition == Condition.Defined ? "#ifdef {0}" : "#ifndef {0}");
			writer.WriteLine (directive, Name);
			base.Write (writer);
			writer.WriteLine ("#endif");
		}
	}
}
