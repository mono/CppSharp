//
// Mono.VisualC.Interop.ABI.ItaniumAbi.cs: An implementation of the Itanium C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010 Alexander Corrado
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

		protected override CppTypeInfo MakeTypeInfo (IEnumerable<MethodInfo> methods, IEnumerable<MethodInfo> virtualMethods)
		{
			bool hasExplicitCopyCtor = false;
			bool hasExplicitDtor = false;

			foreach (var m in methods) {
				if (m.IsDefined (typeof (CopyConstructorAttribute), false) && !m.IsDefined (typeof (ArtificialAttribute), false))
					hasExplicitCopyCtor = true;
				if (m.IsDefined (typeof (DestructorAttribute), false) && !m.IsDefined (typeof (ArtificialAttribute), false))
					hasExplicitDtor = true;
			}

			return new ItaniumTypeInfo (this, virtualMethods, layout_type) { HasExplicitCopyCtor = hasExplicitCopyCtor, HasExplicitDtor = hasExplicitDtor };
		}

		public override CallingConvention? GetCallingConvention (MethodInfo methodInfo)
		{
			return CallingConvention.Cdecl;
		}

		protected override string GetMangledMethodName (MethodInfo methodInfo)
		{
			var compressMap = new Dictionary<string, int> ();

			string methodName = methodInfo.Name;
			MethodType methodType = GetMethodType (methodInfo);
			ParameterInfo [] parameters = methodInfo.GetParameters ();

			StringBuilder nm = new StringBuilder ("_ZN", 30);
			nm.Append (class_name.Length).Append (class_name);
			compressMap [class_name] = compressMap.Count;

			// FIXME: Implement compression completely

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
					nm.Append (GetTypeCode (GetMangleType (parameters [i], parameters [i].ParameterType), compressMap));

			return nm.ToString ();
		}

		public virtual string GetTypeCode (CppType mangleType) {
			return GetTypeCode (mangleType, new Dictionary<string, int> ());
		}

		string GetTypeCode (CppType mangleType, Dictionary<string, int> compressMap)
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
			case CppTypes.Enum: {
				int cid;
				if (compressMap.TryGetValue (mangleType.ElementTypeName, out cid)) {
					if (cid == 0)
						code.Append ("S_");
					else
						throw new NotImplementedException ();
				} else {
					code.Append(mangleType.ElementTypeName.Length);
					code.Append(mangleType.ElementTypeName);
				}
				break;
			}
			}

			return code.ToString ();
		}

		public override PInvokeSignature GetPInvokeSignature (CppTypeInfo typeInfo, MethodInfo method) {
			Type returnType = method.ReturnType;

			bool retClass = false;
			bool retByAddr = false;

			if (typeof (ICppObject).IsAssignableFrom (returnType)) {
				retClass = true;
				if ((typeInfo as ItaniumTypeInfo).HasExplicitCopyCtor ||
					(typeInfo as ItaniumTypeInfo).HasExplicitDtor) {
					// Section 3.1.4:
					// Classes with non-default copy ctors/destructors are returned using a
					// hidden argument
					retByAddr = true;
				}
			}

			ParameterInfo[] parameters = method.GetParameters ();

			var ptypes = new List<Type> ();
			var pmarshal = new List<ParameterMarshal> ();
			// FIXME: Handle ByVal attributes
			foreach (var pi in parameters) {
				Type t = pi.ParameterType;
				Type ptype = t;
				ParameterMarshal m = ParameterMarshal.Default;

				if (t == typeof (bool)) {
					ptype = typeof (byte);
				} else if (typeof (ICppObject).IsAssignableFrom (t) && t != typeof (CppInstancePtr)) {
					if (pi.IsDefined (typeof (ByValAttribute), false)) {
						m = ParameterMarshal.ClassByVal;
						// Can't use an interface/cattr since the native layout type is private
						// Pass it as a type argument to I<Foo> ?
						var f = t.GetField ("native_layout", BindingFlags.Static|BindingFlags.NonPublic);
						if (f == null || f.FieldType != typeof (Type))
							throw new NotImplementedException ("Type '" + t + "' needs to have a 'native_layout' field before it can be passed by value.");
						ptype = (Type)f.GetValue (null);
					} else {
						ptype = typeof (IntPtr);
						m = ParameterMarshal.ClassByRef;
					}
				} else if (typeof (ICppObject).IsAssignableFrom (t)) {
					// CppInstancePtr implements ICppObject
					ptype = typeof (IntPtr);
				}
				ptypes.Add (ptype);
				pmarshal.Add (m);
			}

			if (retByAddr) {
				ptypes.Insert (0, typeof (IntPtr));
				pmarshal.Insert (0, ParameterMarshal.Default);
			}

			return new PInvokeSignature { OrigMethod = method, ParameterTypes = ptypes, ParameterMarshallers = pmarshal, ReturnType = returnType, ReturnClass = retClass, ReturnByAddr = retByAddr };
		}


	}
}
