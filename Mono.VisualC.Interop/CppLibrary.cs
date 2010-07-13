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

namespace Mono.VisualC.Interop {
	public sealed class CppLibrary {
		internal static AssemblyBuilder interopAssembly;
                internal static ModuleBuilder interopModule;

		private CppAbi abi;
		private string name;

		static CppLibrary ()
                {
			AssemblyName assemblyName = new AssemblyName ("__CppLibraryImplAssembly");
                        string moduleName = "CppLibraryImplAssembly.dll";

			interopAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.RunAndSave);
                        interopModule = interopAssembly.DefineDynamicModule (moduleName, moduleName, true);
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

                        return Abi.ImplementClass<Iface> (null, Name, className);
                }

                // For instantiating or working with a class that may have fields
                //  but where overriding virtual methods in managed code is not necessary
		public Iface GetClass<Iface,NativeLayout> (string className)
                                where Iface : ICppClassInstantiatable
                                where NativeLayout : struct
                {

			return Abi.ImplementClass<Iface, NativeLayout> (null, Name, className);
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

			return Abi.ImplementClass<Iface, NativeLayout> (typeof (Managed), Name, className);
		}

        }
}
