//
// Mono.VisualC.Interop.ABI.MsvcAbi.cs: An implementation of the Microsoft Visual C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop.ABI {

	// FIXME: No 64-bit support
	public class MsvcAbi : CppAbi {
		public MsvcAbi ()
		{
		}

		protected override CppTypeInfo MakeTypeInfo (IEnumerable<MethodInfo> virtualMethods)
		{
			return new MsvcTypeInfo (this, virtualMethods, layout_type);
		}

		public override CallingConvention? GetCallingConvention (MethodInfo methodInfo)
		{
			// FIXME: Varargs methods ... ?

			if (IsStatic (methodInfo))
				return CallingConvention.Cdecl;
			else
				return CallingConvention.ThisCall;
		}

		protected override string GetMangledMethodName (MethodInfo methodInfo)
		{
			string methodName = methodInfo.Name;
			MethodType methodType = GetMethodType (methodInfo);
			ParameterInfo [] parameters = methodInfo.GetParameters ();

			StringBuilder nm = new StringBuilder ("?", 30);

			if (methodType == MethodType.NativeCtor)
				nm.Append ("?0");
			else if (methodType == MethodType.NativeDtor)
				nm.Append ("?1");
			else
				nm.Append (methodName).Append ('@');

			// FIXME: This has to include not only the name of the immediate containing class,
			//  but also all names of containing classes and namespaces up the hierarchy.
			nm.Append (class_name).Append ("@@");

			// function modifiers are a matrix of consecutive uppercase letters
			// depending on access type and virtual (far)/static (far)/far modifiers

			// first, access type
			char funcModifier = 'Q'; // (public)
			if (IsProtected (methodInfo))
				funcModifier = 'I';
			else if (IsPrivate (methodInfo)) // (probably don't need this)
				funcModifier = 'A';

			// now, offset based on other modifiers
			if (IsStatic (methodInfo))
				funcModifier += (char)2;
			else if (IsVirtual (methodInfo))
				funcModifier += (char)4;

			nm.Append (funcModifier);

			// FIXME: deal with other storage classes for "this" i.e. the "volatile" in -> int foo () volatile;
			if (!IsStatic (methodInfo)) {
				if (IsConst (methodInfo))
					nm.Append ('B');
				else
					nm.Append ('A');
			}

			switch (GetCallingConvention (methodInfo)) {
			case CallingConvention.Cdecl:
				nm.Append ('A');
				break;
			case CallingConvention.ThisCall:
				nm.Append ('E');
				break;
			case CallingConvention.StdCall:
				nm.Append ('G');
				break;
			case CallingConvention.FastCall:
				nm.Append ('I');
				break;
			}

			// FIXME: handle const, volatile modifiers on return type
			// FIXME: the manual says this is only omitted for simple types.. are we doing the right thing here?
			CppType returnType = GetMangleType (methodInfo.ReturnTypeCustomAttributes, methodInfo.ReturnType);
			if (returnType.ElementType == CppTypes.Class ||
			    returnType.ElementType == CppTypes.Struct ||
			    returnType.ElementType == CppTypes.Union)
				nm.Append ("?A");

			if (methodType == MethodType.NativeCtor || methodType == MethodType.NativeDtor)
				nm.Append ('@');
			else
				nm.Append (GetTypeCode (returnType));

			int argStart = (IsStatic (methodInfo)? 0 : 1);
			if (parameters.Length == argStart) { // no args (other than C++ "this" object)
				nm.Append ("XZ");
				return nm.ToString ();
			} else
				for (int i = argStart; i < parameters.Length; i++)
					nm.Append (GetTypeCode (GetMangleType (parameters [i], parameters [i].ParameterType)));

			nm.Append ("@Z");
			return nm.ToString ();
		}

		public virtual string GetTypeCode (CppType mangleType)
		{
			CppTypes element = mangleType.ElementType;
			IEnumerable<CppModifiers> modifiers = mangleType.Modifiers;

			StringBuilder code = new StringBuilder ();

			var ptr = For.AnyInputIn (CppModifiers.Pointer);
			var ptrRefOrArray = For.AnyInputIn (CppModifiers.Pointer, CppModifiers.Reference, CppModifiers.Array);

			var modifierCode = modifiers.Reverse ().Transform (

				Choose.TopOne (
					For.AllInputsIn (CppModifiers.Const, CppModifiers.Volatile).InAnyOrder ().After (ptrRefOrArray).Emit ('D'),
			                For.AnyInputIn (CppModifiers.Const).After (ptrRefOrArray).Emit ('B'),
					For.AnyInputIn (CppModifiers.Volatile).After (ptrRefOrArray).Emit ('C'),
			                For.AnyInput<CppModifiers> ().After (ptrRefOrArray).Emit ('A')
			        ),

				For.AnyInputIn (CppModifiers.Array).Emit ('Q'),
				For.AnyInputIn (CppModifiers.Reference).Emit ('A'),

				Choose.TopOne (
			                ptr.After ().AllInputsIn (CppModifiers.Const, CppModifiers.Volatile).InAnyOrder ().Emit ('S'),
					ptr.After ().AnyInputIn (CppModifiers.Const).Emit ('Q'),
			                ptr.After ().AnyInputIn (CppModifiers.Volatile).Emit ('R'),
			                ptr.Emit ('P')
                                ),

			        ptrRefOrArray.AtEnd ().Emit ('A')
			);
			code.Append (modifierCode.ToArray ());

			switch (element) {
			case CppTypes.Void:
				code.Append ('X');
				break;
			case CppTypes.Int:
				code.Append (modifiers.Transform (
					For.AllInputsIn (CppModifiers.Unsigned, CppModifiers.Short).InAnyOrder ().Emit ('G')
				).DefaultIfEmpty ('H').ToArray ());
				break;
			case CppTypes.Char:
				code.Append ('D');
				break;
			case CppTypes.Class:
				code.Append ('V');
				code.Append(mangleType.ElementTypeName);
				code.Append ("@@");
				break;
			case CppTypes.Struct:
				code.Append ('U');
				code.Append(mangleType.ElementTypeName);
				code.Append ("@@");
				break;
			case CppTypes.Union:
				code.Append ('T');
				code.Append(mangleType.ElementTypeName);
				code.Append ("@@");
				break;
			case CppTypes.Enum:
				code.Append ("W4");
				code.Append(mangleType.ElementTypeName);
				code.Append ("@@");
				break;
			}

			return code.ToString ();
		}

	}
}

