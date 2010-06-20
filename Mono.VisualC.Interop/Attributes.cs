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

        [AttributeUsage (AttributeTargets.Parameter)]
        public class ConstAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Method)]
        public class OverrideNativeAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Parameter)]
        public class MangleAsAttribute : Attribute {
                public CppModifiers Modifiers { get; set; }
                public Type MangleType { get; private set; }

                public bool ByRef {
                        get { return MangleType.IsByRef; }
                        set {
                                if (!MangleType.IsByRef && value)
                                        MangleType = MangleType.MakeByRefType ();
                                else if (MangleType.IsByRef && !value)
                                        MangleType = MangleType.GetElementType ();
                        }
                }
                public MangleAsAttribute (Type mangleType)
                {
                        this.Modifiers = CppModifiers.None;
                        this.MangleType = mangleType;
                }
                public MangleAsAttribute (string mangleTypeStr)
                {
                        this.Modifiers = CppModifiers.None;
                        this.MangleType = Type.GetType (mangleTypeStr);
                }
        }

        public enum CppModifiers {
                None,
                Const,
                Pointer,
                PointerToConst,
                ConstPointer,
                ConstPointerToConst
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

                public static MangleAsAttribute GetMangleInfo (ParameterInfo param)
                {
                        MangleAsAttribute maa = (MangleAsAttribute)param.GetCustomAttributes (typeof (MangleAsAttribute), false).FirstOrDefault ();
                        bool isConst = param.IsDefined (typeof (ConstAttribute), false);
                        if (maa == null)
                                maa = new MangleAsAttribute (param.ParameterType);

                        if (isConst)
                                maa.Modifiers = CppModifiers.Const;

                        return maa;
                }
        }
}
