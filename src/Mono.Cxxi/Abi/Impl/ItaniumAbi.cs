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

		private ItaniumAbi ()
		{
		}

		public override CppTypeInfo MakeTypeInfo (CppLibrary lib, string typeName, Type interfaceType, Type layoutType/*?*/, Type/*?*/ wrapperType)
		{
			return new ItaniumTypeInfo (lib, typeName, interfaceType, layoutType, wrapperType);
		}

		public override IEnumerable<PInvokeSignature> GetVirtualMethodSlots (CppTypeInfo typeInfo, Type interfaceType)
		{
			foreach (var method in base.GetVirtualMethodSlots (typeInfo, interfaceType)) {
				if (!IsVirtual (method.OrigMethod))
					continue;

				yield return method;

				// Itanium has extra slot for virt dtor
				if (method.Type == MethodType.NativeDtor)
					yield return null;
			}
		}

		internal override Delegate GetManagedOverrideTrampoline (CppTypeInfo typeInfo, int vtableIndex)
		{

			// FIXME: HACK! we really need to support by val return types for managed override trampolines
			if (typeInfo.VirtualMethods [vtableIndex] != null &&
				IsByVal (typeInfo.VirtualMethods [vtableIndex].OrigMethod.ReturnTypeCustomAttributes))
				return null;

			return base.GetManagedOverrideTrampoline (typeInfo, vtableIndex);
		}

		protected override MethodBuilder DefineMethod (CppTypeInfo typeInfo, PInvokeSignature sig, ref int vtableIndex)
		{
			var builder = base.DefineMethod (typeInfo, sig, ref vtableIndex);

			// increment vtableIndex an extra time for that extra vdtor slot (already incremented once in base)
			if (IsVirtual (sig.OrigMethod) && sig.Type == MethodType.NativeDtor)
				vtableIndex++;

			return builder;
		}
		
		public override CallingConvention? GetCallingConvention (MethodInfo methodInfo)
		{
			return CallingConvention.Cdecl;
		}

		protected override string GetMangledMethodName (CppTypeInfo typeInfo, MethodInfo methodInfo)
		{
			var compressMap = new Dictionary<string, int> ();
			var methodName = methodInfo.Name;
			var type = typeInfo.GetMangleType ();
			var className = type.ElementTypeName;

			MethodType methodType = GetMethodType (typeInfo, methodInfo);
			ParameterInfo [] parameters = methodInfo.GetParameters ();

			StringBuilder nm = new StringBuilder ("_ZN", 30);

			if (IsConst (methodInfo))
				nm.Append ('K');

			if (type.Namespaces != null) {
				foreach (var ns in type.Namespaces)
					nm.Append (GetIdentifier (compressMap, ns));
			}

			nm.Append (GetIdentifier (compressMap, className));

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
			case CppTypes.Enum:
				if (mangleType.Namespaces != null) {
					code.Append ('N');
					foreach (var ns in mangleType.Namespaces)
						code.Append (GetIdentifier (compressMap, ns));
				}

				code.Append (GetIdentifier (compressMap, mangleType.ElementTypeName));

				if (mangleType.Namespaces != null)
					code.Append ('E');
				break;

			}

			return code.ToString ();
		}

		string GetIdentifier (Dictionary<string, int> compressMap, string identifier)
		{
			int cid;
			if (compressMap.TryGetValue (identifier, out cid))
				return cid == 0 ? "S_" : ToBase36String (cid - 1);
			compressMap [identifier] = compressMap.Count;
			return identifier.Length.ToString () + identifier;
		}

		const string Base36 = "0123456789abcdefghijklmnopqrstuvwxyz";
		string ToBase36String (int input)
		{
			var result = new Stack<char> ();
			while (input != 0)
			{
				result.Push (Base36 [input % 36]);
				input /= 36;
			}
			return new string (result.ToArray ());
		}

		// Section 3.1.4:
		// Classes with non-default copy ctors/destructors are returned using a hidden
		// argument
		bool ReturnByHiddenArgument (CppTypeInfo typeInfo, MethodInfo method)
		{
			var iti = (ItaniumTypeInfo)typeInfo;

			if (!IsByVal (method.ReturnTypeCustomAttributes))
				return false;

			if (iti.has_non_default_copy_ctor_or_dtor == null)
				iti.has_non_default_copy_ctor_or_dtor = GetMethods (typeInfo.InterfaceType)
				                                       .Any (m => (IsCopyConstructor (m) ||
				                                                     GetMethodType (typeInfo, m) == MethodType.NativeDtor) &&
					                                              !IsArtificial (m));

			return iti.has_non_default_copy_ctor_or_dtor.Value;
		}

		public override PInvokeSignature GetPInvokeSignature (CppTypeInfo/*?*/ typeInfo, MethodInfo method)
		{
			var psig = base.GetPInvokeSignature (typeInfo, method);

			if (ReturnByHiddenArgument (typeInfo, method)) {
				psig.ParameterTypes.Insert (0, typeof (IntPtr));
				psig.ReturnType = typeof (void);
			}

			return psig;
		}

		protected override void EmitNativeCall (CppTypeInfo typeInfo, MethodInfo nativeMethod, PInvokeSignature psig, LocalBuilder nativePtr)
		{
			var il = typeInfo.emit_info.current_il;
			var method = psig.OrigMethod;
			var returnType = method.ReturnType;
			var hiddenReturnByValue = ReturnByHiddenArgument (typeInfo, method);

			LocalBuilder returnValue = null;

			if (hiddenReturnByValue)
			{
				returnValue = il.DeclareLocal (typeof (CppInstancePtr));

				if (typeof (ICppObject).IsAssignableFrom (returnType))
					il.Emit (OpCodes.Ldc_I4, GetTypeInfo (returnType).NativeSize);
				else if (returnType.IsValueType)
					il.Emit (OpCodes.Ldc_I4, Marshal.SizeOf (returnType));

				il.Emit (OpCodes.Newobj, cppip_fromsize);
				il.Emit (OpCodes.Stloc, returnValue);
				il.Emit (OpCodes.Ldloca, returnValue);
				il.Emit (OpCodes.Call, cppip_native);
			}

			base.EmitNativeCall (typeInfo, nativeMethod, psig, nativePtr);

			if (hiddenReturnByValue) {
				EmitCreateCppObjectFromNative (il, returnType, returnValue);

				if (returnType.IsValueType) {
					// FIXME: This dispose should prolly be in a Finally block..
					il.Emit (OpCodes.Ldloca, returnValue);
					il.Emit (OpCodes.Call, cppip_dispose);
				}
			}
		}


	}
}