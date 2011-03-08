//
// Mono.VisualC.Interop.ABI.VTable.cs: abstract vtable
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
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop.ABI {
	public delegate VTable MakeVTableDelegate (CppTypeInfo metadata);

	// TODO: RTTI .. support virtual inheritance
	public abstract class VTable : IDisposable {
		public static MakeVTableDelegate DefaultImplementation = VTableManaged.Implementation;

		protected CppTypeInfo typeInfo;
		protected IntPtr basePtr, vtPtr;

		public virtual int EntryCount {
			get { return typeInfo.VirtualMethods.Count; }
		}
		public virtual int EntrySize {
			get { return Marshal.SizeOf (typeof (IntPtr)); }
		}

		public abstract T GetVirtualCallDelegate<T> (IntPtr native, int vtableIndex) where T : class; /*Delegate*/

		// Subclasses should allocate vtPtr and then call WriteOverrides
		public VTable (CppTypeInfo typeInfo)
		{
			this.typeInfo = typeInfo;
		}

		protected virtual void WriteOverrides ()
		{
			IntPtr vtEntryPtr;
			int currentOffset = typeInfo.VTableTopPadding;
			for (int i = 0; i < EntryCount; i++) {
				Delegate currentOverride = typeInfo.VTableOverrides [i];

				if (currentOverride != null) // managed override
					vtEntryPtr = Marshal.GetFunctionPointerForDelegate (currentOverride);
				else
					vtEntryPtr = IntPtr.Zero;

				Marshal.WriteIntPtr (vtPtr, currentOffset, vtEntryPtr);
				currentOffset += EntrySize;
			}
		}

		// FIXME: Make this method unsafe.. it would probably be much faster
		public virtual void InitInstance (IntPtr instance)
		{
			if (basePtr == IntPtr.Zero) {
				basePtr = Marshal.ReadIntPtr (instance);
				if ((basePtr != IntPtr.Zero) && (basePtr != vtPtr)) {
					// FIXME: This could probably be a more efficient memcpy
					for(int i = 0; i < typeInfo.VTableTopPadding; i++)
						Marshal.WriteByte(vtPtr, i, Marshal.ReadByte(basePtr, i));

					int currentOffset = typeInfo.VTableTopPadding;
					for (int i = 0; i < EntryCount; i++) {
						if (Marshal.ReadIntPtr (vtPtr, currentOffset) == IntPtr.Zero)
							Marshal.WriteIntPtr (vtPtr, currentOffset, Marshal.ReadIntPtr (basePtr, currentOffset));

						currentOffset += EntrySize;
					}

					// FIXME: This could probably be a more efficient memcpy
					for(int i = 0; i < typeInfo.VTableBottomPadding; i++)
						Marshal.WriteByte(vtPtr, currentOffset + i, Marshal.ReadByte(basePtr, currentOffset + i));
				}
			}

			Marshal.WriteIntPtr (instance, vtPtr);
		}

		public virtual void ResetInstance (IntPtr instance)
		{
			Marshal.WriteIntPtr (instance, basePtr);
		}

		public IntPtr Pointer {
			get { return vtPtr; }
		}

		protected virtual void Dispose (bool disposing)
		{
			if (vtPtr != IntPtr.Zero) {
				Marshal.FreeHGlobal (vtPtr);
				vtPtr = IntPtr.Zero;
			}
		}

		// TODO: This WON'T usually be called because VTables are associated with classes
		//  (not instances) and managed C++ class wrappers are staticly held?
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		~VTable ()
		{
			Dispose (false);
		}

		public static bool BindToSignatureAndAttribute (MemberInfo member, object obj)
		{
			bool result = BindToSignature (member, obj);
			if (member.GetCustomAttributes (typeof (OverrideNativeAttribute), true).Length != 1)
				return false;

			return result;
		}

		public static bool BindToSignature (MemberInfo member, object obj)
		{
			MethodInfo imethod = (MethodInfo) obj;
			MethodInfo candidate = (MethodInfo) member;

			if (!candidate.Name.Equals (imethod.Name))
				return false;

			ParameterInfo[] invokeParams = imethod.GetParameters ();
			ParameterInfo[] methodParams = candidate.GetParameters ();

			if (invokeParams.Length == methodParams.Length) {
				for (int i = 0; i < invokeParams.Length; i++) {
					if (!invokeParams [i].ParameterType.IsAssignableFrom (methodParams [i].ParameterType))
						return false;
				}
			} else if (invokeParams.Length == methodParams.Length + 1) {
				for (int i = 1; i < invokeParams.Length; i++) {
					if (!invokeParams [i].ParameterType.IsAssignableFrom (methodParams [i - 1].ParameterType))
						return false;
				}
			} else
				return false;

			return true;
		}
	}
}
