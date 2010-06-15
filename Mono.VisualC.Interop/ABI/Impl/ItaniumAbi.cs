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

			StringBuilder nm = new StringBuilder("_ZN", 30);
			nm.Append(class_name.Length).Append(class_name);

			switch (methodType) {

				case MethodType.NativeCtor:
					nm.Append("C1");
					break;

				case MethodType.NativeDtor:
					nm.Append("D1");
					break;

				default:
					nm.Append(methodName.Length).Append(methodName);
					break;

			}

			nm.Append("E");

			if (parameters.Length == 1) { //only the C++ "this" object
				nm.Append("v");
			} else for (int i = 1; i < parameters.Length; i++) {
				nm.Append(GetTypeCode(parameters[i].ParameterType));
			}

			return nm.ToString();
		}

		protected virtual string GetTypeCode(Type t) {
			if (t.Equals(typeof(int))) return "i";
			throw new NotSupportedException("Unsupported parameter type: " + t.ToString());
		}

	}
}