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
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop {
	public class CppObjectMarshaler : ICustomMarshaler {
		private static CppObjectMarshaler marshaler = null;
		private CppObjectMarshaler () {
		}

		public IntPtr MarshalManagedToNative (object managedObj)
		{
			if (managedObj == null)
				return IntPtr.Zero;

			ICppObject cppObject = managedObj as ICppObject;
			if (cppObject == null)
				throw new ArgumentException ("Object to marshal must implement ICppObject");

			return (IntPtr)cppObject.Native;
		}

		public object MarshalNativeToManaged (IntPtr pNativeData)
		{
			throw new NotImplementedException ();
		}

		public void CleanUpManagedData (object ManagedObj)
		{
		}

		public void CleanUpNativeData (IntPtr pNativeData)
		{
		}

		public int GetNativeDataSize ()
		{
			return -1;
		}

		public static ICustomMarshaler GetInstance(string cookie)
		{
			if(marshaler == null)
				marshaler = new CppObjectMarshaler ();
			return marshaler;
		}

	}
}

