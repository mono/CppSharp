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

namespace Mono.VisualC.Interop.ABI {

        //TODO: Exception handling, operator overloading etc.
        //TODO: Allow interface to override default calling convention
        public abstract class CppAbi {

                protected ModuleBuilder implModule;
                protected TypeBuilder implType;

                protected Type interfaceType, layoutType, wrapperType;
                protected string library, className;

                protected VTable vtable;
                protected FieldBuilder vtableField;
                protected ILGenerator ctorIL;

                public virtual Iface ImplementClass<Iface, NLayout> (ModuleBuilder implModule, Type wrapperType, string lib, string className)
                        where NLayout : struct
                      //where Iface : ICppNativeInterface or ICppNativeInterfaceManaged
                {
                        this.implModule = implModule;
                        this.library = lib;
                        this.className = className;
                        this.interfaceType = typeof (Iface);
                        this.layoutType = typeof (NLayout);
                        this.wrapperType = wrapperType;

                        DefineImplType ();

                        var methods = ( // get all methods defined on the interface
                                       from method in interfaceType.GetMethods ()
                                       orderby method.MetadataToken
                                       select method
                                      ).Union( // ... as well as those defined on inherited interfaces
                                       from iface in interfaceType.GetInterfaces ()
                                       from method in iface.GetMethods ()
                                       select method
                                      );

                        var managedOverrides = from method in methods
                                               where Modifiers.IsVirtual (method)
                                               orderby method.MetadataToken
                                               select GetManagedOverrideTrampoline (method, VTable.BindToSignatureAndAttribute);

                        vtable = MakeVTable (managedOverrides.ToArray ());

                        // Implement all methods
                        int vtableIndex = 0;
                        foreach (var method in methods) {
                                // Skip over special methods like property accessors -- properties will be handled later
                                if (method.IsSpecialName)
                                        continue;

                                DefineMethod (method, vtableIndex);

                                if (Modifiers.IsVirtual (method))
                                        vtableIndex++;
                        }

                        // Implement all properties
                        foreach (var property in interfaceType.GetProperties ())
                                DefineFieldProperty (property);


                        ctorIL.Emit (OpCodes.Ret);
                        return (Iface)Activator.CreateInstance (implType.CreateType (), vtable);
                }

                // These methods might be more commonly overridden for a given C++ ABI:

                public virtual MethodType GetMethodType (MethodInfo imethod)
                {
                        if (imethod.Name.Equals (className))
                                return MethodType.NativeCtor;
                        else if (imethod.Name.Equals ("Alloc"))
                                return MethodType.ManagedAlloc;
                        else if (imethod.Name.Equals ("Destruct"))
                                return MethodType.NativeDtor;

                        return MethodType.Native;
                }

                public virtual int FieldOffsetPadding {
                        get { return Marshal.SizeOf (typeof (IntPtr)); }
                }

                protected virtual int NativeSize {
                        get {
                                // By default: native size = C++ class size + field offset padding (usually just vtable pointer)
                                // TODO: Only include vtable ptr if there are virtual functions? Here I guess it doesn't really matter,
                                // we're just allocing extra memory.
                                return Marshal.SizeOf (layoutType) + FieldOffsetPadding;
                        }
                }

                protected virtual VTable MakeVTable (Delegate[] overrides)
                {
                        return new VTableManaged (implModule, overrides);
                }

                // The members below must be implemented for a given C++ ABI:

                public abstract string GetMangledMethodName (MethodInfo methodInfo);
                public abstract CallingConvention DefaultCallingConvention { get; }

                protected virtual void DefineImplType ()
                {

                        implType = implModule.DefineType (implModule.Name + "_" + interfaceType.Name + "_Impl",
                                                          TypeAttributes.Class | TypeAttributes.Sealed);
                        implType.AddInterfaceImplementation (interfaceType);

                        vtableField = implType.DefineField ("_vtable", typeof (VTable), FieldAttributes.InitOnly | FieldAttributes.Private);
                        ConstructorBuilder ctor = implType.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard,
                                                                              new Type[] { typeof (VTable) });

                        ctorIL = ctor.GetILGenerator ();

                         // this._vtable = (VTable passed to constructor)
                        ctorIL.Emit (OpCodes.Ldarg_0);
                        ctorIL.Emit (OpCodes.Ldarg_1);
                        ctorIL.Emit (OpCodes.Stfld, vtableField);
                }

