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
using System.Diagnostics;

using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop.ABI {

	//FIXME: Exception handling, operator overloading etc.
	//FIXME: Allow interface to override default calling convention

	// Subclasses should be singletons
	public abstract partial class CppAbi {
		// (other part of this partial class in Attributes.cs)

		// These fields are specific to the class we happen to be implementing:
		protected ModuleBuilder impl_module;
		protected TypeBuilder impl_type;

		protected Type interface_type, layout_type, wrapper_type;
		protected CppLibrary library;
		protected string class_name;

		protected FieldBuilder typeinfo_field;
		protected ILGenerator ctor_il;

		// These fields are specific to the ABI:
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
		protected static readonly MethodInfo cppip_getmanaged      = typeof (CppInstancePtr).GetMethod ("GetManaged", BindingFlags.Static | BindingFlags.NonPublic);
		protected static readonly ConstructorInfo cppip_fromnative = typeof (CppInstancePtr).GetConstructor (new Type [] { typeof (IntPtr) });
		protected static readonly ConstructorInfo cppip_fromsize   = typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null, new Type [] { typeof (int) }, null);
		protected static readonly ConstructorInfo cppip_fromsize_managed = typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
		                                                                                                         new Type[] { typeof (int), typeof (object) }, null);
		protected static readonly ConstructorInfo notimplementedexception = typeof (NotImplementedException).GetConstructor (new Type [] { typeof (string) });
		protected static readonly MethodInfo type_gettypefromhandle  = typeof (Type).GetMethod ("GetTypeFromHandle");
		protected static readonly MethodInfo marshal_offsetof        = typeof (Marshal).GetMethod ("OffsetOf");
		protected static readonly MethodInfo marshal_structuretoptr  = typeof (Marshal).GetMethod ("StructureToPtr");
		protected static readonly ConstructorInfo dummytypeinfo_ctor = typeof (DummyCppTypeInfo).GetConstructor (Type.EmptyTypes);
		protected static readonly MethodInfo dummytypeinfo_getbase   = typeof (DummyCppTypeInfo).GetProperty ("BaseTypeInfo").GetGetMethod ();


		// These methods might be more commonly overridden for a given C++ ABI:

		public virtual MethodType GetMethodType (MethodInfo imethod)
		{
			if (IsInline (imethod) && library.InlineMethodPolicy == InlineMethods.NotPresent)
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
		protected abstract string GetMangledMethodName (MethodInfo methodInfo);
		public string GetMangledMethodName (string className, MethodInfo methodInfo)
		{
			class_name = className;
			return GetMangledMethodName (methodInfo);
		}

		// The ImplementClass overrides are the main entry point to the Abi API:

		private struct EmptyNativeLayout { }
		public Iface ImplementClass<Iface> (Type wrapperType, CppLibrary lib, string className)
			where Iface : ICppClass
		{
			return this.ImplementClass<Iface,EmptyNativeLayout> (wrapperType, lib, className);
		}

		public virtual Iface ImplementClass<Iface, NLayout> (Type wrapperType, CppLibrary lib, string className)
			where NLayout : struct
			where Iface : ICppClass
		{
			this.impl_module = CppLibrary.interopModule;
			this.library = lib;
			this.class_name = className;
			this.interface_type = typeof (Iface);
			this.layout_type = typeof (NLayout);
			this.wrapper_type = wrapperType;

			DefineImplType ();

			var properties = GetProperties ();
			var methods = GetMethods ().Select (m => GetPInvokeSignature (m));

 			var typeInfo = MakeTypeInfo (methods);
			if (wrapperType != null)
				wrapper_to_typeinfo.Add (wrapperType, typeInfo);

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

		protected virtual CppTypeInfo MakeTypeInfo (IEnumerable<PInvokeSignature> methods)
		{
			return new CppTypeInfo (this, methods.Where (m => IsVirtual (m.OrigMethod)), layout_type, wrapper_type);
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

			typeinfo_field = impl_type.DefineField ("_typeInfo", typeof (CppTypeInfo), FieldAttributes.InitOnly | FieldAttributes.Private);

			ConstructorBuilder ctor = impl_type.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard,
                                                                              new Type[] { typeof (CppTypeInfo) });

			ctor_il = ctor.GetILGenerator ();

			// this._typeInfo = (CppTypeInfo passed to constructor)
			ctor_il.Emit (OpCodes.Ldarg_0);
			ctor_il.Emit (OpCodes.Ldarg_1);
			ctor_il.Emit (OpCodes.Stfld, typeinfo_field);
		}

		protected virtual MethodBuilder DefineMethod (PInvokeSignature psig, CppTypeInfo typeInfo, ref int vtableIndex)
		{
			var interfaceMethod = psig.OrigMethod;

			// 1. Generate managed trampoline to call native method
			MethodBuilder trampoline = GetMethodBuilder (interfaceMethod);
			ILGenerator il = trampoline.GetILGenerator ();

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
				if (psig.ParameterTypes.Count == 0)
					throw new ArgumentException ("First argument to non-static C++ method must be instance pointer.");

				// 2. Load the native C++ instance pointer
				EmitLoadInstancePtr (il, interfaceMethod.GetParameters () [0].ParameterType, out cppInstancePtr, out nativePtr);

				// 3. Make sure our native pointer is a valid reference. If not, throw ObjectDisposedException
				EmitCheckDisposed (il, nativePtr, psig.Type);
			}

			MethodInfo nativeMethod;

			if (IsVirtual (interfaceMethod) && psig.Type != MethodType.NativeDtor) {
				nativeMethod = EmitPrepareVirtualCall (il, typeInfo, cppInstancePtr, vtableIndex++);
			} else {
				if (IsVirtual (interfaceMethod))
					vtableIndex++;

				nativeMethod = GetPInvokeForMethod (psig);
			}

			switch (psig.Type) {
			case MethodType.NativeCtor:
				EmitConstruct (il, nativeMethod, psig, cppInstancePtr, nativePtr);
				break;
			case MethodType.NativeDtor:
				EmitDestruct (il, nativeMethod, psig, cppInstancePtr, nativePtr);
				break;
			default:
				EmitNativeCall (il, nativeMethod, psig, nativePtr);
				break;
			}

			il.Emit (OpCodes.Ret);
			return trampoline;
		}

		protected virtual PropertyBuilder DefineProperty (PropertyInfo property)
		{
			if (property.CanWrite)
				throw new InvalidProgramException ("Properties in C++ interface must be read-only.");

			var imethod = property.GetGetMethod ();
			var methodName = imethod.Name;
			var propName = property.Name;
			var retType = imethod.ReturnType;

			var fieldProp = impl_type.DefineProperty (propName, PropertyAttributes.None, retType, Type.EmptyTypes);

			var methodAttr = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			var fieldGetter = impl_type.DefineMethod (methodName, methodAttr, retType, Type.EmptyTypes);
			var il = fieldGetter.GetILGenerator ();

			// C++ interface properties are either to return the CppTypeInfo or to access C++ fields
			if (retType.IsGenericType && retType.GetGenericTypeDefinition ().Equals (typeof (CppField<>))) {

				// define a new field for the property
				var fieldData = impl_type.DefineField ("__" + propName + "_Data", retType, FieldAttributes.InitOnly | FieldAttributes.Private);

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
				il.Emit(OpCodes.Ldtoken, layout_type);
				il.Emit(OpCodes.Call, type_gettypefromhandle);
				il.Emit(OpCodes.Ldstr, propName);
				il.Emit(OpCodes.Call, marshal_offsetof);

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, typeinfo_field);
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
				il.Emit (OpCodes.Ldfld, typeinfo_field);
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
			ILGenerator il = trampolineIn.GetILGenerator ();

			// for static (target) methods:
			OpCode callInstruction = OpCodes.Call;
			int argLoadStart = IsStatic (interfaceMethod)? 0 : 1; // chop off C++ instance ptr if there is one

			// for instance methods, we need a managed instance to call them on!
			if (!targetMethod.IsStatic) {
				callInstruction = OpCodes.Callvirt;
				argLoadStart = 1;

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldc_I4, typeInfo.NativeSize);

				var getManagedObj = cppip_getmanaged.MakeGenericMethod (typeInfo.WrapperType);
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
		protected virtual MethodBuilder GetPInvokeForMethod (PInvokeSignature sig)
		{
			string entryPoint = sig.Name;
			if (entryPoint == null)
				throw new NotSupportedException ("Could not mangle method name.");

			string lib;
			if (IsInline (sig.OrigMethod) && library.InlineMethodPolicy == InlineMethods.SurrogateLib)
				lib = library.Name + "-inline";
			else
				lib = library.Name;

			MethodBuilder builder = impl_type.DefinePInvokeMethod (entryPoint, lib, entryPoint,
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
		protected virtual MethodInfo EmitPrepareVirtualCall (ILGenerator il, CppTypeInfo typeInfo,
		                                                     LocalBuilder cppInstancePtr, int vtableIndex)
		{
			Type vtableDelegateType = typeInfo.VTableDelegateTypes [vtableIndex];
			MethodInfo getDelegate  = typeinfo_adjvcall.MakeGenericMethod (vtableDelegateType);

			// this._typeInfo.GetAdjustedVirtualCall<T> (cppInstancePtr, vtableIndex);
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldfld, typeinfo_field);
			il.Emit (OpCodes.Ldloc_S, cppInstancePtr);
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

			if (wrapper_type != null && interfaceMethod.GetParameters ().Any ()) {
				// load managed wrapper
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Newobj, cppip_fromsize_managed);
			} else
				il.Emit (OpCodes.Newobj, cppip_fromsize);
		}

		protected virtual void EmitConstruct (ILGenerator il, MethodInfo nativeMethod, PInvokeSignature psig,
		                                      LocalBuilder cppInstancePtr, LocalBuilder nativePtr)
		{
			Debug.Assert (psig.Type == MethodType.NativeCtor);
			EmitNativeCall (il, nativeMethod, psig, nativePtr);

			if (cppInstancePtr != null && psig.OrigMethod.ReturnType == typeof (CppInstancePtr)) {
				EmitInitVTable (il, cppInstancePtr);
				il.Emit (OpCodes.Ldloc_S, cppInstancePtr);

			} else if (psig.OrigMethod.DeclaringType.GetInterfaces ().Any (i => i.IsGenericType && i.GetGenericTypeDefinition () == typeof (ICppClassOverridable<>))) {
				throw new InvalidProgramException ("In ICppClassOverridable, native constructors must take as first argument and return CppInstancePtr");
			}
		}

		protected virtual void EmitDestruct (ILGenerator il, MethodInfo nativeMethod, PInvokeSignature psig,
		                                     LocalBuilder cppInstancePtr, LocalBuilder nativePtr)
		{
			Debug.Assert (psig.Type == MethodType.NativeDtor);

			// we don't do anything if the object wasn't managed alloc
			if (cppInstancePtr == null)
				return;

			EmitCheckManagedAlloc (il, cppInstancePtr);

			EmitResetVTable (il, cppInstancePtr);
			EmitNativeCall (il, nativeMethod, psig, nativePtr);
		}

		/**
		 * Emits IL to call the native method. nativeMethod should be either a method obtained by
		 * GetPInvokeForMethod or the MethodInfo of a vtable method.
		 * To complete method, emit OpCodes.Ret.
		 */
		protected virtual void EmitNativeCall (ILGenerator il, MethodInfo nativeMethod, PInvokeSignature psig, LocalBuilder nativePtr)
		{
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


		public virtual PInvokeSignature GetPInvokeSignature (MethodInfo method)
		{
			var methodType = GetMethodType (method);
			var parameters = method.GetParameters ();
			var pinvokeTypes = new List<Type> (parameters.Length);

			foreach (var pi in parameters) {
				pinvokeTypes.Add (ToPInvokeType (pi.ParameterType, pi));
			}

			return new PInvokeSignature {
				OrigMethod = method,
				Name = GetMangledMethodName (method),
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

					var typeInfo = GetTypeInfo (t);
					return typeInfo != null? typeInfo.NativeLayout : layout_type;

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

					il.MarkLabel (nullCase);

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

			// marshal IntPtr -> ICppObject
			if (nativeType == typeof (IntPtr) && typeof (ICppObject).IsAssignableFrom (targetType)) {

				var isNull = il.DefineLabel ();

				// first, we check for null
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Brfalse_S, isNull);

				il.Emit (OpCodes.Newobj, cppip_fromnative);
				EmitCreateCppObjectFromNative (il, targetType);

				il.MarkLabel (isNull);
				il.Emit (OpCodes.Pop);
				il.Emit (OpCodes.Ldnull);

			} else if (nativeType.IsValueType && typeof (ICppObject).IsAssignableFrom (targetType)) {
				// marshal value type -> ICppObject

				// Obviously, we lose all managed overrides if we pass by value,
				//  but this "slicing" happens in vanilla C++ as well

				var ptr = il.DeclareLocal (typeof (CppInstancePtr));

				il.Emit (OpCodes.Box, nativeType); // structure

				il.Emit (OpCodes.Sizeof, nativeType);
				il.Emit (OpCodes.Newobj, cppip_fromsize);
				il.Emit (OpCodes.Stloc, ptr);
				il.Emit (OpCodes.Ldloca, ptr);
				il.Emit (OpCodes.Call, cppip_native); // ptr

				il.Emit (OpCodes.Ldc_I4_0); // fDeleteOld

				il.Emit (OpCodes.Call, marshal_structuretoptr);

				il.Emit (OpCodes.Ldloc, ptr);
				EmitCreateCppObjectFromNative (il, targetType);
			}

			il.MarkLabel (next);
		}

		// Gets a typeinfo for another ICppObject.
		// Might return null for the type we're currently emitting.
		protected virtual CppTypeInfo GetTypeInfo (Type otherWrapperType)
		{
			CppTypeInfo info;
			if (wrapper_to_typeinfo.TryGetValue (otherWrapperType, out info))
				return info;

			if (otherWrapperType == wrapper_type)
				return null;

			// pass a "dummy" type info to subclass ctor to trigger the creation of the real one
			try {
				Activator.CreateInstance (otherWrapperType, (CppTypeInfo)(new DummyCppTypeInfo ()));

			} catch (MissingMethodException mme) {

				throw new InvalidProgramException (string.Format ("Type `{0}' implements ICppObject but does not contain a public constructor that takes CppTypeInfo", otherWrapperType));
			}

			return wrapper_to_typeinfo [otherWrapperType];
		}

		// Does the above passthru at runtime.
		// This is perhaps a kludgy/roundabout way for pulling the type info for another
		//  ICppObject at runtime, but I think this keeps the wrappers clean.
		protected virtual void EmitGetTypeInfo (ILGenerator il, Type targetType)
		{
			// check for a subclass constructor (i.e. a public ctor in the wrapper that takes CppTypeInfo)
			var ctor = targetType.GetConstructor (BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Instance, null, new Type [] { typeof (CppTypeInfo) }, null);
			if (ctor == null)
				throw new InvalidProgramException (string.Format ("Type `{0}' implements ICppObject but does not contain a public constructor that takes CppTypeInfo", targetType));

			il.Emit (OpCodes.Newobj, dummytypeinfo_ctor);
			il.Emit (OpCodes.Dup);
			il.Emit (OpCodes.Newobj, ctor);
			il.Emit (OpCodes.Pop);
			il.Emit (OpCodes.Call, dummytypeinfo_getbase);
		}

		// Expects CppInstancePtr on stack. No null check performed
		protected virtual void EmitCreateCppObjectFromNative (ILGenerator il, Type targetType)
		{
			if (targetType == typeof (ICppObject))
				targetType = typeof (CppInstancePtr);

			// check for a native constructor (i.e. a public ctor in the wrapper that takes CppInstancePtr)
			var ctor = targetType.GetConstructor (BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Instance, null, new Type [] { typeof (CppInstancePtr) }, null);
			if (ctor == null)
				throw new InvalidProgramException (string.Format ("Type `{0}' implements ICppObject but does not contain a public constructor that takes CppInstancePtr", targetType));

			il.Emit (OpCodes.Newobj, ctor);
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
		protected virtual void EmitInitVTable (ILGenerator il, LocalBuilder cppip)
		{
			// this._typeInfo.VTable.InitInstance (cppInstancePtr);
			EmitLoadVTable (il);
			il.Emit (OpCodes.Ldloca_S, cppip);
			EmitCallVTableMethod (il, vtable_initinstance, 2, false);
		}

		protected virtual void EmitResetVTable (ILGenerator il, LocalBuilder cppip)
		{
			// this._typeInfo.VTable.ResetInstance (cppInstancePtr);
			EmitLoadVTable (il);
			il.Emit (OpCodes.Ldloc_S, cppip);
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
			Label managedAlloc = il.DefineLabel ();
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
