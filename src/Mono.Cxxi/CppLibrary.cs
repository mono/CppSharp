//
// Mono.Cxxi.CppLibrary.cs: Represents a native C++ library for interop
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
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Reflection;
using System.Reflection.Emit;

using Mono.Cxxi.Abi;

namespace Mono.Cxxi {

	public enum InlineMethods {

		// Normally, C++ inline methods are not exported from the library, so C++ interop cannot call them.
		//  This is the default option. It throws a NotImplementedException if you try to call the native version of one of these methods.
		//  Use this if you reimplement the inline methods in managed code, or if they are not to be available in the bindings.
		NotPresent,

		// Expect the inline methods to be present in the specified library
		//  For example, if the library was compiled with GCC's -fkeep-inline-functions option
		Present,

		// Expect the inline methods to be exported in a separate library named %name%-inline
		SurrogateLib,
	}

	public sealed class CppLibrary {
		internal static AssemblyBuilder interopAssembly;
		internal static ModuleBuilder interopModule;

		public CppAbi Abi { get; private set; }
		public string Name { get; private set; }
		public InlineMethods InlineMethodPolicy { get; private set; }

		static CppLibrary ()
		{
			AssemblyName assemblyName = new AssemblyName ("__CppLibraryImplAssembly");
			string moduleName = "CppLibraryImplAssembly.dll";

			interopAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.RunAndSave);
			interopModule = interopAssembly.DefineDynamicModule (moduleName, moduleName, true);
		}

		public CppLibrary (string name)
			: this (name, InlineMethods.NotPresent)
		{
		}

		public CppLibrary (string name, InlineMethods inlinePolicy)
			: this (name, ItaniumAbi.Instance, inlinePolicy)
		{
			//FIXME: Ideally we would auto-detect ABI here.
		}

		public CppLibrary (string name, CppAbi abi, InlineMethods inlinePolicy)
		{
			if (name == null)
				throw new ArgumentNullException ("Name cannot be NULL.");
			if (abi == null)
				throw new ArgumentNullException ("Abi cannot be NULL.");

			this.Name = name;
			this.Abi = abi;
			this.InlineMethodPolicy = inlinePolicy;
		}

		// Mainly for debugging at this point
		public static void SaveInteropAssembly ()
		{
			interopAssembly.Save ("CppLibraryImplAssembly.dll");
		}

		// For working with a class that you are not instantiating
		//  from managed code and where access to fields is not necessary
		public Iface GetClass<Iface> (string className)
			where Iface : ICppClass
		{
			var typeInfo = Abi.MakeTypeInfo (this, className, typeof (Iface), null, null);
			return (Iface)Abi.ImplementClass (typeInfo);
		}

		// For instantiating or working with a class that may have fields
		//  but where overriding virtual methods in managed code is not necessary
		public Iface GetClass<Iface,NativeLayout> (string className)
			where Iface : ICppClassInstantiatable
			where NativeLayout : struct
		{
			var typeInfo = Abi.MakeTypeInfo (this, className, typeof (Iface), typeof (NativeLayout), null);
			return (Iface)Abi.ImplementClass (typeInfo);
		}

		/* The most powerful override. Allows the following from managed code:
		 *      + Instantiation
		 *      + Field access
		 *      + Virtual method overriding
		 */
		public Iface GetClass<Iface,NativeLayout,Managed> (string className)
			where Iface : ICppClassOverridable<Managed>
			where NativeLayout : struct
			where Managed : ICppObject
		{
			var typeInfo = Abi.MakeTypeInfo (this, className, typeof (Iface), typeof (NativeLayout), typeof (Managed));
			return (Iface)Abi.ImplementClass (typeInfo);
		}

	}
}
