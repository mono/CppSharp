//
// Mono.VisualC.Interop.Util.DelegateTypeCache.cs: Automatic delegate type creation and caching
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Mono.VisualC.Interop.Util {

	public static class DelegateTypeCache {

		private static Dictionary<DelegateSignature, Type> type_cache;


		public static Type GetDelegateType (MethodInfo signature, CallingConvention? callingConvention)
		{
			return GetDelegateType (ReflectionHelper.GetMethodParameterTypes (signature), signature.ReturnType, callingConvention);
		}
		public static Type GetDelegateType (IEnumerable<Type> parameterTypes, Type returnType, CallingConvention? callingConvention)
		{
			return GetDelegateType (new DelegateSignature () { ParameterTypes = parameterTypes, ReturnType = returnType, CallingConvention = callingConvention });
		}
		public static Type GetDelegateType (DelegateSignature signature)
		{
			Type delegateType;
			if (type_cache == null)
				type_cache = new Dictionary<DelegateSignature, Type> ();

			if (!type_cache.TryGetValue (signature, out delegateType)) {
				delegateType = CreateDelegateType (signature);
				type_cache.Add (signature, delegateType);
			}

			return delegateType;
		}

		private static Type CreateDelegateType (DelegateSignature signature)
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

