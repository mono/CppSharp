//
// Mono.VisualC.Interop.Util.DelegateTypeCache.cs: Automatic delegate type creation and caching
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
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Mono.VisualC.Interop.Util {

	public static class DelegateTypeCache {

		private static Dictionary<BasicSignature, Type> type_cache;

		public static Type GetDelegateType (MethodInfo signature, CallingConvention? callingConvention)
		{
			return GetDelegateType (ReflectionHelper.GetMethodParameterTypes (signature), signature.ReturnType, callingConvention);
		}
		public static Type GetDelegateType (IEnumerable<Type> parameterTypes, Type returnType, CallingConvention? callingConvention)
		{
			return GetDelegateType (new BasicSignature { ParameterTypes = parameterTypes.ToList (), ReturnType = returnType, CallingConvention = callingConvention });
		}
		public static Type GetDelegateType (BasicSignature signature)
		{
			Type delegateType;
			if (type_cache == null)
				type_cache = new Dictionary<BasicSignature, Type> ();

			if (!type_cache.TryGetValue (signature, out delegateType)) {
				delegateType = CreateDelegateType (signature);
				type_cache.Add (signature, delegateType);
			}

			return delegateType;
		}

		private static Type CreateDelegateType (BasicSignature signature)
		{
			string delTypeName = signature.UniqueName;

			TypeAttributes typeAttr = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass;
			TypeBuilder del = CppLibrary.interopModule.DefineType (delTypeName, typeAttr, typeof(MulticastDelegate));

			if (signature.CallingConvention.HasValue) {
				ConstructorInfo ufpa = typeof (UnmanagedFunctionPointerAttribute).GetConstructor (new Type [] { typeof (CallingConvention) });
				CustomAttributeBuilder unmanagedPointer = new CustomAttributeBuilder (ufpa, new object [] { signature.CallingConvention.Value });
				del.SetCustomAttribute (unmanagedPointer);
			}

			MethodAttributes ctorAttr = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
			ConstructorBuilder ctor = del.DefineConstructor (ctorAttr, CallingConventions.Standard, new Type[] { typeof(object), typeof(System.IntPtr) });
			ctor.SetImplementationFlags (MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			Type [] parameterTypes = signature.ParameterTypes.ToArray ();
			MethodAttributes methodAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;

			MethodBuilder invokeMethod = del.DefineMethod ("Invoke", methodAttr, signature.ReturnType, parameterTypes);
			invokeMethod.SetImplementationFlags (MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			return del.CreateType ();
		}
	}

}

