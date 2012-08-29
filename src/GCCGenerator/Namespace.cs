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

	private string [] fullyQualifiedName;
	public string [] FullyQualifiedName {
		get {
			if (fullyQualifiedName == null) {

				if (ParentNamespace == null) {
					fullyQualifiedName = new string[] { Name };
				} else {
					var parentFqn = ParentNamespace.FullyQualifiedName;
					fullyQualifiedName = new string[parentFqn.Length + 1];
					Array.Copy (parentFqn, fullyQualifiedName, parentFqn.Length);
					fullyQualifiedName [parentFqn.Length] = Name;
				}
			}
			return fullyQualifiedName;
		}
	}

}

