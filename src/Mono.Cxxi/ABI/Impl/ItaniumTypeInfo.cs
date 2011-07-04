//
// Mono.Cxxi.Abi.ItaniumTypeInfo.cs: An implementation of the Itanium C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2011 Alexander Corrado
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
using System.Runtime.InteropServices;
using System.Reflection;

using Mono.Cxxi.Util;

namespace Mono.Cxxi.Abi {

	public class ItaniumTypeInfo : CppTypeInfo {
		public ItaniumTypeInfo (ItaniumAbi abi, IEnumerable<PInvokeSignature> virtualMethods, Type nativeLayout, Type wrapperType)
			: base (abi, virtualMethods, nativeLayout, wrapperType)
		{
		}

		protected override void AddBase (CppTypeInfo baseType, bool addVTable)
		{

			// When adding a non-primary base class's complete vtable, we need to reserve space for
			// the stuff before the address point of the vtptr..
			// Includes vbase & vcall offsets (virtual inheritance), offset to top, and RTTI info
			if (addVTable) {

				// FIXME: virtual inheritance
				virtual_methods.Add (null);
				virtual_methods.Add (null);

				vt_overrides.Add (2);
				vt_delegate_types.Add (2);
			}

			base.AddBase (baseType, addVTable);
		}

		protected override bool OnVTableDuplicate (ref int iter, PInvokeSignature sig, PInvokeSignature dup)
		{
			var isOverride = base.OnVTableDuplicate (ref iter, sig, dup);
			if (isOverride && sig.Type == MethodType.NativeDtor) {

				// also remove that pesky extra dtor
				virtual_methods.RemoveAt (iter + 1);
				vt_overrides.Remove (1);
				vt_delegate_types.Remove (1);
				return true;
			}

			return false;
		}

	}
}

