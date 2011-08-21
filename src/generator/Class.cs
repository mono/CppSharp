//
// Class.cs: Represents a C++ class
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
using System.Collections.Generic;

public class Class : Namespace {

	public Class (Node n)
		: base (n)
	{
		BaseClasses = new List<Class> ();
		Fields = new List<Field> ();
		Properties = new List<Property> ();
		Methods = new List<Method> ();
		NestedClasses = new List<Class> ();
		NestedEnums = new List<Enumeration> ();
	}

	public List<Class> BaseClasses {
		get; set;
	}

	public List<Class> NestedClasses {
		get; set;
	}

	public List<Enumeration> NestedEnums {
		get; set;
	}

	public List<Field> Fields {
		get; set;
	}

	public List<Property> Properties {
		get; set;
	}

	public List<Method> Methods {
		get;
		set;
	}

	public bool Disable {
		get; set;
	}

}
