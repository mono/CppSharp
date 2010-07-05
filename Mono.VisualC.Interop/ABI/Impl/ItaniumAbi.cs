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

namespace Mono.VisualC.Interop.ABI {
	public class ItaniumAbi : CppAbi {

		public ItaniumAbi ()
                {
                }

		public override CallingConvention DefaultCallingConvention {
			get { return CallingConvention.Cdecl; }
		}

		public override string GetMangledMethodName (MethodInfo methodInfo)
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

			nm.Append ("E");
                        int argStart = (Modifiers.IsStatic (methodInfo)? 0 : 1);

			if (parameters.Length == argStart) // no args (other than C++ "this" object)
				nm.Append ("v");
			else
				for (int i = argStart; i < parameters.Length; i++)
					nm.Append (GetTypeCode (Modifiers.GetMangleType (parameters[i])));

			return nm.ToString ();
		}

		public virtual string GetTypeCode (CppType mangleType)
		{
			CppTypes element = mangleType.ElementType;
			IEnumerable<CppModifiers> modifiers = mangleType.Modifiers;

			StringBuilder code = new StringBuilder ();

			var modifierCode = modifiers.Reverse ().Transform (
				For.AnyInputIn (CppModifiers.Pointer, CppModifiers.Array).Emit ("P"),
				For.AnyInputIn (CppModifiers.Reference).Emit ("R"),

				Choose.TopOne (
					For.AnyInputIn (CppModifiers.Pointer, CppModifiers.Reference).FollowedBy ().AllInputsIn (CppModifiers.Volatile, CppModifiers.Const).InAnyOrder ().Emit ("VK"),
					For.AnyInputIn (CppModifiers.Pointer, CppModifiers.Reference).FollowedBy ().AnyInputIn(CppModifiers.Volatile).Emit ("V"),
					For.AnyInputIn (CppModifiers.Pointer, CppModifiers.Reference).FollowedBy ().AnyInputIn(CppModifiers.Const).Emit ("K")
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
                                code.Append(mangleType.ElementTypeName.Length);
                                code.Append(mangleType.ElementTypeName);
				break;

			}

                        return code.ToString ();
		}



	}
}