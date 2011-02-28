//
// Node.cs:
//
//
using System;
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
		return Attributes.ContainsKey (key) && Attributes[key] == name;
	}
}
