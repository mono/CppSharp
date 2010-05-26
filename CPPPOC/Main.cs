// Main.cs created with MonoDevelop
// User: alex at 17:40 03/14/2009
//
using System;
using Mono.VisualC.Interop;

using System.Runtime.InteropServices;

namespace CPPPOC
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			// bind all wrapper classes to their native implementations
			CppLibrary cppTest = new CppLibrary("CPPTest", new Mono.VisualC.Interop.ABI.Itanium());
			CSimpleClass.Bind(cppTest);

			CSimpleClass csc1 = new CSimpleClass(CreateCSimpleSubClass(10));
			CSimpleClass csc2 = new CSimpleClass(2);
			try {

				csc1.M0();
				Console.WriteLine("Managed code got value: {0}", csc1.value);
				csc2.M0();
				Console.WriteLine("Managed code got value: {0}", csc2.value);

				csc1.value = 100;
				csc1.V2();

				csc2.value = 200;
				csc2.V2();

			} finally {
				DestroyCSimpleSubClass(csc1.Native);
				csc1.Dispose();
				csc2.Dispose();
			}
		}

		[DllImport("CPPTest")]
		public static extern IntPtr CreateCSimpleSubClass(int value);

		[DllImport("CPPTest")]
		public static extern void DestroyCSimpleSubClass(IntPtr obj);
	}
}