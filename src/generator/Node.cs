//
// Node.cs:
//
//
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


//
// This class represents an XML node read from the output of gccxml
//
class Node {

	// The XML element type
	public string Type {
		get; set;
	}

	// The value of the 'id' attribute
	public string Id {
		get; set;
	}

	// The value of the 'name' attribute or null
	public string Name {
		get; set;
	}

	// Attributes
	public Dictionary<string, string> Attributes {
		get; set;
	}

	// The children nodes of this node
	public List<Node> Children {
		get; set;
	}

	public string this [string key] {
		get {
			return Attributes [key];
		}
	}

	// Maps ids to nodes
	public static Dictionary<string, Node> IdToNode = new Dictionary <string, Node> ();

	// For an attribute which contains an id, return the corresponding Node
	public Node NodeForAttr (string attr) {
		return IdToNode [Attributes [attr]];
	}

	public bool IsTrue (string key) {
		return Attributes.ContainsKey (key) && Attributes[key] == "1";
	}

	public bool HasValue (string key) {
		return Attributes.ContainsKey (key) && Attributes[key] != "";
	}

	public bool CheckValue (string key, string name) {
		return Attributes.ContainsKey (key) && Attributes[key].Trim () == name.Trim ();
	}

	public bool CheckValueList (string key, string name) {
		return Attributes.ContainsKey (key) && Attributes[key].Split (' ').Contains (name.Trim ());
	}
}
