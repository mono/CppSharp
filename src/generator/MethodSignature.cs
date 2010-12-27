using System;
using System.Linq;
using System.Collections.Generic;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.Util;

namespace CPPInterop {

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
			                .Select (a => a.Modifiers.Count (m => m == CppModifiers.Long) > 1? a.Subtract (CppModifiers.Long) : a)
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

