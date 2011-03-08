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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Mono.VisualC.Interop.ABI {
	public class MsvcTypeInfo : CppTypeInfo {
		public MsvcTypeInfo (MsvcAbi abi, IEnumerable<MethodInfo> virtualMethods, Type nativeLayout)
			: base (abi, virtualMethods, nativeLayout)
		{
		}

		// AFIK, MSVC places only its first base's virtual methods in the derived class's
		//  primary vtable. Subsequent base classes (each?) get another vtable pointer
		public override void AddBase (CppTypeInfo baseType)
		{
			if (TypeComplete)
				return;

			if (BaseClasses.Count == 0)
				base.AddBase (baseType, false);
			else
				base.AddBase (baseType, true);
		}

	}
}

