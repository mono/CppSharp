//
// Mono.VisualC.Interop.CppModifiers.cs: Abstracts a C++ type modifiers
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mono.VisualC.Interop {

	public abstract class CppModifiers {

		// This can be added to at runtime to support other modifiers
		public static readonly Dictionary<string,Func<Match,CppModifiers>> Tokenize = new Dictionary<string,Func<Match,CppModifiers>> () {
			{ "\\bconst\\b", m => CppModifiers.Const },
			{ "\\*", m => CppModifiers.Pointer },
			{ "\\[([^\\]]*)\\]", m => m.Groups [1].Success && m.Groups [1].Value.Trim () != ""? new ArrayModifier (int.Parse (m.Groups [1].Value)) : CppModifiers.Array },
			{ "\\&", m => CppModifiers.Reference },
			{ "\\bvolatile\\b", m => CppModifiers.Volatile },
			{ "\\bsigned\\b", m => CppModifiers.Signed },
			{ "\\bunsigned\\b", m => CppModifiers.Unsigned },
			{ "\\bshort\\b", m => CppModifiers.Short },
			{ "\\blong\\b", m => CppModifiers.Long },
			{ "\\<(.*)\\>", m => m.Groups [1].Success && m.Groups [1].Value.Trim () != ""? new TemplateModifier (m.Groups [1].Value) : CppModifiers.Template }
		};

		public static IEnumerable<CppModifiers> Parse (string input)
		{
			foreach (var token in Tokenize) {
				foreach (Match match in Regex.Matches (input, token.Key))
					yield return token.Value (match);
			}
		}
		public static string Remove (string input)
		{
			foreach (var token in Tokenize)
				input = Regex.Replace (input, token.Key, "");

			return input;
		}

		public override bool Equals (object obj)
		{
			return this == obj as CppModifiers;
		}
		public override int GetHashCode ()
		{
			return GetType ().GetHashCode ();
		}

		public static bool operator == (CppModifiers a, CppModifiers b)
		{
			if ((object)a == (object)b)
				return true;

			if ((object)a == null || (object)b == null)
				return false;

			return a.GetHashCode () == b.GetHashCode ();
		}
		public static bool operator != (CppModifiers a, CppModifiers b)
		{
			return !(a == b);
		}

		public static readonly CppModifiers Const = new ConstModifier ();
		public static readonly CppModifiers Pointer = new PointerModifier ();
		public static readonly CppModifiers Array = new ArrayModifier ();
		public static readonly CppModifiers Reference = new ReferenceModifier ();
		public static readonly CppModifiers Volatile = new VolatileModifier ();
		public static readonly CppModifiers Signed = new SignedModifier ();
		public static readonly CppModifiers Unsigned = new UnsignedModifier ();
		public static readonly CppModifiers Short = new ShortModifier ();
		public static readonly CppModifiers Long = new LongModifier ();
		public static readonly CppModifiers Template = new TemplateModifier ();

		// Add list of modifiers here:
		public class ConstModifier : CppModifiers { public override string ToString () { return "const"; } }
		public class PointerModifier : CppModifiers { public override string ToString () { return "*"; } }
		public class ReferenceModifier : CppModifiers { public override string ToString () { return "&"; } }
		public class VolatileModifier : CppModifiers { public override string ToString () { return "volatile"; } }
		public class SignedModifier : CppModifiers { public override string ToString () { return "signed"; } }
		public class UnsignedModifier : CppModifiers { public override string ToString () { return "unsigned"; } }
		public class ShortModifier : CppModifiers { public override string ToString () { return "short"; } }
		public class LongModifier : CppModifiers { public override string ToString () { return "long"; } }

		public class ArrayModifier : CppModifiers {
			public int? Size { get; set; }

			public ArrayModifier ()
			{
			}

			public ArrayModifier (int size) {
				Size = size;
			}

			public override string ToString ()
			{
				return string.Format ("[{0}]", Size.HasValue? Size.ToString () : "");
			}
		}

		public class TemplateModifier : CppModifiers {
			public CppType [] Types { get; set; }

			public TemplateModifier ()
			{
			}

			public TemplateModifier (string types)
			{
				Types = Regex.Split (types, "(?<!\\<[^\\>]*),").Select (p => new CppType (p)).ToArray ();
			}

			public TemplateModifier (CppType [] types)
			{
				Types = types;
			}

			public override string ToString ()
			{
				return string.Format ("<{0}>", Types == null? "" : string.Join (", ", Types.Select (t => t.ToString ()).ToArray ()));
			}
		}
	}
}

