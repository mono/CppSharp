//
// Mono.VisualC.Interop.Util.MethodSignature.cs: Hash-friendly structs to represent arbitrary method and delegate signatures
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010 Alexander Corrado
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
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Mono.VisualC.Interop.Util {

	public class BasicSignature {
		public CallingConvention? CallingConvention { get; set; }
		public List<Type> ParameterTypes { get; set; }
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

		public bool IsCompatibleWith (BasicSignature other)
		{
			return CallingConvention == other.CallingConvention &&
			       ((ParameterTypes == null && other.ParameterTypes == null) ||
			         ParameterTypes.SequenceEqual (other.ParameterTypes)) &&
			       ReturnType.Equals (other.ReturnType);
		}

		public override bool Equals (object obj)
		{
			var other = obj as BasicSignature;
			if (other == null)
				return false;

			return IsCompatibleWith (other);
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

	public class MethodSignature : BasicSignature {
		public string Name { get; set; }
		public MethodType Type { get; set; }

		public new bool Equals (object obj)
		{
			var other = obj as MethodSignature;
			if (other == null)
				return false;

			return IsCompatibleWith (other) &&
			       Type == other.Type &&
			      (Name.Equals (other.Name) || Type != MethodType.Native);
		}


		public new int GetHashCode ()
		{
			unchecked {
				return base.GetHashCode () ^
				       Type.GetHashCode () ^
				      (Type == MethodType.Native? Name.GetHashCode () : 0);
			}
		}

	}

	public class PInvokeSignature : MethodSignature {
		// The original c# method this signature was generated from
		public MethodInfo OrigMethod;
	}
}

