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
using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Tools.Generator {

	// This is the best I could come up with to prevent duplicate managed
	//  signatures. The problem is, most of the types don't exist yet.
	public struct MethodSignature {
		public string Name;
		public IEnumerable<CppType> Arguments;

		public MethodSignature (string name, IEnumerable<CppType> args)
		{
			Name = name;

			// This is kinda hacky, but it was the best I could come up with at the time.
			// Remove any modifiers that will affect the managed type signature.
			// FIXME: Subtract more?
			Arguments = args.Select (a => a.Subtract (CppModifiers.Const))
					.Select (a => a.Subtract (CppModifiers.Volatile))
					.Select (a => a.Subtract (CppModifiers.Pointer))
					.Select (a => a.Subtract (CppModifiers.Reference))
	                .Select (a => a.Modifiers.Count (m => m == CppModifiers.Long) > 1 ? a.Subtract (CppModifiers.Long) : a)
	                .Select (a => a.ElementType == CppTypes.Char? a.Subtract (CppModifiers.Unsigned).Subtract (CppModifiers.Signed) : a);
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType () != typeof(MethodSignature))
				return false;
			MethodSignature other = (MethodSignature)obj;
			return Name == other.Name &&
			       Arguments != null? Arguments.SequenceEqual (other.Arguments) :
				other.Arguments == null;
		}


		public override int GetHashCode ()
		{
			unchecked {
				return (Name != null? Name.GetHashCode () : 0) ^
				       (Arguments != null? Arguments.SequenceHashCode () : 0);
			}
		}

	}
}

