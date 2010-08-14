using System;
using System.Linq;
using Mono.VisualC.Interop;
using Mono.VisualC.Interop.Util;

namespace CPPInterop {

	// This is the best I could come up with to prevent duplicate managed
	//  signatures. The problem is, most of the types don't exist yet.
	public struct MethodSignature {
		public string Name;
		public CppType [] Arguments;

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

