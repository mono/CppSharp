using System;
using System.Collections.Generic;

public class Namespace {

	public Namespace (string name)
	{
		this.Name = name;
	}

	public Namespace (Node node)
		: this (node.Name)
	{
		this.Node = node;
	}

	public Node Node {
		get; set;
	}

	// Back ref to enclosing namespace (may be null)
	public Namespace ParentNamespace {
		get; set;
	}

	public string Name {
		get; set;
	}

	public string FullyQualifiedName {
		get {
			return ParentNamespace != null? ParentNamespace.FullyQualifiedName + "::" + Name : Name;
		}
	}

}

