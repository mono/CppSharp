//
// Mono.VisualC.Interop.Attributes.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Reflection;

namespace Mono.VisualC.Interop {
        [AttributeUsage (AttributeTargets.Method)]
        public class VirtualAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Method)]
        public class StaticAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Method)]
        public class OverrideNativeAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Parameter)]
        public class MangleAsAttribute : Attribute {
                public Type MangleType { get; private set; }
                public MangleAsAttribute (Type mangleType)
                {
                        this.MangleType = mangleType;
                }
                public MangleAsAttribute (string mangleTypeAsString)
                {
                        this.MangleType = Type.GetType (mangleTypeAsString);
                }
        }

        public static class Modifiers {

                public static bool IsVirtual (MethodInfo method)
                {
                        return method.IsDefined (typeof (VirtualAttribute), false);
                }

                public static bool IsStatic (MethodInfo method)
                {
                        return method.IsDefined (typeof (StaticAttribute), false);
                }

                public static Type GetMangleType (ParameterInfo param)
                {
                        MangleAsAttribute maa = (MangleAsAttribute)param.GetCustomAttributes (typeof (MangleAsAttribute), false).FirstOrDefault ();
                        if (maa == null)
                                return param.ParameterType;

                        return maa.MangleType;
                }
        }
}
