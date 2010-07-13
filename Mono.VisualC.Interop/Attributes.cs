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

	#region Interface method attributes
        [AttributeUsage (AttributeTargets.Method)]
        public class VirtualAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Interface)]
	public class VirtualDestructorAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Method)]
        public class StaticAttribute : Attribute {}

	// FIXME: Will we ever be calling private methods?
	[AttributeUsage (AttributeTargets.Method)]
        public class PrivateAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Method)]
        public class ProtectedAttribute : Attribute {}

        [AttributeUsage (AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
        public class MangleAsAttribute : Attribute {
                public CppType MangleType { get; private set; }

                public MangleAsAttribute (CppType mangleType)
                {
                        this.MangleType = mangleType;
                }
                public MangleAsAttribute (string mangleTypeStr)
                {
                        this.MangleType = new CppType (mangleTypeStr);
                }
		public MangleAsAttribute (params object[] cppTypeSpec)
		{
			this.MangleType = new CppType (cppTypeSpec);
		}
        }
	#endregion

        [AttributeUsage (AttributeTargets.Method)]
        public class OverrideNativeAttribute : Attribute {}
}

namespace Mono.VisualC.Interop.ABI {
using Mono.VisualC.Interop;

	public partial class CppAbi {

                public virtual bool IsVirtual (MethodInfo method)
                {
			return method.IsDefined (typeof (VirtualAttribute), false);
                }
		public virtual bool IsVirtualDtor (MethodInfo method)
		{
			return GetMethodType (method) == MethodType.NativeDtor &&
				interface_type.IsDefined (typeof (VirtualDestructorAttribute), false);
		}
                public virtual bool IsStatic (MethodInfo method)
                {
                        return method.IsDefined (typeof (StaticAttribute), false);
                }
		public virtual bool IsPrivate (MethodInfo method)
		{
			return method.IsDefined (typeof (PrivateAttribute), false);
		}
		public virtual bool IsProtected (MethodInfo method)
		{
			return method.IsDefined (typeof (ProtectedAttribute), false);
		}

                public virtual CppType GetMangleType (ICustomAttributeProvider icap, Type managedType)
                {
			CppType mangleType = new CppType ();
                        MangleAsAttribute maa = (MangleAsAttribute)icap.GetCustomAttributes (typeof (MangleAsAttribute), false).FirstOrDefault ();
			if (maa != null)
				mangleType = maa.MangleType;

			// this means that either no MangleAsAttribute was defined, or
			//  only CppModifiers were applied .. apply CppType from managed parameter type
			if (mangleType.ElementType == CppTypes.Unknown && mangleType.ElementTypeName == null)
				mangleType.ApplyTo (CppType.ForManagedType (managedType));
			else if (mangleType.ElementType == CppTypes.Unknown)
				// FIXME: otherwise, we just assume it's CppTypes.Class for now.
				mangleType.ElementType = CppTypes.Class;

                        return mangleType;
                }
        }

}
