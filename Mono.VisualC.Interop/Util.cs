using System;
using System.Linq;
using System.Runtime.InteropServices;
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
                        // TODO: Actually return the same delegate type instead of creating a new one if
                        //  a suitable type already exists??
                        string delTypeName = mod.Name + "_" + targetMethod.Name + "_VTdel";
                        while (mod.GetType (delTypeName) != null)
                                delTypeName += "_";

                        TypeAttributes typeAttr = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass;
                        TypeBuilder del = mod.DefineType (delTypeName, typeAttr, typeof(MulticastDelegate));


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

                public static void ApplyMethodParameterAttributes (MethodInfo source, MethodBuilder target, bool forPInvoke)
                {
                        ParameterInfo[] parameters = source.GetParameters ();
                        foreach (var param in parameters)
                                ApplyAttributes (param, target.DefineParameter, forPInvoke);
                }

                public static void ApplyMethodParameterAttributes (MethodInfo source, DynamicMethod target, bool forPInvoke)
                {
                        ParameterInfo[] parameters = source.GetParameters ();
                        foreach (var param in parameters)
                                ApplyAttributes (param, target.DefineParameter, forPInvoke);
                }

                public static ParameterBuilder ApplyAttributes (ParameterInfo param, Func<int,ParameterAttributes,string,ParameterBuilder> makePB, bool forPInvoke)
                {
                        ParameterAttributes attr = param.Attributes;
                        CustomAttributeBuilder marshalAsAttr = null;

                        MarshalAsAttribute existingMarshalAs = param.GetCustomAttributes (typeof (MarshalAsAttribute),
                                                                                          false).FirstOrDefault () as MarshalAsAttribute;

                        if (forPInvoke && typeof (ICppObject).IsAssignableFrom (param.ParameterType) &&
                            !param.ParameterType.Equals (typeof (CppInstancePtr)) && existingMarshalAs == null)
                        {
                               ConstructorInfo ctor = typeof (MarshalAsAttribute).GetConstructor (new Type[] { typeof (UnmanagedType) });
                               object[] args = new object [] { UnmanagedType.CustomMarshaler };
                               FieldInfo[] fields = new FieldInfo [] { typeof (MarshalAsAttribute).GetField("MarshalTypeRef") };
                               object[] values = new object [] { typeof (CppObjectMarshaler) };

                               marshalAsAttr = new CustomAttributeBuilder (ctor, args, fields, values);
                               attr = attr | ParameterAttributes.HasFieldMarshal;

                        } else if (forPInvoke && existingMarshalAs != null) {
                                // TODO: This still doesn't feel like it's working right..
                               ConstructorInfo ctor = typeof (MarshalAsAttribute).GetConstructor (new Type[] { typeof (UnmanagedType) });
                               object[] args = new object [] { existingMarshalAs.Value };

                               var fields = from field in typeof (MarshalAsAttribute).GetFields ()
                                             where field.GetValue (existingMarshalAs) != null
                                             select field;

                               var values = from field in fields select field.GetValue (existingMarshalAs);

                               marshalAsAttr = new CustomAttributeBuilder (ctor, args, fields.ToArray (), values.ToArray ());
                               attr = attr | ParameterAttributes.HasFieldMarshal;
                        }
                         
                        ParameterBuilder pb = makePB (param.Position, attr, param.Name);
                        if (marshalAsAttr != null) pb.SetCustomAttribute (marshalAsAttr);
                        return pb;
                }
        }
}