                protected virtual MethodBuilder DefineMethod (MethodInfo interfaceMethod, int index)
                {
                        // 0. Introspect method
                        MethodType methodType = GetMethodType (interfaceMethod);
                        Type[] parameterTypes = Util.GetMethodParameterTypes (interfaceMethod, false);

                        // 1. Generate managed trampoline to call native method
                        MethodBuilder trampoline = GetMethodBuilder (interfaceMethod);
                        ILGenerator il = trampoline.GetILGenerator ();

                        if (methodType == MethodType.ManagedAlloc) {
                                EmitManagedAlloc (il);
                                il.Emit (OpCodes.Ret);
                                return trampoline;
                        }

                        // 2. Load the native C++ instance pointer
                        LocalBuilder cppInstancePtr, nativePtr;
                        EmitLoadInstancePtr (il, parameterTypes[0], out cppInstancePtr, out nativePtr);

                        // 3. Make sure our native pointer is a valid reference. If not, throw ObjectDisposedException
                        EmitCheckDisposed (il, nativePtr, methodType);

                        MethodInfo nativeMethod;
                        if (Modifiers.IsVirtual (interfaceMethod))
                                nativeMethod = vtable.PrepareVirtualCall (interfaceMethod, il, vtableField, nativePtr, index);
                        else
                                nativeMethod = GetPInvokeForMethod (interfaceMethod);

                        switch (methodType) {
                        case MethodType.NativeCtor:
                                EmitConstruct (il, nativeMethod, parameterTypes.Length, nativePtr);
                                break;

                        case MethodType.NativeDtor:
                                EmitDestruct (il, nativeMethod, parameterTypes.Length, nativePtr);
                                break;

                        default: // regular native method
                                EmitCallNative (il, nativeMethod, parameterTypes.Length, nativePtr);
                                break;

                        }

                        il.Emit (OpCodes.Ret);
                        return trampoline;
                }

