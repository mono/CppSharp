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

using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop.ABI {

	public class ItaniumTypeInfo : CppTypeInfo {
		public ItaniumTypeInfo (ItaniumAbi abi, IEnumerable<PInvokeSignature> virtualMethods, Type nativeLayout, Type wrapperType)
			: base (abi, virtualMethods, nativeLayout, wrapperType)
		{
		}


		// When adding a non-primary base class's complete vtable, we need to reserve space for
		//  the stuff before the address point of the vtptr..
		//  Includes vbase & vcall offsets (virtual inheritance), offset to top, and RTTI info
		public override void AddBase (CppTypeInfo baseType)
		{
			if (TypeComplete)
				return;

			if (BaseClasses.Count > 0 && baseType.VirtualMethods.Any ()) { // already have a primary base

				// FIXME: virtual inheritance
				virtual_methods.Insert (BaseVTableSlots, null);
				virtual_methods.Insert (BaseVTableSlots, null);
				BaseVTableSlots += 2;

			}

			base.AddBase (baseType);
		}

	}
}

