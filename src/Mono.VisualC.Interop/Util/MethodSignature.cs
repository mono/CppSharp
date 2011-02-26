//
// Mono.VisualC.Interop.Util.MethodSignature.cs: Hash-friendly structs to represent arbitrary method and delegate signatures
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Mono.VisualC.Interop.Util {

	public struct DelegateSignature {
		public CallingConvention? CallingConvention { get; set; }
		public IEnumerable<Type> ParameterTypes { get; set; }
		public Type ReturnType { get; set; }

		private string uniqueName;

		public string UniqueName {
			get {
				if (uniqueName != null)
					return uniqueName;

				StringBuilder sb = new StringBuilder ("_");

				if (CallingConvention.HasValue)
					sb.Append (Enum.GetName (typeof (CallingConvention), CallingConvention.Value));

				sb.Append ('_').Append (ReturnType.Name);

				if (ParameterTypes == null)
					return uniqueName = sb.ToString ();

				foreach (var param in ParameterTypes)
					sb.Append ('_').Append (param.Name);

				return uniqueName = sb.ToString ();
			}
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType () != typeof(DelegateSignature))
				return false;
			DelegateSignature other = (DelegateSignature)obj;

			return CallingConvention == other.CallingConvention &&
				((ParameterTypes == null && other.ParameterTypes == null) ||
				 ParameterTypes.SequenceEqual (other.ParameterTypes)) &&
				ReturnType.Equals (other.ReturnType);
		}

		public override int GetHashCode ()
		{
			unchecked {
				return CallingConvention.GetHashCode () ^
				      (ParameterTypes != null? ParameterTypes.SequenceHashCode () : 0) ^
				      ReturnType.GetHashCode ();
			}
		}
	}

	public struct MethodSignature {
		public string Name { get; set; }
		public MethodType Type { get; set; }
		public DelegateSignature Signature { get; set; }

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType () != typeof(MethodSignature))
				return false;
			MethodSignature other = (MethodSignature)obj;
			return Signature.Equals (other.Signature) &&
			       Type == other.Type &&
			      (Type != MethodType.Native || Name.Equals (other.Name));
		}


		public override int GetHashCode ()
		{
			unchecked {
				return Signature.GetHashCode () ^
				       Type.GetHashCode () ^
				      (Type == MethodType.Native? Name.GetHashCode () : 0);
			}
		}

	}
}

