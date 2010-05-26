//
// Mono.VisualC.Interop.Attributes.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Reflection;

namespace Mono.VisualC.Interop {
        [AttributeUsage (AttributeTargets.Method)]
        public class VirtualAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Method)]
        public class OverrideNativeAttribute : Attribute {}

        public static class Modifiers {

                public static bool IsVirtual (MethodInfo method)
                {
                        return method.IsDefined (typeof (VirtualAttribute), false);
                }
        }
}
