//
// Mono.VisualC.Interop.Util.ReflectionHelper.cs: Helper methods for common reflection API tasks
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Reflection.Emit;

namespace Mono.VisualC.Interop.Util {
        internal static class ReflectionHelper {

                public static MethodInfo GetMethodInfoForDelegate (Type delType)
                {
                        return delType.GetMethod ("Invoke");
                }

                public static Type[] GetDelegateParameterTypes (Type delType)
                {
                        MethodInfo invoke = GetMethodInfoForDelegate (delType);
                        if (invoke == null)
                                return null;

                        return GetMethodParameterTypes (invoke);
                }

                public static Type[] GetMethodParameterTypes (MethodInfo method)
                {
                        ParameterInfo[] parameters = method.GetParameters ();
                        Type[] parameterTypes = new Type [parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                                        parameterTypes [i] = parameters [i].ParameterType;

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

                       /* if (forPInvoke &&
			    typeof (ICppObject).IsAssignableFrom (param.ParameterType) &&
                            !param.ParameterType.Equals (typeof (CppInstancePtr)) &&
			    existingMarshalAs == null)
                        {
                               ConstructorInfo ctor = typeof (MarshalAsAttribute).GetConstructor (new Type[] { typeof (UnmanagedType) });
                               object[] args = new object [] { UnmanagedType.CustomMarshaler };
                               FieldInfo[] fields = new FieldInfo [] { typeof (MarshalAsAttribute).GetField("MarshalTypeRef") };
                               object[] values = new object [] { typeof (CppObjectMarshaler) };

                               marshalAsAttr = new CustomAttributeBuilder (ctor, args, fields, values);
                               attr = attr | ParameterAttributes.HasFieldMarshal;

                        } else */ if (forPInvoke && existingMarshalAs != null) {
                                // FIXME: This still doesn't feel like it's working right.. especially for virtual functions
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