                protected virtual PropertyBuilder DefineFieldProperty (PropertyInfo property)
                {
                        if (property.CanWrite)
                                throw new InvalidProgramException ("Properties in C++ interface must be read-only.");

                        MethodInfo imethod = property.GetGetMethod ();
                        string methodName = imethod.Name;
                        string propName = property.Name;
                        Type retType = imethod.ReturnType;

                        if ((!retType.IsGenericType) || (!retType.GetGenericTypeDefinition ().Equals (typeof (CppField<>))))
                                throw new InvalidProgramException ("Properties in C++ interface can only be of type CppField.");

                        PropertyBuilder fieldProp = implType.DefineProperty (propName, PropertyAttributes.None, retType, Type.EmptyTypes);
                        FieldBuilder fieldData = implType.DefineField ("__" + propName + "_Data", retType, FieldAttributes.InitOnly | FieldAttributes.Private);

                        // init our field data with a new instance of CppField
                        // first, get field offset
                        ctorIL.Emit (OpCodes.Ldarg_0);

                        /* TODO: Code prolly should not emit hardcoded offsets n such, in case we end up saving these assemblies in the future.
                         *  Something more like this perhaps? (need to figure out how to get field offset padding into this)
                         *       ctorIL.Emit(OpCodes.Ldtoken, nativeLayout);
                         *       ctorIL.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                         *       ctorIL.Emit(OpCodes.Ldstr, propName);
                         *       ctorIL.Emit(OpCodes.Call, typeof(Marshal).GetMethod("OffsetOf"));
                         */
                        int fieldOffset = ((int)Marshal.OffsetOf (layoutType, propName)) + FieldOffsetPadding;
                        ctorIL.Emit (OpCodes.Ldc_I4, fieldOffset);
                        ctorIL.Emit (OpCodes.Newobj, retType.GetConstructor (new Type[] { typeof(int) }));

                        ctorIL.Emit (OpCodes.Stfld, fieldData);

                        MethodAttributes methodAttr = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                        MethodBuilder fieldGetter = implType.DefineMethod (methodName, methodAttr, retType, Type.EmptyTypes);
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
                protected virtual Delegate GetManagedOverrideTrampoline (MethodInfo interfaceMethod, MemberFilter binder)
                {
                        MethodInfo targetMethod = FindManagedOverrideTarget (interfaceMethod, binder);
                        if (targetMethod == null)
                                return null;

                        Type delegateType = Util.GetDelegateTypeForMethodInfo (implModule, interfaceMethod);
                        Type[] parameterTypes = Util.GetMethodParameterTypes (interfaceMethod, true);

                        // TODO: According to http://msdn.microsoft.com/en-us/library/w16z8yc4.aspx
                        // The dynamic method created with this constructor has access to public and internal members of all the types contained in module m.
                        // This does not appear to hold true, so we also disable JIT visibility checks.
                        DynamicMethod trampolineIn = new DynamicMethod (wrapperType.Name + "_" + interfaceMethod.Name + "_FromNative", interfaceMethod.ReturnType,
                                                                        parameterTypes, typeof (CppInstancePtr).Module, true);

                        ILGenerator il = trampolineIn.GetILGenerator ();

                        // for static methods:
                        OpCode callInstruction = OpCodes.Call;
                        int argLoadStart = 0;

                        // for instance methods, we need an instance to call them on!
                        if (!targetMethod.IsStatic) {
                                callInstruction = OpCodes.Callvirt;
                                argLoadStart = 1;

                                il.Emit (OpCodes.Ldarg_0);
                                il.Emit (OpCodes.Ldc_I4, NativeSize);

                                MethodInfo getManagedObj = typeof (CppInstancePtr).GetMethod ("GetManaged", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod (wrapperType);
                                il.Emit (OpCodes.Call, getManagedObj);
                        }

                        for (int i = argLoadStart; i < parameterTypes.Length; i++) {
                                il.Emit (OpCodes.Ldarg, i);
                        }
                        il.Emit (OpCodes.Tailcall);
                        il.Emit (callInstruction, targetMethod);
                        il.Emit (OpCodes.Ret);

                        return trampolineIn.CreateDelegate (delegateType);
                }

                protected virtual MethodInfo FindManagedOverrideTarget (MethodInfo interfaceMethod, MemberFilter filter)
                {
                        MemberInfo[] possibleMembers = wrapperType.FindMembers (MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic |
                                                                                BindingFlags.Instance | BindingFlags.Static, filter, interfaceMethod);

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

                        Type[] parameterTypes = Util.GetMethodParameterTypes (interfaceMethod, false);
                        MethodBuilder methodBuilder = implType.DefineMethod (interfaceMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual,
                                                                             interfaceMethod.ReturnType, parameterTypes);

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

                        Type[] parameterTypes = Util.GetMethodParameterTypes (signature, true);

                        MethodBuilder builder = implType.DefinePInvokeMethod ("__$" + signature.Name + "_Impl", library, entryPoint,
                                                                              MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                                                                              CallingConventions.Any, signature.ReturnType, parameterTypes,
                                                                              DefaultCallingConvention, CharSet.Ansi);

                        builder.SetImplementationFlags (builder.GetMethodImplementationFlags () | MethodImplAttributes.PreserveSig);
                        return builder;
                }

                /**
                 *  Emits IL to allocate the memory for a new instance of the C++ class.
                 *  To complete method, emit OpCodes.Ret.
                 */
                protected virtual void EmitManagedAlloc (ILGenerator il)
                {

                        //TODO: Do not hard-emit native size in case assembly is saved?
                        il.Emit (OpCodes.Ldc_I4, NativeSize);

                        if (wrapperType != null) {
                                // load managed object
                                il.Emit (OpCodes.Ldarg_1);

                                //new CppInstancePtr (Abi.GetNativeSize (typeof (NativeLayout)), managedWrapper);
                                il.Emit (OpCodes.Newobj, typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                                                                new Type[] { typeof (int), typeof (object) }, null));
                        } else
                                il.Emit (OpCodes.Newobj, typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                                                                new Type[] { typeof (int) }, null));
                }

                protected virtual void EmitConstruct (ILGenerator il, MethodInfo nativeMethod, int parameterCount, LocalBuilder nativePtr)
                {
                        EmitCallNative (il, nativeMethod, parameterCount, nativePtr);
                        EmitInitVTable (il, nativePtr);
                }

                protected virtual void EmitDestruct (ILGenerator il, MethodInfo nativeMethod, int parameterCount, LocalBuilder nativePtr)
                {
                        EmitResetVTable (il, nativePtr);
                        EmitCallNative (il, nativeMethod, parameterCount, nativePtr);
                }

