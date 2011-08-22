//
// Mono.Cxxi.Attributes.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Reflection;

namespace Mono.Cxxi {

	#region Interface method attributes

	[AttributeUsage (AttributeTargets.Method)]
	public class ConstructorAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Method)]
	public class DestructorAttribute : Attribute {}


	[AttributeUsage (AttributeTargets.Method)]
	public class VirtualAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Method)]
	public class StaticAttribute : Attribute {}

	// used for the const "this" - for example: int value () const;
	//  use MangleAsAttribute for const parameters
	[AttributeUsage (AttributeTargets.Method)]
	public class ConstAttribute : Attribute {}

	// FIXME: Will we ever be calling private methods?
	[AttributeUsage (AttributeTargets.Method)]
	public class PrivateAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Method)]
	public class ProtectedAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Method)]
	public class InlineAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Method)]
	public class ArtificialAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Method)]
	public class CopyConstructorAttribute : Attribute {}

	// applied when the target would normally be passed by reference (e.g. class)
	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	public class ByValAttribute : Attribute {}

	// used for byref return of things that would normally be passed by value (e.g. int&)
	[AttributeUsage (AttributeTargets.ReturnValue)]
	public class ByRefAttribute : Attribute {}

	[AttributeUsage (AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
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
		public MangleAsAttribute (params object [] cppTypeSpec)
		{
			this.MangleType = new CppType (cppTypeSpec);
		}
	}

	// for testing:
	[AttributeUsage (AttributeTargets.Method)]
	public class AbiTestAttribute : Attribute {
		public string MangledName { get; set; }
		public Type Abi { get; set; }

		public AbiTestAttribute (string mangledName)
		{
			MangledName = mangledName;
		}
	}

	#endregion

	#region Wrapper method attributes
	[AttributeUsage (AttributeTargets.Method)]
	public class OverrideNativeAttribute : Attribute {
		public string NativeMethod { get; set; }
		public OverrideNativeAttribute ()
		{
		}
		public OverrideNativeAttribute (string nativeMethod)
		{
			this.NativeMethod = nativeMethod;
		}
	}
	#endregion
}

namespace Mono.Cxxi.Abi {
using Mono.Cxxi;

	public partial class CppAbi {

		public virtual bool IsVirtual (MethodInfo method)
		{
			return method.IsDefined (typeof (VirtualAttribute), false);
		}
		public virtual bool IsStatic (MethodInfo method)
		{
			return method.IsDefined (typeof (StaticAttribute), false);
		}
		public virtual bool IsConst (MethodInfo method)
		{
			return method.IsDefined (typeof (ConstAttribute), false);
		}
		public virtual bool IsPrivate (MethodInfo method)
		{
			return method.IsDefined (typeof (PrivateAttribute), false);
		}
		public virtual bool IsProtected (MethodInfo method)
		{
			return method.IsDefined (typeof (ProtectedAttribute), false);
		}
		public virtual bool IsInline (MethodInfo method)
		{
			return method.IsDefined (typeof (InlineAttribute), false);
		}
		public virtual bool IsArtificial (MethodInfo method)
		{
			return method.IsDefined (typeof (ArtificialAttribute), false);
		}
		public virtual bool IsCopyConstructor (MethodInfo method)
		{
			return method.IsDefined (typeof (CopyConstructorAttribute), false);
		}
		public virtual bool IsByVal (ICustomAttributeProvider icap)
		{
			return icap.IsDefined (typeof (ByValAttribute), false);
		}
		public virtual bool IsByRef (ICustomAttributeProvider icap, Type type)
		{
			return type.IsByRef || icap.IsDefined (typeof (ByRefAttribute), false);
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
				mangleType.CopyTypeFrom (CppType.ForManagedType (managedType));
			else if (mangleType.ElementType == CppTypes.Unknown)
				// FIXME: otherwise, we just assume it's CppTypes.Class for now.
				mangleType.ElementType = CppTypes.Class;

			return mangleType;
		}

	}

}
