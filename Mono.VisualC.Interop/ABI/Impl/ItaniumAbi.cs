//
// Mono.VisualC.Interop.ABI.ItaniumAbi.cs: An implementation of the Itanium C++ ABI
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

using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop.ABI {
	public class ItaniumAbi : CppAbi {

		public ItaniumAbi ()
                {
                }

		protected override CppTypeInfo MakeTypeInfo (IEnumerable<MethodInfo> virtualMethods)
		{
			return new ItaniumTypeInfo (this, virtualMethods, layout_type);
		}

		public override CallingConvention? GetCallingConvention (MethodInfo methodInfo)
		{
			return CallingConvention.Cdecl;
		}

		protected override string GetMangledMethodName (MethodInfo methodInfo)
                {
			string methodName = methodInfo.Name;
                        MethodType methodType = GetMethodType (methodInfo);
			ParameterInfo [] parameters = methodInfo.GetParameters ();

			StringBuilder nm = new StringBuilder ("_ZN", 30);
			nm.Append (class_name.Length).Append (class_name);

			switch (methodType) {
			case MethodType.NativeCtor:
				nm.Append ("C1");
				break;

			case MethodType.NativeDtor:
				nm.Append ("D1");
				break;

			default:
				nm.Append (methodName.Length).Append (methodName);
				break;
			}

			nm.Append ('E');
                        int argStart = (IsStatic (methodInfo)? 0 : 1);

			if (parameters.Length == argStart) // no args (other than C++ "this" object)
				nm.Append ('v');
			else
				for (int i = argStart; i < parameters.Length; i++)
					nm.Append (GetTypeCode (GetMangleType (parameters [i], parameters [i].ParameterType)));

			return nm.ToString ();
		}

		public virtual string GetTypeCode (CppType mangleType)
		{
			CppTypes element = mangleType.ElementType;
			IEnumerable<CppModifiers> modifiers = mangleType.Modifiers;

			StringBuilder code = new StringBuilder ();

			var ptrOrRef = For.AnyInputIn (CppModifiers.Pointer, CppModifiers.Reference);
			var modifierCode = modifiers.Reverse ().Transform (
				For.AnyInputIn (CppModifiers.Pointer, CppModifiers.Array).Emit ("P"),
				For.AnyInputIn (CppModifiers.Reference).Emit ("R"),

			        // Itanium mangled names do not include const or volatile unless
			        //  they modify the type pointed to by pointer or reference.
				Choose.TopOne (
					For.AllInputsIn (CppModifiers.Volatile, CppModifiers.Const).InAnyOrder ().After (ptrOrRef).Emit ("VK"),
					For.AnyInputIn(CppModifiers.Volatile).After (ptrOrRef).Emit ("V"),
					For.AnyInputIn(CppModifiers.Const).After (ptrOrRef).Emit ("K")
			        )
			);
			code.Append (string.Join(string.Empty, modifierCode.ToArray ()));

			switch (element) {
			case CppTypes.Int:
				code.Append (modifiers.Transform (
					For.AllInputsIn (CppModifiers.Unsigned, CppModifiers.Short).InAnyOrder ().Emit ('t')
				).DefaultIfEmpty ('i').ToArray ());
				break;
			case CppTypes.Char:
				code.Append ('c');
				break;
			case CppTypes.Class:
			case CppTypes.Struct:
			case CppTypes.Union:
			case CppTypes.Enum:
                                code.Append(mangleType.ElementTypeName.Length);
                                code.Append(mangleType.ElementTypeName);
				break;

			}

                        return code.ToString ();
		}



	}
}