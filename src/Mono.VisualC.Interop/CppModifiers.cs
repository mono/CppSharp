//
// Mono.VisualC.Interop.CppModifiers.cs: Abstracts a C++ type modifiers
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010 Alexander Corrado
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop {

	public abstract class CppModifiers {
		#pragma warning disable 0414
		static int tmp;
		#pragma warning restore
		// This can be added to at runtime to support other modifiers
		// The list should be prioritized, in that the first items should be modifiers that can potentially contain other modifiers
		public static readonly Dictionary<string,Action<Match,List<CppModifiers>>> Tokenize = new Dictionary<string,Action<Match,List<CppModifiers>>> () {
			{ "\\<(.*)\\>", (m,l) => l.AddFirst (m.Groups [1].Success && m.Groups [1].Value.Trim () != ""? new TemplateModifier (m.Groups [1].Value) : CppModifiers.Template) },
			{ "\\[([^\\]]*)\\]", (m,l) => l.Add (m.Groups [1].Success && m.Groups [1].Value.Trim () != "" && int.TryParse (m.Groups [1].Value, out tmp) ? new ArrayModifier (int.Parse (m.Groups [1].Value)) : CppModifiers.Array) },
			{ "\\bconst\\b", (m,l) => l.Add (CppModifiers.Const) },
			{ "\\*", (m,l) => l.Add (CppModifiers.Pointer) },
			{ "\\&", (m,l) => l.Add (CppModifiers.Reference) },
			{ "\\bvolatile\\b", (m,l) => l.Add (CppModifiers.Volatile) },
			{ "\\bunsigned\\b", (m,l) => l.Add (CppModifiers.Unsigned) },
			{ "\\bsigned\\b", (m,l) => l.Add (CppModifiers.Signed) },
			{ "\\bshort\\b", (m,l) => l.Add (CppModifiers.Short) },
			{ "\\blong\\b", (m,l) => l.Add (CppModifiers.Long) }
		};

		private struct Token {
			public Action<Match, List<CppModifiers>> action;
			public Match match;
		}
		private static IEnumerable<Token> Tokenizer (string input) {

			foreach (var token in Tokenize) {
				Match match;

				while ((match = Regex.Match (input, token.Key)) != null && match.Success) {
					yield return new Token { match = match, action = token.Value };
					input = input.Remove (match.Index, match.Length);
				}
			}

		}

		public static List<CppModifiers> Parse (string input)
		{
			List<CppModifiers> cpm = new List<CppModifiers> ();
			var tokenizer = Tokenizer (input);

			foreach (var token in tokenizer.OrderBy (t => t.match.Index))
				token.action (token.match, cpm);

			return cpm;
		}

		// removes any modifiers from the passed input
		public static string Remove (string input)
		{
			foreach (var token in Tokenize)
				input = Regex.Replace (input, token.Key, "");

			return input;
		}

		// normalizes the order of order-agnostic modifiers
		public static IEnumerable<CppModifiers> NormalizeOrder (IEnumerable<CppModifiers> modifiers)
		{
			var parts = modifiers.Transform (
			        For.AllInputsIn (CppModifiers.Unsigned, CppModifiers.Long).InAnyOrder ().Emit (new CppModifiers [] { CppModifiers.Unsigned, CppModifiers.Long }),
			        For.AllInputsIn (CppModifiers.Signed, CppModifiers.Long).InAnyOrder ().Emit (new CppModifiers [] { CppModifiers.Signed, CppModifiers.Long }),
			        For.AllInputsIn (CppModifiers.Unsigned, CppModifiers.Short).InAnyOrder ().Emit (new CppModifiers [] { CppModifiers.Unsigned, CppModifiers.Short }),
			        For.AllInputsIn (CppModifiers.Signed, CppModifiers.Short).InAnyOrder ().Emit (new CppModifiers [] { CppModifiers.Signed, CppModifiers.Short }),

			        For.UnmatchedInput<CppModifiers> ().Emit (cppmod => new CppModifiers [] { cppmod })
			);

			foreach (var array in parts)
				foreach (var item in array)
					yield return item;
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

