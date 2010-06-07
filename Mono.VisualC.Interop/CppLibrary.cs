//
// Mono.VisualC.Interop.CppLibrary.cs: Represents a native C++ library for interop
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//


using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Reflection;
using System.Reflection.Emit;

using Mono.VisualC.Interop.ABI;

namespace Mono.VisualC.Interop
{

	public sealed class CppLibrary
        {
		internal static AssemblyBuilder interopAssembly;
                internal static ModuleBuilder interopModule;

		private CppAbi abi;
		private string name;

		static CppLibrary ()
                {
			AssemblyName assemblyName = new AssemblyName ("__CPPLibraryImplAssembly");
                        string moduleName = "__CPPLibraryImplModule";

			interopAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.Run);
                        interopModule = interopAssembly.DefineDynamicModule (moduleName);
		}

		public CppLibrary (string name, CppAbi abi)
                {

			if (name == null)
				throw new ArgumentNullException ("Name cannot be NULL.");
                        if (abi == null)
                                throw new ArgumentNullException ("Abi cannot be NULL.");

                        this.name = name;
			this.abi = abi;
		}

		public string Name {
			get { return name; }
		}

		public CppAbi Abi {
			get { return abi; }
		}

		// For a class that may have fields with no virtual methods to be overridden
		public Iface GetClass<Iface,NativeLayout> (string className)
                                where Iface : ICppClassInstantiatable
                                where NativeLayout : struct
                {

			return Abi.ImplementClass<Iface, NativeLayout> (null, Name, className);
		}

		// For a class that may have fields and virtual methods to be overridden
		public Iface GetClass<Iface,NativeLayout,Managed> (string className)
                                where Iface : ICppClassOverridable<Managed>
                                where NativeLayout : struct
                                where Managed : ICppObject
                {

			return Abi.ImplementClass<Iface, NativeLayout> (typeof (Managed), Name, className);
		}
		// TODO: Define a method for pure virtual classes (no NativeLayout)?


	}

}
