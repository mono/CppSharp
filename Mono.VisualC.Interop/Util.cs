using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Mono.VisualC.Interop {
        public static class Util {

                public static MethodInfo GetMethodInfoForDelegate (Type delType)
                {
                        return delType.GetMethod ("Invoke");
                }

                public static Type GetDelegateTypeForMethodInfo (ModuleBuilder mod, MethodInfo targetMethod)
                {

                        TypeAttributes typeAttr = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass;
                        TypeBuilder del = mod.DefineType (mod.Name + "_" + targetMethod.Name + "_VTdel", typeAttr, typeof(MulticastDelegate));


                        MethodAttributes ctorAttr = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
                        ConstructorBuilder ctor = del.DefineConstructor (ctorAttr, CallingConventions.Standard, new Type[] { typeof(object), typeof(System.IntPtr) });
                        ctor.SetImplementationFlags (MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

                        Type[] parameterTypes = GetMethodParameterTypes (targetMethod, true);
                        MethodAttributes methodAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;

                        MethodBuilder invokeMethod = del.DefineMethod ("Invoke", methodAttr, targetMethod.ReturnType, parameterTypes);
                        invokeMethod.SetImplementationFlags (MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

                        return del.CreateType ();
                }

                public static Delegate GetDelegateForMethodInfo (ModuleBuilder mod, MethodInfo targetMethod)
                {
                        Type delType = GetDelegateTypeForMethodInfo (mod, targetMethod);
                        return Delegate.CreateDelegate (delType, targetMethod);
                }

                public static Type[] GetDelegateParameterTypes (Type delType)
                {
                        MethodInfo invoke = GetMethodInfoForDelegate (delType);
                        if (invoke == null)
                                return null;

                        return GetMethodParameterTypes (invoke, false);
                }

                public static Type[] GetMethodParameterTypes (MethodInfo method, bool forPInvoke)
                {
                        ParameterInfo[] parameters = method.GetParameters ();
                        Type[] parameterTypes = new Type [parameters.Length];

                        for (int i = 0; i < parameters.Length; i++) {
                                if (forPInvoke) {
                                        if (parameters [i].ParameterType.Equals (typeof (CppInstancePtr)))
                                                parameterTypes [i] = typeof (IntPtr);
                                        else if (parameters [i].ParameterType.IsPointer)
                                                parameterTypes [i] = parameters [i].ParameterType.GetElementType ().MakeByRefType ();
                                        else
                                                parameterTypes [i] = parameters [i].ParameterType;
                                } else
                                        parameterTypes [i] = parameters [i].ParameterType;
                        }

                        return parameterTypes;
                }
        }
}
