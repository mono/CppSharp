// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//   Zoltan Varga <vargaz@gmail.com>
//
// Copyright (C) 2011 Novell Inc.
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

using Mono.VisualC.Interop;
using Mono.VisualC.Code;
using Mono.VisualC.Code.Atoms;

namespace Mono.VisualC.Tools.Generator {

	public class Entry {

		public static Dictionary<string, Dictionary<string, Entry>> typelist;
                public static Dictionary<string, Entry> idlist;

		public string id;
		public string type;
		public string name;
		public string computedName;
		public string reftype;
		public Dictionary<string, string> attributes;
		public List<Entry> children;
		public bool isCreated;
		public CodeAtom atom;
		public Class Class {
			get { return (Class)atom; }
		}
		public CppType cppType;
		public bool isTemplate;

		List<Entry> members;


		public List<Entry> Members {
			get {
				if (members == null) {
					members = new List<Entry> ();
					if (HasValue ("members")) {
						var m = this["members"].ToString ().Split (' ').Where (id => !id.Equals (string.Empty)).ToArray ();
						members.AddRange (from o in m
							where idlist.ContainsKey (o)
							select idlist[o]);
					}
				}
				return members;
			}
		}

		public bool CheckValue (string key, string name)
		{
			return attributes.ContainsKey (key) && attributes[key] == name;
		}

		public string this[string key] {
			get { return HasValue (key) ? attributes[key] : null; }
		}

		public bool HasValue (string key)
		{
			return attributes.ContainsKey (key) && attributes[key] != "";
		}

		public bool IsTrue (string key)
		{
			return attributes.ContainsKey (key) && attributes[key] == "1";
		}

		public Entry Base {
			get {
				if (HasValue ("type"))
					return idlist[reftype];
				return this;
			}
		}

		public Entry Namespace {
			get {
				if (HasValue ("context"))
					return idlist[this["context"]].Namespace;
				return this;
			}
		}

		public static Entry Find (string name)
		{
			if (idlist.ContainsKey (name))
				return idlist[name];
			return (from o in idlist
				where o.Value.name == name
				select o.Value).FirstOrDefault ();
		}

		public override string ToString ()
		{
			return String.Format ("{0} - {1}", name, computedName);
		}
	}
}

