//
// Mono.VisualC.Interop.ABI.CppAbi.cs: Represents an abstract C++ ABI
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

using System.Collections.Generic;
using System.Runtime.InteropServices;

using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop.ABI {

	//FIXME: Exception handling, operator overloading etc.
	//FIXME: Allow interface to override default calling convention
	public abstract partial class CppAbi {
		protected ModuleBuilder impl_module;
		protected TypeBuilder impl_type;

		protected Type interface_type, layout_type, wrapper_type;
		protected string library, class_name;

		protected FieldBuilder typeinfo_field;
		protected ILGenerator ctor_il;

		protected MemberFilter vtable_override_filter = VTable.BindToSignatureAndAttribute;

		// Cache some frequently used methodinfos:
		private static readonly MethodInfo typeinfo_nativesize  = typeof (CppTypeInfo).GetProperty ("NativeSize").GetGetMethod ();
		private static readonly MethodInfo typeinfo_vtable      = typeof (CppTypeInfo).GetProperty ("VTable").GetGetMethod ();
		private static readonly MethodInfo typeinfo_adjvcall    = typeof (CppTypeInfo).GetMethod ("GetAdjustedVirtualCall");
		private static readonly MethodInfo vtable_initinstance  = typeof (VTable).GetMethod ("InitInstance");
		private static readonly MethodInfo vtable_resetinstance = typeof (VTable).GetMethod ("ResetInstance");

		// These methods might be more commonly overridden for a given C++ ABI:

		public virtual MethodType GetMethodType (MethodInfo imethod)
		{
			if (imethod.IsDefined (typeof (ConstructorAttribute), false))
				return MethodType.NativeCtor;
			else if (imethod.Name.Equals ("Alloc"))
				return MethodType.ManagedAlloc;
			else if (imethod.IsDefined (typeof (DestructorAttribute), false))
				return MethodType.NativeDtor;

			return MethodType.Native;
		}

		// The members below must be implemented for a given C++ ABI:

		public abstract CallingConvention? GetCallingConvention (MethodInfo methodInfo);
		protected abstract string GetMangledMethodName (MethodInfo methodInfo);
		public string GetMangledMethodName (string className, MethodInfo methodInfo)
		{
			class_name = className;
			return GetMangledMethodName (methodInfo);
		}

		// The ImplementClass overrides are the main entry point to the Abi API:

		private struct EmptyNativeLayout { }
		public Iface ImplementClass<Iface> (Type wrapperType, string lib, string className)
		{
			return this.ImplementClass<Iface,EmptyNativeLayout> (wrapperType, lib, className);
		}

		public virtual Iface ImplementClass<Iface, NLayout> (Type wrapperType, string lib, string className)
			where NLayout : struct
		//where Iface : ICppClassInstantiatable or ICppClassOverridable
		{
			this.impl_module = CppLibrary.interopModule;
			this.library = lib;
			this.class_name = className;
			this.interface_type = typeof (Iface);
			this.layout_type = typeof (NLayout);
			this.wrapper_type = wrapperType;

			DefineImplType ();

			var properties = GetProperties ();
			var methods = GetMethods ();
 			CppTypeInfo typeInfo = MakeTypeInfo (methods.Where (m => IsVirtual (m)));

			// Implement all methods
			int vtableIndex = 0;
			foreach (var method in methods)
				DefineMethod (method, typeInfo, ref vtableIndex);

			// Implement all properties
			foreach (var property in properties)
				DefineProperty (property);

			ctor_il.Emit (OpCodes.Ret);

			return (Iface)Activator.CreateInstance (impl_type.CreateType (), typeInfo);
		}

		protected virtual CppTypeInfo MakeTypeInfo (IEnumerable<MethodInfo> virtualMethods)
		{
			return new CppTypeInfo (this, virtualMethods, layout_type);
		}

		protected virtual IEnumerable<PropertyInfo> GetProperties ()
		{
			return ( // get all properties defined on the interface
			        from property in interface_type.GetProperties ()
			        select property
			       ).Union ( // ... as well as those defined on inherited interfaces
			        from iface in interface_type.GetInterfaces ()
			        from property in iface.GetProperties ()
			        select property
			       );
		}

		protected virtual IEnumerable<MethodInfo> GetMethods ()
		{
			// get all methods defined on inherited interfaces first
			var methods = (
				       from iface in interface_type.GetInterfaces ()
			               from method in iface.GetMethods ()
			               where !method.IsSpecialName
				       select method
			              ).Concat (
			               from method in interface_type.GetMethods ()
			               where !method.IsSpecialName
			               orderby method.MetadataToken
			               select method
			              );

			return methods;
		}

		protected virtual void DefineImplType ()
		{
			string implTypeName = interface_type.Name + "_" + layout_type.Name + "_" + this.GetType ().Name + "_Impl";
			impl_type = impl_module.DefineType (implTypeName, TypeAttributes.Class | TypeAttributes.Sealed);
			impl_type.AddInterfaceImplementation (interface_type);

			//vtable_field = impl_type.DefineField ("_vtable", typeof (VTable), FieldAttributes.InitOnly | FieldAttributes.Private);
			//native_size_field = impl_type.DefineField ("_nativeSize", typeof (int), FieldAttributes.InitOnly | FieldAttributes.Private);
			typeinfo_field = impl_type.DefineField ("_typeInfo", typeof (CppTypeInfo), FieldAttributes.InitOnly | FieldAttributes.Private);

			ConstructorBuilder ctor = impl_type.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard,
                                                                              new Type[] { typeof (CppTypeInfo) });

			ctor_il = ctor.GetILGenerator ();

			// this._typeInfo = (CppTypeInfo passed to constructor)
			ctor_il.Emit (OpCodes.Ldarg_0);
			ctor_il.Emit (OpCodes.Ldarg_1);
			ctor_il.Emit (OpCodes.Stfld, typeinfo_field);

			/*
			// this._vtable = (VTable passed to constructor)
			ctor_il.Emit (OpCodes.Ldarg_0);
			ctor_il.Emit (OpCodes.Ldarg_1);
			ctor_il.Emit (OpCodes.Stfld, vtable_field);
			// this._nativeSize = (native size passed to constructor)
			ctor_il.Emit (OpCodes.Ldarg_0);
			ctor_il.Emit (OpCodes.Ldarg_2);
			ctor_il.Emit (OpCodes.Stfld, native_size_field);
			*/
		}

		protected virtual MethodBuilder DefineMethod (MethodInfo interfaceMethod, CppTypeInfo typeInfo, ref int vtableIndex)
		{
			// 0. Introspect method
			MethodType methodType = GetMethodType (interfaceMethod);
			Type [] parameterTypes = ReflectionHelper.GetMethodParameterTypes (interfaceMethod);

			// 1. Generate managed trampoline to call native method
			MethodBuilder trampoline = GetMethodBuilder (interfaceMethod);

			ILGenerator il = trampoline.GetILGenerator ();

			if (methodType == MethodType.NoOp) {
				// return NULL if method is supposed to return a value
				// FIXME: this will make value types explode?
				if (!interfaceMethod.ReturnType.Equals (typeof (void)))
					il.Emit (OpCodes.Ldnull);
				il.Emit (OpCodes.Ret);
				return trampoline;
			} else if (methodType == MethodType.ManagedAlloc) {
				EmitManagedAlloc (il, interfaceMethod);
				il.Emit (OpCodes.Ret);
				return trampoline;
			}

			bool isStatic = IsStatic (interfaceMethod);
			LocalBuilder cppInstancePtr = null;
			LocalBuilder nativePtr = null;

			// If we're an instance method, load up the "this" pointer
			if (!isStatic)
			{
				if (parameterTypes.Length < 1)
					throw new ArgumentException ("First argument to non-static C++ method must be IntPtr or CppInstancePtr.");

				// 2. Load the native C++ instance pointer
				EmitLoadInstancePtr (il, parameterTypes [0], out cppInstancePtr, out nativePtr);

				// 3. Make sure our native pointer is a valid reference. If not, throw ObjectDisposedException
				EmitCheckDisposed (il, nativePtr, methodType);
			}

			MethodInfo nativeMethod;

			if (IsVirtual (interfaceMethod) && methodType != MethodType.NativeDtor)
				nativeMethod = EmitPrepareVirtualCall (il, typeInfo, nativePtr, vtableIndex++);
			else
				nativeMethod = GetPInvokeForMethod (interfaceMethod);

			switch (methodType) {
			case MethodType.NativeCtor:
				EmitConstruct (il, nativeMethod, parameterTypes, nativePtr);
				break;
			case MethodType.NativeDtor:
				EmitDestruct (il, nativeMethod, parameterTypes, cppInstancePtr, nativePtr);
				break;
			default:
				EmitCallNative (il, nativeMethod, isStatic, parameterTypes, nativePtr);
				break;
			}

			il.Emit (OpCodes.Ret);
			return trampoline;
		}

		protected virtual PropertyBuilder DefineProperty (PropertyInfo property)
		{
			if (property.CanWrite)
				throw new InvalidProgramException ("Properties in C++ interface must be read-only.");

			MethodInfo imethod = property.GetGetMethod ();
			string methodName = imethod.Name;
			string propName = property.Name;
			Type retType = imethod.ReturnType;
			FieldBuilder fieldData;

			// C++ interface properties are either to return the CppTypeInfo or to access C++ fields
			if (retType.IsGenericType && retType.GetGenericTypeDefinition ().Equals (typeof (CppField<>))) {
				// define a new field for the property
				// fieldData = impl_type.DefineField ("__" + propName + "_Data", retType, FieldAttributes.InitOnly | FieldAttributes.Private);

				// init our field data with a new instance of CppField
				// first, get field offset
				//ctor_il.Emit (OpCodes.Ldarg_0);

				/* TODO: Code prolly should not emit hardcoded offsets n such, in case we end up saving these assemblies in the future.
				 *  Something more like this perhaps? (need to figure out how to get field offset padding into this)
				 *       ctorIL.Emit(OpCodes.Ldtoken, nativeLayout);
				 *       ctorIL.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
				 *       ctorIL.Emit(OpCodes.Ldstr, propName);
				 *       ctorIL.Emit(OpCodes.Call, typeof(Marshal).GetMethod("OffsetOf"));
				 */
				/*
				  int fieldOffset = ((int)Marshal.OffsetOf (layout_type, propName)) + FieldOffsetPadding;
				  ctor_il.Emit (OpCodes.Ldc_I4, fieldOffset);
				  ctor_il.Emit (OpCodes.Newobj, retType.GetConstructor (new Type[] { typeof(int) }));

				  ctor_il.Emit (OpCodes.Stfld, fieldData);
				*/
				throw new NotImplementedException ("CppFields need to be reimplemented to use CppTypeInfo.");
			} else if (retType.Equals (typeof (CppTypeInfo)))
				fieldData = typeinfo_field;
			else
				throw new InvalidProgramException ("Properties in C++ interface can only be of type CppField.");

			PropertyBuilder fieldProp = impl_type.DefineProperty (propName, PropertyAttributes.None, retType, Type.EmptyTypes);

			MethodAttributes methodAttr = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			MethodBuilder fieldGetter = impl_type.DefineMethod (methodName, methodAttr, retType, Type.EmptyTypes);
			ILGenerator il = fieldGetter.GetILGenerator ();

			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, fieldData);
			il.Emit (OpCodes.Ret);

			fieldProp.SetGetMethod (fieldGetter);

			return fieldProp;
		}

		/**
		 * Implements the managed trampoline that will be invoked from the vtable by native C++ code when overriding
		 *  the specified C++ virtual method with the specified managed one.
		 */
		internal virtual Delegate GetManagedOverrideTrampoline (CppTypeInfo typeInfo, int vtableIndex)
		{
			if (wrapper_type == null)
				return null;

			MethodInfo interfaceMethod = typeInfo.VirtualMethods [vtableIndex];
			MethodInfo targetMethod = FindManagedOverrideTarget (interfaceMethod);
			if (targetMethod == null)
				return null;

			Type[] parameterTypes = GetParameterTypesForPInvoke (interfaceMethod).ToArray ();

			// TODO: According to http://msdn.microsoft.com/en-us/library/w16z8yc4.aspx
			// The dynamic method created with this constructor has access to public and internal members of all the types contained in module m.
			// This does not appear to hold true, so we also disable JIT visibility checks.
			DynamicMethod trampolineIn = new DynamicMethod (wrapper_type.Name + "_" + interfaceMethod.Name + "_FromNative", interfaceMethod.ReturnType,
                                                                        parameterTypes, typeof (CppInstancePtr).Module, true);

			ReflectionHelper.ApplyMethodParameterAttributes (interfaceMethod, trampolineIn, true);
			ILGenerator il = trampolineIn.GetILGenerator ();

			// for static methods:
			OpCode callInstruction = OpCodes.Call;
			int argLoadStart = 1;

			// for instance methods, we need an instance to call them on!
			if (!targetMethod.IsStatic) {
				callInstruction = OpCodes.Callvirt;
				//argLoadStart = 1;

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldc_I4, typeInfo.NativeSize);

				MethodInfo getManagedObj = typeof (CppInstancePtr).GetMethod ("GetManaged", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod (wrapper_type);
				il.Emit (OpCodes.Call, getManagedObj);
			}

			for (int i = argLoadStart; i < parameterTypes.Length; i++) {
				il.Emit (OpCodes.Ldarg, i);
			}
			il.Emit (OpCodes.Tailcall);
			il.Emit (callInstruction, targetMethod);
			il.Emit (OpCodes.Ret);

			return trampolineIn.CreateDelegate (typeInfo.VTableDelegateTypes [vtableIndex]);
		}

		protected virtual MethodInfo FindManagedOverrideTarget (MethodInfo interfaceMethod)
		{
			// FIXME: Does/should this look in superclasses?
			MemberInfo [] possibleMembers = wrapper_type.FindMembers (MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic |
																	  BindingFlags.Instance | BindingFlags.Static, vtable_override_filter, interfaceMethod);

			if (possibleMembers.Length > 1)
				throw new InvalidProgramException ("More than one possible override found when binding virtual method: " + interfaceMethod.Name);
			else if (possibleMembers.Length == 0)
				return null;

			return (MethodInfo)possibleMembers [0];
		}

		/**
		 * Defines a new MethodBuilder with the same signature as the passed MethodInfo
		 */
		protected virtual MethodBuilder GetMethodBuilder (MethodInfo interfaceMethod)
		{
			Type [] parameterTypes = ReflectionHelper.GetMethodParameterTypes (interfaceMethod);
			MethodBuilder methodBuilder = impl_type.DefineMethod (interfaceMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual,
																  interfaceMethod.ReturnType, parameterTypes);
			ReflectionHelper.ApplyMethodParameterAttributes (interfaceMethod, methodBuilder, false);
			return methodBuilder;
		}

		/**
		 * Defines a new MethodBuilder that calls the specified C++ (non-virtual) method using its mangled name
		 */
		protected virtual MethodBuilder GetPInvokeForMethod (MethodInfo signature)
		{
			string entryPoint = GetMangledMethodName (signature);
			if (entryPoint == null)
				throw new NotSupportedException ("Could not mangle method name.");

			Type [] parameterTypes = GetParameterTypesForPInvoke (signature).ToArray ();

			MethodBuilder builder = impl_type.DefinePInvokeMethod ("__$" + signature.Name + "_Impl", library, entryPoint,
                                                                              MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                                                                              CallingConventions.Standard, signature.ReturnType, parameterTypes,
                                                                              GetCallingConvention (signature).Value, CharSet.Ansi);
			builder.SetImplementationFlags (builder.GetMethodImplementationFlags () | MethodImplAttributes.PreserveSig);
			ReflectionHelper.ApplyMethodParameterAttributes (signature, builder, true);
			return builder;
		}

		/**
		 * Emits the IL to load the correct delegate instance and/or retrieve the MethodInfo from the VTable
		 * for a C++ virtual call.
		 */
		protected virtual MethodInfo EmitPrepareVirtualCall (ILGenerator il, CppTypeInfo typeInfo, LocalBuilder native, int vtableIndex)
		{
			Type vtableDelegateType = typeInfo.VTableDelegateTypes [vtableIndex];
			MethodInfo getDelegate  = typeinfo_adjvcall.MakeGenericMethod (vtableDelegateType);

			// this._typeInfo.GetAdjustedVirtualCall<T> (native, vtableIndex);
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, typeinfo_field);
			il.Emit (OpCodes.Ldloc_S, native);
			il.Emit (OpCodes.Ldc_I4, vtableIndex);
			il.Emit (OpCodes.Callvirt, getDelegate);

			return ReflectionHelper.GetMethodInfoForDelegate (vtableDelegateType);
		}

		/**
		 *  Emits IL to allocate the memory for a new instance of the C++ class.
		 *  To complete method, emit OpCodes.Ret.
		 */
		protected virtual void EmitManagedAlloc (ILGenerator il, MethodInfo interfaceMethod)
		{
			// this._typeInfo.NativeSize
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, typeinfo_field);
			il.Emit (OpCodes.Callvirt, typeinfo_nativesize);

			if (wrapper_type != null) {
				// load managed wrapper
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Newobj, typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
																				 new Type[] { typeof (int), typeof (object) }, null));
			} else
				il.Emit (OpCodes.Newobj, typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
																				 new Type[] { typeof (int) }, null));
		}

		protected virtual void EmitConstruct (ILGenerator il, MethodInfo nativeMethod, Type [] parameterTypes,
											  LocalBuilder nativePtr)
		{
			EmitCallNative (il, nativeMethod, false, parameterTypes, nativePtr);
			EmitInitVTable (il, nativePtr);
		}

		protected virtual void EmitDestruct (ILGenerator il, MethodInfo nativeMethod, Type [] parameterTypes,
											 LocalBuilder cppInstancePtr, LocalBuilder nativePtr)
		{
			// bail if we weren't alloc'd by managed code
			Label bail = il.DefineLabel ();

			il.Emit (OpCodes.Ldloca_S, cppInstancePtr);
			il.Emit (OpCodes.Brfalse_S, bail); // <- FIXME? (would this ever branch?)
			il.Emit (OpCodes.Ldloca_S, cppInstancePtr);
			il.Emit (OpCodes.Call, typeof (CppInstancePtr).GetProperty ("IsManagedAlloc").GetGetMethod ());
			il.Emit (OpCodes.Brfalse_S, bail);

			EmitResetVTable (il, nativePtr);
			EmitCallNative (il, nativeMethod, false, parameterTypes, nativePtr);

			il.MarkLabel (bail);
		}

		/**
		 * Emits IL to call the native method. nativeMethod should be either a method obtained by
		 * GetPInvokeForMethod or the MethodInfo of a vtable method.
		 * To complete method, emit OpCodes.Ret.
		 */
		protected virtual void EmitCallNative (ILGenerator il, MethodInfo nativeMethod, bool isStatic, Type [] parameterTypes,
											   LocalBuilder nativePtr)
		{
			int argLoadStart = 1; // For static methods, just strip off arg0 (.net this pointer)
			if (!isStatic)
			{
				argLoadStart = 2; // For instance methods, strip off CppInstancePtr and pass the corresponding IntPtr
				il.Emit (OpCodes.Ldloc_S, nativePtr);
			}
			for (int i = argLoadStart; i <= parameterTypes.Length; i++) {
				il.Emit (OpCodes.Ldarg, i);
				EmitSpecialParameterMarshal (il, parameterTypes [i - 1]);
			}

			il.Emit (OpCodes.Call, nativeMethod);
		}

		// Note: when this is modified, usually GetParameterTypesForPInvoke types should be updated too
		protected virtual void EmitSpecialParameterMarshal (ILGenerator il, Type parameterType)
		{
			// auto marshal bool to C++ bool type (0 = false , 1 = true )
			if (parameterType.Equals (typeof (bool))) {
				Label isTrue = il.DefineLabel ();
				Label done = il.DefineLabel ();
				il.Emit (OpCodes.Brtrue_S, isTrue);
				il.Emit (OpCodes.Ldc_I4_0);
				il.Emit (OpCodes.Br_S, done);
				il.MarkLabel (isTrue);
				il.Emit (OpCodes.Ldc_I4_1);
				il.MarkLabel (done);
				//il.Emit (OpCodes.Conv_I1);
			}
			// auto marshal ICppObject
		}

		public virtual IEnumerable<Type> GetParameterTypesForPInvoke (MethodInfo method)
		{
			var originalTypes = ReflectionHelper.GetMethodParameterTypes (method);

			var pinvokeTypes = originalTypes.Transform (
			        For.AnyInputIn (typeof (bool)).Emit (typeof (byte)),

				// CppInstancePtr implements ICppObject
				For.InputsWhere ((Type t) => typeof (ICppObject).IsAssignableFrom (t)).Emit (typeof (IntPtr)),

			        For.UnmatchedInput<Type> ().Emit (t => t)
			);
			return pinvokeTypes;
		}

		/**
		 * Emits IL to load the VTable object onto the stack.
		 */
		protected virtual void EmitLoadVTable (ILGenerator il)
		{
			// this._typeInfo.VTable
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, typeinfo_field);
			il.Emit (OpCodes.Callvirt, typeinfo_vtable);
		}

		/**
		 * Emits IL to set the vtable pointer of the instance (if class has a vtable).
		 * This should usually happen in the managed wrapper of the C++ instance constructor.
		 */
		protected virtual void EmitInitVTable (ILGenerator il, LocalBuilder nativePtr)
		{
			// this._typeInfo.VTable.InitInstance (nativePtr);
			EmitLoadVTable (il);
			il.Emit (OpCodes.Ldloc_S, nativePtr);
			EmitCallVTableMethod (il, vtable_initinstance, 2, false);
		}

		protected virtual void EmitResetVTable (ILGenerator il, LocalBuilder nativePtr)
		{
			// this._typeInfo.VTable.ResetInstance (nativePtr);
			EmitLoadVTable (il);
			il.Emit (OpCodes.Ldloc_S, nativePtr);
			EmitCallVTableMethod (il, vtable_resetinstance, 2, false);
		}

		/**
		 * A utility function to emit the IL for a vtable-dependant operation.
		 * In other words, classes with no virtual methods will not have vtables,
		 * so this method emits code to check for that and either throw an exception
		 * or do nothing if no vtable exists. To use, push the arguments to the method you
		 * want to call and pass the stackHeight for the call. If no vtable exists, this method
		 * will emit code to pop the arguments off the stack.
		 */
		protected virtual void EmitCallVTableMethod (ILGenerator il, MethodInfo method, int stackHeight,
		                                             bool throwOnNoVTable)
		{
			// prepare a jump; do not call vtable method if no vtable
			Label noVirt = il.DefineLabel ();
			Label dontPushOrThrow = il.DefineLabel ();

			EmitLoadVTable (il);
			il.Emit (OpCodes.Brfalse_S, noVirt); // if (vtableInfo == null) goto noVirt

			il.Emit (OpCodes.Callvirt, method); // call method
			il.Emit (OpCodes.Br_S, dontPushOrThrow); // goto dontPushOrThrow

			il.MarkLabel (noVirt);
			// noVirt:
			// since there is no vtable, we did not make the method call.
			// pop arguments
			for (int i = 0; i < stackHeight; i++)
				il.Emit (OpCodes.Pop);

			// if the method was supposed to return a value, we must
			// still push something onto the stack
			// FIXME: This is a kludge. What about value types?
			if (!method.ReturnType.Equals (typeof (void)))
				il.Emit (OpCodes.Ldnull);

			if (throwOnNoVTable) {
				il.Emit (OpCodes.Ldstr, "Native class has no VTable.");
				il.Emit (OpCodes.Newobj, typeof (InvalidOperationException).GetConstructor(new Type[] {typeof (string)}));
				il.Emit (OpCodes.Throw);
			}

			il.MarkLabel (dontPushOrThrow);
		}

		protected virtual void EmitLoadInstancePtr (ILGenerator il, Type firstParamType, out LocalBuilder cppip,
                                                            out LocalBuilder native)
		{
			cppip = null;
			native = null;

			il.Emit (OpCodes.Ldarg_1);
			if (firstParamType.Equals (typeof (CppInstancePtr))) {
				cppip = il.DeclareLocal (typeof (CppInstancePtr));
				native = il.DeclareLocal (typeof (IntPtr));
				il.Emit (OpCodes.Stloc_S, cppip);
				il.Emit (OpCodes.Ldloca_S, cppip);
				il.Emit (OpCodes.Call, typeof (CppInstancePtr).GetProperty ("Native").GetGetMethod ());
				il.Emit (OpCodes.Stloc_S, native);
			} else if (firstParamType.Equals (typeof (IntPtr))) {
				native = il.DeclareLocal (typeof (IntPtr));
				il.Emit (OpCodes.Stloc_S, native);
			} else if (firstParamType.IsByRef) {
				native = il.DeclareLocal (firstParamType);
				il.Emit (OpCodes.Stloc_S, native);
			} else
				throw new ArgumentException ("First argument to non-static C++ method must be byref, IntPtr or CppInstancePtr.");
		}

		protected virtual void EmitCheckManagedAlloc (ILGenerator il, LocalBuilder cppip)
		{
			// make sure we were allocated by managed code
			// if not, return
			Label managedAlloc = il.DefineLabel ();
			il.Emit (OpCodes.Ldloca_S, cppip);
			il.Emit (OpCodes.Call, typeof (CppInstancePtr).GetProperty ("IsManagedAlloc").GetGetMethod ());
			il.Emit (OpCodes.Brtrue_S, managedAlloc);
			il.Emit (OpCodes.Ret);
			il.MarkLabel (managedAlloc);
		}

		/**
		 * throw ObjectDisposedException if we have a null pointer for native
		 * however, allow destructor to be called even if we're disposed (just return immediately)
		 */
		protected virtual void EmitCheckDisposed (ILGenerator il, LocalBuilder native, MethodType methodType)
		{
			Label validRef = il.DefineLabel ();
			il.Emit (OpCodes.Ldloc_S, native);
			il.Emit (OpCodes.Brtrue_S, validRef);
			if (methodType == MethodType.NativeDtor) {
				il.Emit (OpCodes.Ret);
				il.MarkLabel (validRef);
			} else {
				il.Emit (OpCodes.Ldstr, String.Empty);
				il.Emit (OpCodes.Newobj, typeof (ObjectDisposedException).GetConstructor (new Type[] { typeof(string) }));
				il.Emit (OpCodes.Throw);
				il.MarkLabel (validRef);
			}
		}
	}

}
