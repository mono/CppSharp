//
// Mono.Cxxi.CppField.cs: Represents a field in a native C++ object
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010-2011 Alexander Corrado
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

namespace Mono.Cxxi {
	public class CppField<T> {
		private int fieldOffset;

		public CppField (int fieldOffset)
		{
			this.fieldOffset = fieldOffset;
		}

		public T this [CppInstancePtr ip] {
			get {

				Type retType = typeof (T).IsEnum? Enum.GetUnderlyingType (typeof (T)) : typeof (T);
				object retVal;

				     if (retType.Equals (typeof (Byte)))
					retVal = Marshal.ReadByte (ip.Native, fieldOffset);
				else if (retType.Equals (typeof (Int16)))
					retVal = Marshal.ReadInt16 (ip.Native, fieldOffset);
				else if (retType.Equals (typeof (Int32)))
					retVal = Marshal.ReadInt32 (ip.Native, fieldOffset);

				else if (typeof (ICppObject).IsAssignableFrom (retType)) {

					var ptr = Marshal.ReadIntPtr (ip.Native, fieldOffset);
					if (ptr == IntPtr.Zero)
						return default (T);

					var ctor = retType.GetConstructor (new Type [] { typeof (IntPtr) });
					if (ctor != null) {

						retVal = ctor.Invoke (new object [] { ptr });

					} else {
						ctor = retType.GetConstructor (new Type [] { typeof (CppInstancePtr) });
						if (ctor == null)
							throw new NotSupportedException ("Type " + retType.Name + " does not have a constructor that takes either IntPtr or CppInstancePtr.");

						retVal = ctor.Invoke (new object [] { new CppInstancePtr (ptr) });
					}

				} else {
					throw new NotSupportedException ("Cannot read C++ fields of type " + retType.Name);
				}

				return (T)retVal;
			}
			set {
				Type setType = typeof (T).IsEnum? Enum.GetUnderlyingType (typeof (T)) : typeof (T);
				object setVal = value;

				     if (setType.Equals (typeof (Byte)))
					Marshal.WriteByte (ip.Native, fieldOffset, (byte)setVal);

				else if (setType.Equals (typeof (Int16)))
					Marshal.WriteInt16 (ip.Native, fieldOffset, (Int16)setVal);

				else if (setType.Equals (typeof (Int32)))
					Marshal.WriteInt32 (ip.Native, fieldOffset, (Int32)setVal);

				else if (typeof (ICppObject).IsAssignableFrom (setType)) {

					if (value == null) {
						Marshal.WriteIntPtr (ip.Native, fieldOffset, IntPtr.Zero);

					} else {

						var cppobj = (ICppObject)value;
						Marshal.WriteIntPtr (ip.Native, fieldOffset, (IntPtr)cppobj.Native);
					}

				} else {
					throw new NotSupportedException ("Cannot write C++ fields of type " + setType.Name);
				}

			}
		}
	}
}
