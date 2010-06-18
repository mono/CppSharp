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

                protected ModuleBuilder impl_module;
                protected TypeBuilder impl_type;

                protected Type interface_type, layout_type, wrapper_type;
                protected string library, class_name;

                protected VTable vtable;
                protected FieldBuilder vtable_field, native_size_field;
                protected ILGenerator ctor_il;

                // default settings that subclasses can override:
                protected MakeVTableDelegate make_vtable_method = VTable.DefaultImplementation;
                protected MemberFilter vtable_override_filter = VTable.BindToSignatureAndAttribute;


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

                        var properties = ( // get all properties defined on the interface
                                          from property in interface_type.GetProperties ()
                                          select property
                                         ).Union ( // ... as well as those defined on inherited interfaces
                                          from iface in interface_type.GetInterfaces ()
                                          from property in iface.GetProperties ()
                                          select property
                                         );

                        var methods = ( // get all methods defined on inherited interfaces first
                                       from iface in interface_type.GetInterfaces ()
                                       from method in iface.GetMethods ()
                                       select method
                                      ).Union ( // ... as well as those defined on this interface
                                       from method in interface_type.GetMethods ()
                                       orderby method.MetadataToken
                                       select method
                                      );

                        // TODO: If a method in a base class interface is defined again in a child class
                        //  interface with exactly the same signature, it will prolly share a vtable slot

                        // first, pull in all virtual methods from base classes
                        var baseVirtualMethods = from iface in interface_type.GetInterfaces ()
                                                 where iface.Name.Equals ("Base`1")
                                                 from method in iface.GetGenericArguments ().First ().GetMethods ()
                                                 where Modifiers.IsVirtual (method)
                                                 orderby method.MetadataToken
                                                 select GetManagedOverrideTrampoline (method, vtable_override_filter);


                        var virtualMethods = baseVirtualMethods.Concat ( // now create managed overrides for virtuals in this class
                                                 from method in methods
                                                 where Modifiers.IsVirtual (method)
                                                 orderby method.MetadataToken
                                                 select GetManagedOverrideTrampoline (method, vtable_override_filter)
                                             );

                        vtable = make_vtable_method (virtualMethods.ToArray ());
                        int vtableIndex = baseVirtualMethods.Count ();

                        // Implement all methods
                        foreach (var method in methods) {
                                // Skip over special methods like property accessors -- properties will be handled later
                                if (method.IsSpecialName)
                                        continue;

                                DefineMethod (method, vtableIndex);

                                if (Modifiers.IsVirtual (method))
                                        vtableIndex++;
                        }

                        // Implement all properties
                        foreach (var property in properties)
                                DefineProperty (property);

                        ctor_il.Emit (OpCodes.Ret);
                        return (Iface)Activator.CreateInstance (impl_type.CreateType (), vtable, NativeSize);
                }

                // These methods might be more commonly overridden for a given C++ ABI:

                public virtual MethodType GetMethodType (MethodInfo imethod)
                {
                        if (imethod.Name.Equals (class_name))
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
                                return Marshal.SizeOf (layout_type) + FieldOffsetPadding;
                        }
                }

                // The members below must be implemented for a given C++ ABI:

                public abstract string GetMangledMethodName (MethodInfo methodInfo);
                public abstract CallingConvention DefaultCallingConvention { get; }

                protected virtual void DefineImplType ()
                {
                        string implTypeName = interface_type.Name + "_" + layout_type.Name + "_" + this.GetType ().Name + "_Impl";
                        impl_type = impl_module.DefineType (implTypeName, TypeAttributes.Class | TypeAttributes.Sealed);
                        impl_type.AddInterfaceImplementation (interface_type);

                        vtable_field = impl_type.DefineField ("_vtable", typeof (VTable), FieldAttributes.InitOnly | FieldAttributes.Private);
                        native_size_field = impl_type.DefineField ("_nativeSize", typeof (int), FieldAttributes.InitOnly | FieldAttributes.Private);

                        ConstructorBuilder ctor = impl_type.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard,
                                                                              new Type[] { typeof (VTable), typeof (int) });

                        ctor_il = ctor.GetILGenerator ();

                         // this._vtable = (VTable passed to constructor)
                        ctor_il.Emit (OpCodes.Ldarg_0);
                        ctor_il.Emit (OpCodes.Ldarg_1);
                        ctor_il.Emit (OpCodes.Stfld, vtable_field);
                        // this._nativeSize = (native size passed to constructor)
                        ctor_il.Emit (OpCodes.Ldarg_0);
                        ctor_il.Emit (OpCodes.Ldarg_2);
                        ctor_il.Emit (OpCodes.Stfld, native_size_field);
                }

                protected virtual MethodBuilder DefineMethod (MethodInfo interfaceMethod, int index)
                {
                        // 0. Introspect method
                        MethodType methodType = GetMethodType (interfaceMethod);
                        Type[] parameterTypes = Util.GetMethodParameterTypes (interfaceMethod, false);

                        // 1. Generate managed trampoline to call native method
                        MethodBuilder trampoline = GetMethodBuilder (interfaceMethod);

                        ILGenerator il = trampoline.GetILGenerator ();

                        if (methodType == MethodType.NoOp) {
                                // return NULL if method is supposed to return a value
                                // TODO: this will make value types explode?
                                if (!interfaceMethod.ReturnType.Equals (typeof (void)))
                                        il.Emit (OpCodes.Ldnull);
                                il.Emit (OpCodes.Ret);
                                return trampoline;
                        } else if (methodType == MethodType.ManagedAlloc) {
                                EmitManagedAlloc (il, interfaceMethod);
                                il.Emit (OpCodes.Ret);
                                return trampoline;
                        }

                        bool isStatic = true;
                        LocalBuilder cppInstancePtr = null;
                        LocalBuilder nativePtr = null;

                        // If we're not a static method, do the following ...
                        if (!Modifiers.IsStatic (interfaceMethod))
                        {
                                isStatic = false;
                                if (parameterTypes.Length < 1)
                                        throw new ArgumentException ("First argument to non-static C++ method must be IntPtr or CppInstancePtr.");

                                // 2. Load the native C++ instance pointer
                                EmitLoadInstancePtr (il, parameterTypes[0], out cppInstancePtr, out nativePtr);

                                // 3. Make sure our native pointer is a valid reference. If not, throw ObjectDisposedException
                                EmitCheckDisposed (il, nativePtr, methodType);
                        }

                        MethodInfo nativeMethod;
                        if (Modifiers.IsVirtual (interfaceMethod))
                                nativeMethod = vtable.PrepareVirtualCall (interfaceMethod, il, vtable_field, nativePtr, index);
                        else
                                nativeMethod = GetPInvokeForMethod (interfaceMethod);

                        switch (methodType) {
                        case MethodType.NativeCtor:
                                EmitConstruct (il, nativeMethod, parameterTypes.Length, nativePtr);
                                break;

                        case MethodType.NativeDtor:
                                EmitDestruct (il, nativeMethod, parameterTypes.Length, cppInstancePtr, nativePtr);
                                break;

                        default: // regular native method
                                EmitCallNative (il, nativeMethod, isStatic, parameterTypes.Length, nativePtr);
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

                        // C++ interface properties are either to return the VTable, get NativeSize, or to access C++ fields
                        if (retType.IsGenericType && retType.GetGenericTypeDefinition ().Equals (typeof (CppField<>))) {
                                // define a new field for the property
                                fieldData = impl_type.DefineField ("__" + propName + "_Data", retType, FieldAttributes.InitOnly | FieldAttributes.Private);

                                // init our field data with a new instance of CppField
                                // first, get field offset
                                ctor_il.Emit (OpCodes.Ldarg_0);

                                /* TODO: Code prolly should not emit hardcoded offsets n such, in case we end up saving these assemblies in the future.
                                *  Something more like this perhaps? (need to figure out how to get field offset padding into this)
                                *       ctorIL.Emit(OpCodes.Ldtoken, nativeLayout);
                                *       ctorIL.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                                *       ctorIL.Emit(OpCodes.Ldstr, propName);
                                *       ctorIL.Emit(OpCodes.Call, typeof(Marshal).GetMethod("OffsetOf"));
                                */
                                int fieldOffset = ((int)Marshal.OffsetOf (layout_type, propName)) + FieldOffsetPadding;
                                ctor_il.Emit (OpCodes.Ldc_I4, fieldOffset);
                                ctor_il.Emit (OpCodes.Newobj, retType.GetConstructor (new Type[] { typeof(int) }));

                                ctor_il.Emit (OpCodes.Stfld, fieldData);
                        } else if (retType.Equals (typeof (VTable)))
                                fieldData = vtable_field;
                          else if (retType.Equals (typeof (int)))
                                fieldData = native_size_field;
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
                protected virtual Delegate GetManagedOverrideTrampoline (MethodInfo interfaceMethod, MemberFilter binder)
                {
                        if (wrapper_type == null)
                                return null;

                        MethodInfo targetMethod = FindManagedOverrideTarget (interfaceMethod, binder);
                        if (targetMethod == null)
                                return null;

                        Type delegateType = Util.GetDelegateTypeForMethodInfo (impl_module, interfaceMethod);
                        Type[] parameterTypes = Util.GetMethodParameterTypes (interfaceMethod, true);

                        // TODO: According to http://msdn.microsoft.com/en-us/library/w16z8yc4.aspx
                        // The dynamic method created with this constructor has access to public and internal members of all the types contained in module m.
                        // This does not appear to hold true, so we also disable JIT visibility checks.
                        DynamicMethod trampolineIn = new DynamicMethod (wrapper_type.Name + "_" + interfaceMethod.Name + "_FromNative", interfaceMethod.ReturnType,
                                                                        parameterTypes, typeof (CppInstancePtr).Module, true);

                        Util.ApplyMethodParameterAttributes (interfaceMethod, trampolineIn);
                        ILGenerator il = trampolineIn.GetILGenerator ();

                        // for static methods:
                        OpCode callInstruction = OpCodes.Call;
                        int argLoadStart = 1;

                        // for instance methods, we need an instance to call them on!
                        if (!targetMethod.IsStatic) {
                                callInstruction = OpCodes.Callvirt;
                                //argLoadStart = 1;

                                il.Emit (OpCodes.Ldarg_0);
                                il.Emit (OpCodes.Ldc_I4, NativeSize);

                                MethodInfo getManagedObj = typeof (CppInstancePtr).GetMethod ("GetManaged", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod (wrapper_type);
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
                        MemberInfo[] possibleMembers = wrapper_type.FindMembers (MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic |
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
                        MethodBuilder methodBuilder = impl_type.DefineMethod (interfaceMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual,
                                                                             interfaceMethod.ReturnType, parameterTypes);
                        Util.ApplyMethodParameterAttributes (interfaceMethod, methodBuilder);
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

                        MethodBuilder builder = impl_type.DefinePInvokeMethod ("__$" + signature.Name + "_Impl", library, entryPoint,
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
                protected virtual void EmitManagedAlloc (ILGenerator il, MethodInfo interfaceMethod)
                {
                        Type paramType = interfaceMethod.GetParameters () [0].ParameterType;
                        if (typeof (ICppObject).IsAssignableFrom (paramType))
                        {
                                // TODO: This is probably going to be causing us to alloc too much memory
                                //  if the ICppObject is returning impl.NativeSize + base.NativeSize. (We get FieldOffsetPadding
                                //  each time).
                                il.Emit (OpCodes.Ldarg_1);
                                il.Emit (OpCodes.Callvirt, typeof (ICppObject).GetProperty ("NativeSize").GetGetMethod ());
                        } else
                                il.Emit (OpCodes.Ldfld, native_size_field);

                        if (wrapper_type != null) {
                                // load managed object
                                il.Emit (OpCodes.Ldarg_1);

                                //new CppInstancePtr (Abi.GetNativeSize (typeof (NativeLayout)), managedWrapper);
                                il.Emit (OpCodes.Newobj, typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                                                                new Type[] { typeof (int), typeof (object) }, null));
                        } else
                                il.Emit (OpCodes.Newobj, typeof (CppInstancePtr).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                                                                new Type[] { typeof (int) }, null));
                }

                protected virtual void EmitConstruct (ILGenerator il, MethodInfo nativeMethod, int parameterCount,
                                                      LocalBuilder nativePtr)
                {
                        EmitCallNative (il, nativeMethod, false, parameterCount, nativePtr);
                        EmitInitVTable (il, nativePtr);
                }

                protected virtual void EmitDestruct (ILGenerator il, MethodInfo nativeMethod, int parameterCount,
                                                     LocalBuilder cppInstancePtr, LocalBuilder nativePtr)
                {
                        // bail if we weren't alloc'd by managed code
                        Label bail = il.DefineLabel ();

                        il.Emit (OpCodes.Ldloca_S, cppInstancePtr);
                        //il.Emit (OpCodes.Dup);
                        //il.Emit (OpCodes.Brfalse_S, bail);
                        il.Emit (OpCodes.Call, typeof (CppInstancePtr).GetProperty ("IsManagedAlloc").GetGetMethod ());
                        il.Emit (OpCodes.Brfalse_S, bail);

                        EmitResetVTable (il, nativePtr);
                        EmitCallNative (il, nativeMethod, false, parameterCount, nativePtr);

                        il.MarkLabel (bail);
                }

                /**
                 * Emits IL to call the native method. nativeMethod should be either a method obtained by
                 * GetPInvokeForMethod or the MethodInfo of a vtable method.
                 * To complete method, emit OpCodes.Ret.
                 */
                protected virtual void EmitCallNative (ILGenerator il, MethodInfo nativeMethod, bool isStatic, int parameterCount,
                                                       LocalBuilder nativePtr)
                {
                        int argLoadStart = 1; // For static methods, just strip off arg0 (.net this pointer)
                        if (!isStatic)
                        {
                                argLoadStart = 2; // For instance methods, strip off CppInstancePtr and pass the corresponding IntPtr
                                il.Emit (OpCodes.Ldloc_S, nativePtr);
                        }
                        for (int i = argLoadStart; i <= parameterCount; i++)
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
                        il.Emit (OpCodes.Ldfld, vtable_field);
                        il.Emit (OpCodes.Ldloc_S, nativePtr);
                        EmitVTableOp (il, typeof (VTable).GetMethod ("InitInstance"), 2, false);
                }

                protected virtual void EmitResetVTable (ILGenerator il, LocalBuilder nativePtr)
                {
                        // this._vtable.ResetInstance (nativePtr);
                        il.Emit (OpCodes.Ldarg_0);
                        il.Emit (OpCodes.Ldfld, vtable_field);
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
                protected virtual void EmitVTableOp(ILGenerator il, MethodInfo method, int stackHeight,
                                                    bool throwOnNoVTable)
                {
                        // prepare a jump; do not call vtable method if no vtable
                        Label noVirt = il.DefineLabel ();
                        Label dontPushOrThrow = il.DefineLabel ();

                        il.Emit (OpCodes.Ldarg_0); // load this
                        il.Emit (OpCodes.Ldfld, vtable_field); // load this._vtable
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

                protected virtual void EmitLoadInstancePtr (ILGenerator il, Type firstParamType, out LocalBuilder cppip,
                                                            out LocalBuilder native)
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
                                throw new ArgumentException ("First argument to non-static C++ method must be IntPtr or CppInstancePtr.");
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
