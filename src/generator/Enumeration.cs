using System;
using System.Collections.Generic;

public class Enumeration : Namespace {

	public struct Item {
		public string Name;
		public int Value;
	}

	public Enumeration (Node n)
		: base (n)
	{
		this.Items = new List<Item> ();
	}

	public List<Item> Items {
		get; set;
	}
}