                /**
                 * Emits IL to call the native method. nativeMethod should be either a method obtained by
                 * GetPInvokeForMethod or the MethodInfo of a vtable method.
                 * To complete method, emit OpCodes.Ret.
                 */
                protected virtual void EmitCallNative (ILGenerator il, MethodInfo nativeMethod, int parameterCount, LocalBuilder nativePtr)
                {
                        il.Emit (OpCodes.Ldloc_S, nativePtr);
                        for (int i = 2; i <= parameterCount; i++)
                                il.Emit (OpCodes.Ldarg, i);

                        il.Emit (OpCodes.Call, nativeMethod);
                }

                /**
                 * Emits IL to set the vtable pointer of the instance (if class has a vtable).
                 * This should usually happen in the managed wrapper of the C++ instance constructor.
                 */
                protected virtual void EmitInitVTable (ILGenerator il, LocalBuilder nativePtr)
                {
                        // this._vtable.InitInstance (nativePtr);
                        il.Emit (OpCodes.Ldarg_0);
                        il.Emit (OpCodes.Ldfld, vtableField);
                        il.Emit (OpCodes.Ldloc_S, nativePtr);
                        EmitVTableOp (il, typeof (VTable).GetMethod ("InitInstance"), 2, false);
                }

                protected virtual void EmitResetVTable (ILGenerator il, LocalBuilder nativePtr)
                {
                        // this._vtable.ResetInstance (nativePtr);
                        il.Emit (OpCodes.Ldarg_0);
                        il.Emit (OpCodes.Ldfld, vtableField);
                        il.Emit (OpCodes.Ldloc_S, nativePtr);
                        EmitVTableOp (il, typeof(VTable).GetMethod ("ResetInstance"), 2, false);
                }

                /**
                 * A utility function to emit the IL for a vtable-dependant operation.
                 * In other words, classes with no virtual methods will not have vtables,
                 * so this method emits code to check for that and either throw an exception
                 * or do nothing if no vtable exists. To use, push the arguments to the method you
                 * want to call and pass the stackHeight for the call. If no vtable exists, this method
                 * will emit code to pop the arguments off the stack.
                 */
                protected virtual void EmitVTableOp(ILGenerator il, MethodInfo method, int stackHeight, bool throwOnNoVTable)
                {
                        // prepare a jump; do not call vtable method if no vtable
                        Label noVirt = il.DefineLabel ();
                        Label dontPushOrThrow = il.DefineLabel ();

                        il.Emit (OpCodes.Ldarg_0); // load this
                        il.Emit (OpCodes.Ldfld, vtableField); // load this._vtable
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
                        // TODO: This is a kludge. What about value types?
                        if (!method.ReturnType.Equals (typeof (void)))
                                il.Emit (OpCodes.Ldnull);

                        if (throwOnNoVTable) {
                                il.Emit (OpCodes.Ldstr, "Native class has no VTable.");
                                il.Emit (OpCodes.Newobj, typeof (InvalidOperationException).GetConstructor(new Type[] {typeof (string)}));
                                il.Emit (OpCodes.Throw);
                        }

                        il.MarkLabel (dontPushOrThrow);
                }

                protected virtual void EmitLoadInstancePtr (ILGenerator il, Type firstParamType, out LocalBuilder cppip, out LocalBuilder native)
                {
                        cppip = null;
                        native = il.DeclareLocal (typeof (IntPtr));

                        il.Emit (OpCodes.Ldarg_1);
                        if (firstParamType.Equals (typeof (CppInstancePtr))) {
                                cppip = il.DeclareLocal (typeof (CppInstancePtr));
                                il.Emit (OpCodes.Stloc_S, cppip);
                                il.Emit (OpCodes.Ldloca_S, cppip);
                                il.Emit (OpCodes.Call, typeof (CppInstancePtr).GetProperty ("Native").GetGetMethod ());
                                il.Emit (OpCodes.Stloc_S, native);
                        } else if (firstParamType.Equals (typeof (IntPtr)))
                                il.Emit (OpCodes.Stloc_S, native);
                        else
                                throw new ArgumentException ("First argument to C++ method must be IntPtr or CppInstancePtr.");
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
