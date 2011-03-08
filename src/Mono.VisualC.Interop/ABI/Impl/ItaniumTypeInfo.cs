//
// Author:
//   Andreia Gaita (shana@spoiledcat.net)
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
using System.Reflection;

namespace Mono.VisualC.Interop.ABI {
	public class ItaniumTypeInfo : CppTypeInfo {

		private bool hasVirtualDtor;

		public ItaniumTypeInfo (ItaniumAbi abi, IEnumerable<MethodInfo> virtualMethods, Type nativeLayout)
			: base (abi, virtualMethods, nativeLayout)
		{
			// Remove all virtual destructors from their declared position in the vtable
			for (int i = 0; i < VirtualMethods.Count; i++) {
				if (Abi.GetMethodType (VirtualMethods [i]) != MethodType.NativeDtor)
					continue;

				hasVirtualDtor = true;
				VirtualMethods.RemoveAt (i);
				break;
			}
		}

		public bool HasVirtualDestructor {
			get { return hasVirtualDtor; }
		}

		public bool HasExplicitCopyCtor { get; set; }
		public bool HasExplicitDtor { get; set; }

		public override void AddBase (CppTypeInfo baseType)
		{
			if (TypeComplete)
				return;

			hasVirtualDtor |= ((ItaniumTypeInfo)baseType).HasVirtualDestructor;
			base.AddBase (baseType);
		}

		// Itanium puts all its virtual dtors at the bottom of the vtable (2 slots each).
		//  Since we will never be in a position of calling a dtor virtually, we can just pad the vtable out.
		public override int VTableBottomPadding {
			get {
				if (!hasVirtualDtor)
					return 0;

				return 2 + CountBases (b => ((ItaniumTypeInfo)b).HasVirtualDestructor) * 2;
			}
		}

	}
}

