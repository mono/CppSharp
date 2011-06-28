//
// Mono.Cxxi.Abi.ItaniumAbi.cs: An implementation of the Itanium C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010-2011 Alexander Corrado
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
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Mono.Cxxi.Util;

namespace Mono.Cxxi.Abi {
	public class ItaniumAbi : CppAbi {

		public static readonly ItaniumAbi Instance = new ItaniumAbi ();

		private bool? hasNonDefaultCopyCtorOrDtor;

		private ItaniumAbi ()
		{
		}

		public override Iface ImplementClass<Iface, NLayout> (Type wrapperType, CppLibrary lib, string className)
		{
			hasNonDefaultCopyCtorOrDtor = null;
			return base.ImplementClass<Iface, NLayout> (wrapperType, lib, className);
		}

		protected override CppTypeInfo MakeTypeInfo (IEnumerable<PInvokeSignature> methods)
		{
			return new ItaniumTypeInfo (this, GetVirtualMethodSlots (methods), layout_type, wrapper_type);
		}

		private IEnumerable<PInvokeSignature> GetVirtualMethodSlots (IEnumerable<PInvokeSignature> methods)
		{
			foreach (var method in methods) {
				if (!IsVirtual (method.OrigMethod))
					continue;

				yield return method;

				// Itanium has extra slot for virt dtor
				if (method.Type == MethodType.NativeDtor)
					yield return null;
			}
		}

		protected override MethodBuilder DefineMethod (PInvokeSignature sig, CppTypeInfo typeInfo, ref int vtableIndex)
		{
			var builder = base.DefineMethod (sig, typeInfo, ref vtableIndex);

			// increment vtableIndex an extra time for that extra vdtor slot (already incremented once in base)
			if (IsVirtual (sig.OrigMethod) && sig.Type == MethodType.NativeDtor)
				vtableIndex++;

			return builder;
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

			if (IsConst (methodInfo))
				nm.Append ('K');

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
					For.AnyInputIn  (CppModifiers.Volatile).After (ptrOrRef).Emit ("V"),
					For.AnyInputIn  (CppModifiers.Const).After (ptrOrRef).Emit ("K")
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
					code.Append (mangleType.ElementTypeName.Length);
					code.Append (mangleType.ElementTypeName);
				}
				break;
			}
			}

			return code.ToString ();
		}

		// Section 3.1.4:
		// Classes with non-default copy ctors/destructors are returned using a hidden
		// argument
		bool ReturnByHiddenArgument (MethodInfo method)
		{
			if (!IsByVal (method.ReturnTypeCustomAttributes))
				return false;

			if (hasNonDefaultCopyCtorOrDtor == null)
				hasNonDefaultCopyCtorOrDtor = GetMethods ().Any (m => (IsCopyConstructor (m) || GetMethodType (m) == MethodType.NativeDtor) && !IsArtificial (m));

			return hasNonDefaultCopyCtorOrDtor.Value;
		}

		public override PInvokeSignature GetPInvokeSignature (MethodInfo method)
		{
			var psig = base.GetPInvokeSignature (method);

			if (ReturnByHiddenArgument (method)) {
				psig.ParameterTypes.Insert (0, typeof (IntPtr));
				psig.ReturnType = typeof (void);
			}

			return psig;
		}

		protected override void EmitNativeCall (ILGenerator il, MethodInfo nativeMethod, PInvokeSignature psig, LocalBuilder nativePtr)
		{
			var method = psig.OrigMethod;
			LocalBuilder returnValue = null;
			var hiddenReturnByValue = ReturnByHiddenArgument (method);

			if (hiddenReturnByValue)
			{
				returnValue = il.DeclareLocal (typeof (CppInstancePtr));

				EmitGetTypeInfo (il, method.ReturnType);
				il.Emit (OpCodes.Call, typeinfo_nativesize);
				il.Emit (OpCodes.Newobj, cppip_fromsize);
				il.Emit (OpCodes.Stloc, returnValue);
				il.Emit (OpCodes.Ldloca, returnValue);
				il.Emit (OpCodes.Call, cppip_native);
			}

			base.EmitNativeCall (il, nativeMethod, psig, nativePtr);

			if (hiddenReturnByValue) {
				EmitCreateCppObjectFromNative (il, method.ReturnType, returnValue);
			}
		}


	}
}