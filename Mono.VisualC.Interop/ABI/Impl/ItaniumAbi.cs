//
// Mono.VisualC.Interop.ABI.ItaniumAbi.cs: An implementation of the Itanium C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop.ABI {
	public class ItaniumAbi : CppAbi {

		public ItaniumAbi ()
                {
                }

		public override CallingConvention DefaultCallingConvention {
			get {
				return CallingConvention.Cdecl;
			}
		}

		public override string GetMangledMethodName (MethodInfo methodInfo)
                {
			string methodName = methodInfo.Name;
                        MethodType methodType = GetMethodType (methodInfo);
			ParameterInfo[] parameters = methodInfo.GetParameters ();

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

			if (parameters.Length == argStart) { // no args (other than C++ "this" object)
				nm.Append ("v");
			} else for (int i = argStart; i < parameters.Length; i++) {
				nm.Append (GetTypeCode (Modifiers.GetMangleInfo (parameters[i])));
			}

			return nm.ToString ();
		}

		protected virtual string GetTypeCode(MangleAsAttribute mangleInfo) {

                        Type type = mangleInfo.MangleType;
                        Type element = type;
                        bool cppObj = typeof (ICppObject).IsAssignableFrom (element);

                        StringBuilder code = new StringBuilder ();

                        if (type.IsArray)
                        {
                                code.Append ("P");
                                element = type.GetElementType ();
                        }

                        if (type.IsByRef)
                        {
                                code.Append ("R");
                                element = type.GetElementType ();
                        }
                        else
                        if (mangleInfo.Modifiers == CppModifiers.PointerToConst)
                                code.Append ("PK");
                        else
                        if (cppObj || mangleInfo.Modifiers == CppModifiers.Pointer || mangleInfo.Modifiers == CppModifiers.ConstPointer)
                                code.Append ("P");

                        if (element.Equals (typeof (string)))
                                code.Append ("P");

                        if (mangleInfo.Modifiers == CppModifiers.Const)
                                code.Append ("K");

			     if (element.Equals (typeof (int))) code.Append ("i");
                        else if (element.Equals (typeof (ushort))) code.Append ("t");
                        else if (element.Equals (typeof (string))) code.Append ("c"); // char
                        else if (cppObj || element.StructLayoutAttribute.Value != LayoutKind.Auto) {
                                code.Append(element.Name.Length);
                                code.Append(element.Name);
                        }
                        else throw new NotSupportedException ("Unsupported parameter type: " + type.ToString ());

                        return code.ToString ();
		}

	}
}