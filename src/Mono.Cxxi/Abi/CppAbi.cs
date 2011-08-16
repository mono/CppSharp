//
// Mono.Cxxi.Abi.CppAbi.cs: Represents an abstract C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Mono.Cxxi.Util;

namespace Mono.Cxxi.Abi {

	//FIXME: Exception handling, operator overloading etc.
	//FIXME: Allow interface to override default calling convention

	// Subclasses should be singletons
	public abstract partial class CppAbi {
		// (other part of this partial class in Attributes.cs)

		protected Dictionary<Type,CppTypeInfo> wrapper_to_typeinfo = new Dictionary<Type, CppTypeInfo> ();
		protected MemberFilter vtable_override_filter = VTable.BindToSignatureAndAttribute;

		// Cache some frequently used methodinfos:
		protected static readonly MethodInfo typeinfo_nativesize   = typeof (CppTypeInfo).GetProperty ("NativeSize").GetGetMethod ();
		protected static readonly MethodInfo typeinfo_vtable       = typeof (CppTypeInfo).GetProperty ("VTable").GetGetMethod ();
		protected static readonly MethodInfo typeinfo_adjvcall     = typeof (CppTypeInfo).GetMethod ("GetAdjustedVirtualCall");
		protected static readonly MethodInfo typeinfo_fieldoffset  = typeof (CppTypeInfo).GetProperty ("FieldOffsetPadding").GetGetMethod ();
		protected static readonly MethodInfo vtable_initinstance   = typeof (VTable).GetMethod ("InitInstance");
		protected static readonly MethodInfo vtable_resetinstance  = typeof (VTable).GetMethod ("ResetInstance");
		protected static readonly MethodInfo cppobj_native         = typeof (ICppObject).GetProperty ("Native").GetGetMethod ();
		protected static readonly MethodInfo cppip_native          = typeof (CppInstancePtr).GetProperty ("Native").GetGetMethod ();
		protected static readonly MethodInfo cppip_managedalloc    = typeof (CppInstancePtr).GetProperty ("IsManagedAlloc").GetGetMethod ();
		protected static readonly MethodInfo cppip_tomanaged       = typeof (CppInstancePtr).GetMethod ("ToManaged", BindingFlags.Static | BindingFlags.NonPublic, null, new Type [] { typeof (IntPtr) }, null);
		protected static readonly MethodInfo cppip_tomanaged_size  = typeof (CppInstancePtr).GetMethod ("ToManaged", BindingFlags.Static | BindingFlags.NonPublic, null, new Type [] { typeof (IntPtr), typeof (int) }, null);
		protected static readonly MethodInfo cppip_dispose         = typeof (CppInstancePtr).GetMethod ("Dispose");
		protected static readonly ConstructorInfo cppip_fromnative = typeof (CppInstancePtr).GetConstructor (new Type [] { typeof (IntPtr) });
		protected static readonly ConstructorInfo cppip_fromsize   = typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null, new Type [] { typeof (int) }, null);
		protected static readonly ConstructorInfo cppip_fromtype_managed = typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
		                                                                                                         new Type[] { typeof (CppTypeInfo), typeof (object) }, null);
		protected static readonly ConstructorInfo notimplementedexception = typeof (NotImplementedException).GetConstructor (new Type [] { typeof (string) });
		protected static readonly MethodInfo type_gettypefromhandle  = typeof (Type).GetMethod ("GetTypeFromHandle");
		protected static readonly MethodInfo marshal_offsetof        = typeof (Marshal).GetMethod ("OffsetOf");
		protected static readonly MethodInfo marshal_structuretoptr  = typeof (Marshal).GetMethod ("StructureToPtr");
		protected static readonly MethodInfo marshal_ptrtostructure  = typeof (Marshal).GetMethod ("PtrToStructure", BindingFlags.Static | BindingFlags.Public, null, new Type [] { typeof (IntPtr), typeof (Type) }, null);
		protected static readonly FieldInfo  intptr_zero             = typeof (IntPtr).GetField ("Zero");

		// These methods might be more commonly overridden for a given C++ ABI:

		public virtual MethodType GetMethodType (CppTypeInfo typeInfo, MethodInfo imethod)
		{
			if (IsInline (imethod) && !IsVirtual (imethod) && typeInfo.Library.InlineMethodPolicy == InlineMethods.NotPresent)
				return MethodType.NotImplemented;
			else if (imethod.IsDefined (typeof (ConstructorAttribute), false))
				return MethodType.NativeCtor;
			else if (imethod.Name.Equals ("Alloc"))
				return MethodType.ManagedAlloc;
			else if (imethod.IsDefined (typeof (DestructorAttribute), false))
				return MethodType.NativeDtor;

			return MethodType.Native;
		}

		// The members below must be implemented for a given C++ ABI:

		public abstract CallingConvention? GetCallingConvention (MethodInfo methodInfo);
		protected abstract string GetMangledMethodName (CppTypeInfo typeInfo, MethodInfo methodInfo);

		// ---------------------------------

		private struct EmptyNativeLayout { }

		public virtual ICppClass ImplementClass (CppTypeInfo typeInfo)
		{
			if (typeInfo.WrapperType == null || !wrapper_to_typeinfo.ContainsKey (typeInfo.WrapperType)) {

				if (typeInfo.WrapperType != null)
					wrapper_to_typeinfo.Add (typeInfo.WrapperType, typeInfo);

				DefineImplType (typeInfo);

				var properties = GetProperties (typeInfo.InterfaceType);
				var methods = GetMethods (typeInfo.InterfaceType).Select (m => GetPInvokeSignature (typeInfo, m));
	
				// Implement all methods
				int vtableIndex = 0;
				foreach (var method in methods)
					DefineMethod (typeInfo, method, ref vtableIndex);

				// Implement all properties
				foreach (var property in properties)
					DefineProperty (typeInfo, property);

				typeInfo.emit_info.ctor_il.Emit (OpCodes.Ret);
				return (ICppClass)Activator.CreateInstance (typeInfo.emit_info.type_builder.CreateType (), typeInfo);
			}

			throw new InvalidOperationException ("This type has already been implemented");
		}

		public virtual CppTypeInfo MakeTypeInfo (CppLibrary lib, string typeName, Type interfaceType, Type layoutType/*?*/, Type/*?*/ wrapperType)
		{
			Debug.Assert (lib.Abi == this);
			return new CppTypeInfo (lib, typeName, interfaceType, layoutType ?? typeof (EmptyNativeLayout), wrapperType);
		}

		public virtual IEnumerable<PInvokeSignature> GetVirtualMethodSlots (CppTypeInfo typeInfo, Type interfaceType)
		{
			return from m in GetMethods (interfaceType)
	                where IsVirtual (m)
	                select GetPInvokeSignature (typeInfo, m);
		}

		protected virtual IEnumerable<MethodInfo> GetMethods (Type interfaceType)
		{
			// get all methods defined on inherited interfaces first
			var methods = (
				       from iface in interfaceType.GetInterfaces ()
			               from method in iface.GetMethods ()
			               where !method.IsSpecialName
				       select method
			              ).Concat (
			               from method in interfaceType.GetMethods ()
			               where !method.IsSpecialName
			               orderby method.MetadataToken
			               select method
			              );

			return methods;
		}

		protected virtual IEnumerable<PropertyInfo> GetProperties (Type interfaceType)
		{
			return ( // get all properties defined on the interface
			        from property in interfaceType.GetProperties ()
			        select property
			       ).Union ( // ... as well as those defined on inherited interfaces
			        from iface in interfaceType.GetInterfaces ()
			        from property in iface.GetProperties ()
			        select property
			       );
		}

		protected virtual void DefineImplType (CppTypeInfo typeInfo)
		{
			string implTypeName = typeInfo.InterfaceType.Name + "_";
			if (typeInfo.NativeLayout != null)
				implTypeName += typeInfo.NativeLayout.Name + "_";
			implTypeName += this.GetType ().Name + "_Impl";

			var impl_type = CppLibrary.interopModule.DefineType (implTypeName, TypeAttributes.Class | TypeAttributes.Sealed);
			impl_type.AddInterfaceImplementation (typeInfo.InterfaceType);

			var typeinfo_field = impl_type.DefineField ("_typeInfo", typeof (CppTypeInfo), FieldAttributes.InitOnly | FieldAttributes.Private);

			ConstructorBuilder ctor = impl_type.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard,
                                                                              new Type[] { typeof (CppTypeInfo) });

			var ctor_il = ctor.GetILGenerator ();

			// this._typeInfo = (CppTypeInfo passed to constructor)
			ctor_il.Emit (OpCodes.Ldarg_0);
			ctor_il.Emit (OpCodes.Ldarg_1);
			ctor_il.Emit (OpCodes.Stfld, typeinfo_field);

			typeInfo.emit_info.ctor_il = ctor_il;
			typeInfo.emit_info.typeinfo_field = typeinfo_field;
			typeInfo.emit_info.type_builder = impl_type;
		}

		protected virtual MethodBuilder DefineMethod (CppTypeInfo typeInfo, PInvokeSignature psig, ref int vtableIndex)
		{
			var interfaceMethod = psig.OrigMethod;

			// 1. Generate managed trampoline to call native method
			var trampoline = GetMethodBuilder (typeInfo, interfaceMethod);
			var il = typeInfo.emit_info.current_il = trampoline.GetILGenerator ();

			switch (psig.Type) {

			case MethodType.NotImplemented:
				il.Emit (OpCodes.Ldstr, "This method is not available.");
				il.Emit (OpCodes.Newobj, notimplementedexception);
				il.Emit (OpCodes.Throw);

				goto case MethodType.NoOp; // fallthrough
			case MethodType.NoOp:
				// return NULL if method is supposed to return a value
				if (!interfaceMethod.ReturnType.Equals (typeof (void)))
					il.Emit (OpCodes.Ldnull);
				il.Emit (OpCodes.Ret);
				return trampoline;

			case MethodType.ManagedAlloc:
				EmitManagedAlloc (typeInfo, interfaceMethod);
				il.Emit (OpCodes.Ret);
				return trampoline;
			}

			var isStatic = IsStatic (interfaceMethod);
			LocalBuilder cppInstancePtr = null;
			LocalBuilder nativePtr = null;

			// If we're an instance method, load up the "this" pointer
			if (!isStatic)
			{
				if (psig.ParameterTypes.Count == 0)
					throw new ArgumentException ("First argument to non-static C++ method must be instance pointer.");

				// 2. Load the native C++ instance pointer
				EmitLoadInstancePtr (il, interfaceMethod.GetParameters () [0].ParameterType, out cppInstancePtr, out nativePtr);

				// 3. Make sure our native pointer is a valid reference. If not, throw ObjectDisposedException
				EmitCheckDisposed (il, nativePtr, psig.Type);
			}

			MethodInfo nativeMethod;

			if (IsVirtual (interfaceMethod) && psig.Type != MethodType.NativeDtor) {
				nativeMethod = EmitPrepareVirtualCall (typeInfo, cppInstancePtr, vtableIndex++);
			} else {
				if (IsVirtual (interfaceMethod))
					vtableIndex++;

				nativeMethod = GetPInvokeForMethod (typeInfo, psig);
			}

			switch (psig.Type) {
			case MethodType.NativeCtor:
				EmitConstruct (typeInfo, nativeMethod, psig, cppInstancePtr, nativePtr);
				break;
			case MethodType.NativeDtor:
				EmitDestruct (typeInfo, nativeMethod, psig, cppInstancePtr, nativePtr);
				break;
			default:
				EmitNativeCall (typeInfo, nativeMethod, psig, nativePtr);
				break;
			}

			il.Emit (OpCodes.Ret);
			return trampoline;
		}

		protected virtual PropertyBuilder DefineProperty (CppTypeInfo typeInfo, PropertyInfo property)
		{
			if (property.CanWrite)
				throw new InvalidProgramException ("Properties in C++ interface must be read-only.");

			var imethod = property.GetGetMethod ();
			var methodName = imethod.Name;
			var propName = property.Name;
			var retType = imethod.ReturnType;

			var fieldProp = typeInfo.emit_info.type_builder.DefineProperty (propName, PropertyAttributes.None, retType, Type.EmptyTypes);

			var methodAttr = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			var fieldGetter = typeInfo.emit_info.type_builder.DefineMethod (methodName, methodAttr, retType, Type.EmptyTypes);
			var il = fieldGetter.GetILGenerator ();

			// C++ interface properties are either to return the CppTypeInfo or to access C++ fields
			if (retType.IsGenericType && retType.GetGenericTypeDefinition ().Equals (typeof (CppField<>))) {

				// define a new field for the property
				var fieldData = typeInfo.emit_info.type_builder.DefineField ("__" + propName + "_Data", retType, FieldAttributes.InitOnly | FieldAttributes.Private);

				// we need to lazy init the field because we don't have accurate field offset until after
				// all base classes have been added (by ctor)
				var lazyInit = il.DefineLabel ();

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, fieldData);
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse_S, lazyInit);
				il.Emit (OpCodes.Ret);

				// init new CppField
				il.MarkLabel (lazyInit);
				il.Emit (OpCodes.Pop);

				il.Emit (OpCodes.Ldarg_0);

				// first, get field offset
				//  = ((int)Marshal.OffsetOf (layout_type, propName)) + FieldOffsetPadding;
				il.Emit(OpCodes.Ldtoken, typeInfo.NativeLayout);
				il.Emit(OpCodes.Call, type_gettypefromhandle);
				il.Emit(OpCodes.Ldstr, propName);
				il.Emit(OpCodes.Call, marshal_offsetof);

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, typeInfo.emit_info.typeinfo_field);
				il.Emit (OpCodes.Callvirt, typeinfo_fieldoffset);

				il.Emit (OpCodes.Add);

				// new CppField<T> (<field offset>)
				il.Emit (OpCodes.Newobj, retType.GetConstructor (new Type[] { typeof(int) }));

				il.Emit (OpCodes.Stfld, fieldData);

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, fieldData);
				il.Emit (OpCodes.Ret);

			} else if (retType.Equals (typeof (CppTypeInfo))) {
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, typeInfo.emit_info.typeinfo_field);
				il.Emit (OpCodes.Ret);
			} else
				throw new InvalidProgramException ("Properties in C++ interface can only be of type CppField.");

			fieldProp.SetGetMethod (fieldGetter);

			return fieldProp;
		}

		/**
		 * Implements the managed trampoline that will be invoked from the vtable by native C++ code when overriding
		 *  the specified C++ virtual method with the specified managed one.
		 */
		// FIXME: This should be moved into CppTypeInfo class
		internal virtual Delegate GetManagedOverrideTrampoline (CppTypeInfo typeInfo, int vtableIndex)
		{
			if (typeInfo.WrapperType == null)
				return null;

			var sig = typeInfo.VirtualMethods [vtableIndex];
			if (sig == null)
				return null;

			var interfaceMethod = sig.OrigMethod;
			var targetMethod = FindManagedOverrideTarget (typeInfo.WrapperType, interfaceMethod);
			if (targetMethod == null)
				return null;

			var interfaceArgs = ReflectionHelper.GetMethodParameterTypes (interfaceMethod);
			var nativeArgs = sig.ParameterTypes.ToArray ();

			// TODO: According to http://msdn.microsoft.com/en-us/library/w16z8yc4.aspx
			// The dynamic method created with this constructor has access to public and internal members of all the types contained in module m.
			// This does not appear to hold true, so we also disable JIT visibility checks.
			var trampolineIn = new DynamicMethod (typeInfo.WrapperType.Name + "_" + interfaceMethod.Name + "_FromNative", sig.ReturnType,
			                                      nativeArgs, typeof (CppInstancePtr).Module, true);

			ReflectionHelper.ApplyMethodParameterAttributes (interfaceMethod, trampolineIn, true);
			var il = trampolineIn.GetILGenerator ();

			// for static (target) methods:
			OpCode callInstruction = OpCodes.Call;
			int argLoadStart = IsStatic (interfaceMethod)? 0 : 1; // chop off C++ instance ptr if there is one

			// for instance methods, we need a managed instance to call them on!
			if (!targetMethod.IsStatic) {
				callInstruction = OpCodes.Callvirt;
				argLoadStart = 1;

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldc_I4, typeInfo.GCHandleOffset);

				var getManagedObj = cppip_tomanaged_size.MakeGenericMethod (typeInfo.WrapperType);
				il.Emit (OpCodes.Call, getManagedObj);
			}

			for (int i = argLoadStart; i < interfaceArgs.Length; i++) {
				il.Emit (OpCodes.Ldarg, i);
				EmitInboundMarshal (il, nativeArgs [i], interfaceArgs [i]);
			}

			il.Emit (callInstruction, targetMethod);
			EmitOutboundMarshal (il, targetMethod.ReturnType, sig.ReturnType);
			il.Emit (OpCodes.Ret);

			return trampolineIn.CreateDelegate (typeInfo.VTableDelegateTypes [vtableIndex]);
		}

		protected virtual MethodInfo FindManagedOverrideTarget (Type wrapper, MethodInfo interfaceMethod)
		{
			if (interfaceMethod == null)
				return null;

			var possibleMembers = wrapper.FindMembers (MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic |
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
		protected virtual MethodBuilder GetMethodBuilder (CppTypeInfo typeInfo, MethodInfo interfaceMethod)
		{
			var parameterTypes = ReflectionHelper.GetMethodParameterTypes (interfaceMethod);
			var methodBuilder = typeInfo.emit_info.type_builder.DefineMethod (interfaceMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual,
															                  interfaceMethod.ReturnType, parameterTypes);
			ReflectionHelper.ApplyMethodParameterAttributes (interfaceMethod, methodBuilder, false);
			return methodBuilder;
		}

		/**
		 * Defines a new MethodBuilder that calls the specified C++ (non-virtual) method using its mangled name
		 */
		protected virtual MethodBuilder GetPInvokeForMethod (CppTypeInfo typeInfo, PInvokeSignature sig)
		{
			var entryPoint = sig.Name;
			if (entryPoint == null)
				throw new NotSupportedException ("Could not mangle method name.");

			string lib;
			if (IsInline (sig.OrigMethod) && typeInfo.Library.InlineMethodPolicy == InlineMethods.SurrogateLib)
				lib = typeInfo.Library.Name + "-inline";
			else
				lib = typeInfo.Library.Name;

			var builder = typeInfo.emit_info.type_builder.DefinePInvokeMethod (entryPoint, lib, entryPoint,
			                                             MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
			                                             CallingConventions.Standard, sig.ReturnType, sig.ParameterTypes.ToArray (),
			                                             sig.CallingConvention.Value, CharSet.Ansi);
			builder.SetImplementationFlags (builder.GetMethodImplementationFlags () | MethodImplAttributes.PreserveSig);
			ReflectionHelper.ApplyMethodParameterAttributes (sig.OrigMethod, builder, true);
			return builder;
		}

		/**
		 * Emits the IL to load the correct delegate instance and/or retrieve the MethodInfo from the VTable
		 * for a C++ virtual call.
		 */
		protected virtual MethodInfo EmitPrepareVirtualCall (CppTypeInfo typeInfo, LocalBuilder cppInstancePtr, int vtableIndex)
		{
			var il = typeInfo.emit_info.current_il;
			var vtableDelegateType = typeInfo.VTableDelegateTypes [vtableIndex];
			var getDelegate  = typeinfo_adjvcall.MakeGenericMethod (vtableDelegateType);

			// this._typeInfo.GetAdjustedVirtualCall<T> (cppInstancePtr, vtableIndex);
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, typeInfo.emit_info.typeinfo_field);
			il.Emit (OpCodes.Ldloc_S, cppInstancePtr);
			il.Emit (OpCodes.Ldc_I4, vtableIndex);
			il.Emit (OpCodes.Callvirt, getDelegate);

			return ReflectionHelper.GetMethodInfoForDelegate (vtableDelegateType);
		}

		/**
		 *  Emits IL to allocate the memory for a new instance of the C++ class.
		 *  To complete method, emit OpCodes.Ret.
		 */
		protected virtual void EmitManagedAlloc (CppTypeInfo typeInfo, MethodInfo interfaceMethod)
		{
			var il = typeInfo.emit_info.current_il;

			// this._typeInfo.NativeSize
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, typeInfo.emit_info.typeinfo_field);

			if (typeInfo.WrapperType != null && interfaceMethod.GetParameters ().Any ()) {
				// load managed wrapper
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Newobj, cppip_fromtype_managed);
			} else {
				il.Emit (OpCodes.Callvirt, typeinfo_nativesize);
				il.Emit (OpCodes.Newobj, cppip_fromsize);
			}
		}

		protected virtual void EmitConstruct (CppTypeInfo typeInfo, MethodInfo nativeMethod, PInvokeSignature psig,
		                                      LocalBuilder cppInstancePtr, LocalBuilder nativePtr)
		{
			Debug.Assert (psig.Type == MethodType.NativeCtor);
			var il = typeInfo.emit_info.current_il;

			EmitNativeCall (typeInfo, nativeMethod, psig, nativePtr);

			if (cppInstancePtr != null && psig.OrigMethod.ReturnType == typeof (CppInstancePtr)) {
				EmitInitVTable (typeInfo, cppInstancePtr);
				il.Emit (OpCodes.Ldloc_S, cppInstancePtr);

			} else if (psig.OrigMethod.DeclaringType.GetInterfaces ().Any (i => i.IsGenericType && i.GetGenericTypeDefinition () == typeof (ICppClassOverridable<>))) {
				throw new InvalidProgramException ("In ICppClassOverridable, native constructors must take as first argument and return CppInstancePtr");
			}
		}

		protected virtual void EmitDestruct (CppTypeInfo typeInfo, MethodInfo nativeMethod, PInvokeSignature psig,
		                                     LocalBuilder cppInstancePtr, LocalBuilder nativePtr)
		{
			Debug.Assert (psig.Type == MethodType.NativeDtor);
			var il = typeInfo.emit_info.current_il;

			// we don't do anything if the object wasn't managed alloc
			if (cppInstancePtr == null)
				return;

			EmitCheckManagedAlloc (il, cppInstancePtr);

			EmitResetVTable (typeInfo, cppInstancePtr);
			EmitNativeCall (typeInfo, nativeMethod, psig, nativePtr);
		}

		/**
		 * Emits IL to call the native method. nativeMethod should be either a method obtained by
		 * GetPInvokeForMethod or the MethodInfo of a vtable method.
		 * To complete method, emit OpCodes.Ret.
		 */
		protected virtual void EmitNativeCall (CppTypeInfo typeInfo, MethodInfo nativeMethod, PInvokeSignature psig, LocalBuilder nativePtr)
		{
			var il = typeInfo.emit_info.current_il;
			var interfaceMethod = psig.OrigMethod;
			var interfaceArgs = interfaceMethod.GetParameters ();

			int argLoadStart = 1; // For static methods, just strip off arg0 (.net this pointer)
			if (!IsStatic (interfaceMethod))
			{
				argLoadStart = 2; // For instance methods, strip off CppInstancePtr and pass the corresponding IntPtr
				il.Emit (OpCodes.Ldloc_S, nativePtr);
			}

			// load and marshal arguments
			for (int i = argLoadStart; i <= interfaceArgs.Length; i++) {
				il.Emit (OpCodes.Ldarg, i);
				EmitOutboundMarshal (il, interfaceArgs [i - 1].ParameterType, psig.ParameterTypes [i - 1]);
			}

			il.Emit (OpCodes.Call, nativeMethod);

			// Marshal return value
			if (psig.Type != MethodType.NativeCtor)
				EmitInboundMarshal (il, psig.ReturnType, interfaceMethod.ReturnType);
		}


		public virtual PInvokeSignature GetPInvokeSignature (CppTypeInfo typeInfo, MethodInfo method)
		{
			var methodType = GetMethodType (typeInfo, method);
			var parameters = method.GetParameters ();
			var pinvokeTypes = new List<Type> (parameters.Length);

			foreach (var pi in parameters) {
				pinvokeTypes.Add (ToPInvokeType (pi.ParameterType, pi));
			}

			return new PInvokeSignature {
				OrigMethod = method,
				Name = GetMangledMethodName (typeInfo, method),
				Type = methodType,
				CallingConvention = GetCallingConvention (method),
				ParameterTypes = pinvokeTypes,
				ReturnType = methodType == MethodType.NativeCtor? typeof (void) :
					ToPInvokeType (method.ReturnType, method.ReturnTypeCustomAttributes)
			};
		}

		public virtual Type ToPInvokeType (Type t, ICustomAttributeProvider icap)
		{
			if (t == typeof (bool)) {
				return typeof (byte);

			} else if (typeof (ICppObject).IsAssignableFrom (t)) {

				if (IsByVal (icap)) {

					return GetTypeInfo (t).NativeLayout;

				} else { // by ref

					return typeof (IntPtr);
				}
			}

			return t;
		}

		// The above should return parameter/return types that
		// correspond with the marshalling implemented in the next 2 methods.

		// This method marshals from managed -> C++
		// The value it is marshaling will be on the stack.
		protected virtual void EmitOutboundMarshal (ILGenerator il, Type managedType, Type targetType)
		{
			var nextArg = il.DefineLabel ();

			// FIXME: Why is this needed ?
			// auto marshal bool to C++ bool type (0 = false , 1 = true )
			if (managedType.Equals (typeof (bool))) {
				Label isTrue = il.DefineLabel ();
				Label done = il.DefineLabel ();
				il.Emit (OpCodes.Brtrue, isTrue);
				il.Emit (OpCodes.Ldc_I4_0);
				il.Emit (OpCodes.Br, done);
				il.MarkLabel (isTrue);
				il.Emit (OpCodes.Ldc_I4_1);
				il.MarkLabel (done);
				//il.Emit (OpCodes.Conv_I1);
			}

			// auto marshal ICppObject
			if (typeof (ICppObject).IsAssignableFrom (managedType)) {

				var nullCase = il.DefineLabel ();
				var param = il.DeclareLocal (typeof (CppInstancePtr));

				// check for null
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse_S, nullCase);

				// FIXME: ICppObject could be implemented by a value type
				il.Emit (OpCodes.Callvirt, cppobj_native);
				il.Emit (OpCodes.Stloc, param);

				if (targetType == typeof (IntPtr)) { // by ref
					il.Emit (OpCodes.Ldloca, param);
					il.Emit (OpCodes.Call, cppip_native);

					il.Emit (OpCodes.Br_S, nextArg);

					il.MarkLabel (nullCase);
					// Null case
					il.Emit (OpCodes.Pop);
					il.Emit (OpCodes.Ldsfld, intptr_zero);

				} else { // by val

					var val = il.DeclareLocal (targetType);
					il.Emit (OpCodes.Ldloca, val); // dest

					il.Emit (OpCodes.Ldloca, param);
					il.Emit (OpCodes.Call, cppip_native); // src

					il.Emit (OpCodes.Cpobj, targetType);
					il.Emit (OpCodes.Ldloc, val);

					il.Emit (OpCodes.Br_S, nextArg);

					il.MarkLabel (nullCase);
					// Null case
					il.Emit (OpCodes.Ldstr, "Cannot pass null object of type '" + managedType + "' to c++ by value");
					il.Emit (OpCodes.Newobj, typeof (ArgumentException).GetConstructor (new Type[] { typeof(string) }));
					il.Emit (OpCodes.Throw);
				}
			}

			il.MarkLabel (nextArg);
		}


		// This method marshals from C++ -> managed
		// The value it is marshaling will be on the stack.
		protected virtual void EmitInboundMarshal (ILGenerator il, Type nativeType, Type targetType)
		{
			if (nativeType == typeof (void))
				return; // <- yes, this is necessary

			var next = il.DefineLabel ();
			var ptr = il.DeclareLocal (typeof (CppInstancePtr));

			// marshal IntPtr -> ICppObject
			if (nativeType == typeof (IntPtr) && typeof (ICppObject).IsAssignableFrom (targetType)) {

				var isNull = il.DefineLabel ();

				// first, we check for null
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse_S, isNull);

				il.Emit (OpCodes.Newobj, cppip_fromnative);
				il.Emit (OpCodes.Stloc, ptr);
				EmitCreateCppObjectFromNative (il, targetType, ptr);
				il.Emit (OpCodes.Br_S, next);

				il.MarkLabel (isNull);
				il.Emit (OpCodes.Pop);
				il.Emit (OpCodes.Ldnull);

			} else if (nativeType.IsValueType && typeof (ICppObject).IsAssignableFrom (targetType)) {
				// marshal value type -> ICppObject

				// Obviously, we lose all managed overrides if we pass by value,
				//  but this "slicing" happens in vanilla C++ as well

				il.Emit (OpCodes.Box, nativeType); // structure

				il.Emit (OpCodes.Sizeof, nativeType);
				il.Emit (OpCodes.Newobj, cppip_fromsize);
				il.Emit (OpCodes.Stloc, ptr);
				il.Emit (OpCodes.Ldloca, ptr);
				il.Emit (OpCodes.Call, cppip_native); // ptr

				il.Emit (OpCodes.Ldc_I4_0); // fDeleteOld

				il.Emit (OpCodes.Call, marshal_structuretoptr);
				EmitCreateCppObjectFromNative (il, targetType, ptr);
			}

			il.MarkLabel (next);
		}

		// Gets a typeinfo for another ICppObject.
		protected virtual CppTypeInfo GetTypeInfo (Type otherWrapperType)
		{
			CppTypeInfo info;
			if (wrapper_to_typeinfo.TryGetValue (otherWrapperType, out info))
				return info;
	
			// pass a "dummy" type info to subclass ctor to trigger the creation of the real one
			try {
				Activator.CreateInstance (otherWrapperType, (CppTypeInfo)(new DummyCppTypeInfo ()));

			} catch (MissingMethodException) {

				throw new InvalidProgramException (string.Format ("Type `{0}' implements ICppObject but does not contain a public constructor that takes CppTypeInfo", otherWrapperType));
			}

			return wrapper_to_typeinfo [otherWrapperType];
		}



		// Expects cppip = CppInstancePtr local
		protected virtual void EmitCreateCppObjectFromNative (ILGenerator il, Type targetType, LocalBuilder cppip)
		{
			if (targetType == typeof (ICppObject))
				targetType = typeof (CppInstancePtr);

			il.Emit (OpCodes.Ldloca, cppip);
			il.Emit (OpCodes.Call, cppip_native);

			// check for a native constructor (i.e. a public ctor in the wrapper that takes CppInstancePtr)
			if (typeof (ICppObject).IsAssignableFrom (targetType)) {
				var ctor = targetType.GetConstructor (BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Instance, null, new Type [] { typeof (CppInstancePtr) }, null);
				if (ctor == null)
					throw new InvalidProgramException (string.Format ("Type `{0}' implements ICppObject but does not contain a public constructor that takes CppInstancePtr", targetType));

				// Basically emitting this:
				// CppInstancePtr.ToManaged<targetType> (native) ?? new targetType (native)
	
				var hasWrapper = il.DefineLabel ();
	
				il.Emit (OpCodes.Call, cppip_tomanaged.MakeGenericMethod (targetType));
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brtrue_S, hasWrapper);
				il.Emit (OpCodes.Pop);
	
				il.Emit (OpCodes.Ldloc, cppip);
				il.Emit (OpCodes.Newobj, ctor);
	
				il.MarkLabel (hasWrapper);

			} else if (targetType.IsValueType) {

				il.Emit (OpCodes.Ldtoken, targetType);
				il.Emit (OpCodes.Call, type_gettypefromhandle);
				il.Emit (OpCodes.Call, marshal_ptrtostructure);
				il.Emit (OpCodes.Unbox_Any, targetType);
			}
		}

		/**
		 * Emits IL to load the VTable object onto the stack.
		 */
		protected virtual void EmitLoadVTable (CppTypeInfo typeInfo)
		{
			var il = typeInfo.emit_info.current_il;

			// this._typeInfo.VTable
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, typeInfo.emit_info.typeinfo_field);
			il.Emit (OpCodes.Callvirt, typeinfo_vtable);
		}

		/**
		 * Emits IL to set the vtable pointer of the instance (if class has a vtable).
		 * This should usually happen in the managed wrapper of the C++ instance constructor.
		 */
		protected virtual void EmitInitVTable (CppTypeInfo typeInfo, LocalBuilder cppip)
		{
			var il = typeInfo.emit_info.current_il;

			// this._typeInfo.VTable.InitInstance (cppInstancePtr);
			EmitLoadVTable (typeInfo);
			il.Emit (OpCodes.Ldloca_S, cppip);
			EmitCallVTableMethod (typeInfo, vtable_initinstance, 2, false);
		}

		protected virtual void EmitResetVTable (CppTypeInfo typeInfo, LocalBuilder cppip)
		{
			var il = typeInfo.emit_info.current_il;

			// this._typeInfo.VTable.ResetInstance (cppInstancePtr);
			EmitLoadVTable (typeInfo);
			il.Emit (OpCodes.Ldloc_S, cppip);
			EmitCallVTableMethod (typeInfo, vtable_resetinstance, 2, false);
		}

		/**
		 * A utility function to emit the IL for a vtable-dependant operation.
		 * In other words, classes with no virtual methods will not have vtables,
		 * so this method emits code to check for that and either throw an exception
		 * or do nothing if no vtable exists. To use, push the arguments to the method you
		 * want to call and pass the stackHeight for the call. If no vtable exists, this method
		 * will emit code to pop the arguments off the stack.
		 */
		protected virtual void EmitCallVTableMethod (CppTypeInfo typeInfo, MethodInfo method, int stackHeight, bool throwOnNoVTable)
		{
			var il = typeInfo.emit_info.current_il;

			// prepare a jump; do not call vtable method if no vtable
			var noVirt = il.DefineLabel ();
			var dontPushOrThrow = il.DefineLabel ();

			EmitLoadVTable (typeInfo);
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
				il.Emit (OpCodes.Call, cppip_native);
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
			var managedAlloc = il.DefineLabel ();

			il.Emit (OpCodes.Ldloca_S, cppip);
			il.Emit (OpCodes.Call, cppip_managedalloc);
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
			var validRef = il.DefineLabel ();

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
