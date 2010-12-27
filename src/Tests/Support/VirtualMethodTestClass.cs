using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

namespace Tests
{
        public interface IVirtualMethodTestClass : ICppClass {
                [Virtual] void V0 (CppInstancePtr @this, int a1, int a2, int a3);
        }

        public static class VirtualMethodTestClass {

                public static CppInstancePtr Create ()
                {
                        return CreateVirtualMethodTestClass ();
                }

                public static void Destroy (CppInstancePtr vmtc)
                {
                        DestroyVirtualMethodTestClass ((IntPtr)vmtc);
                }


                [DllImport("CPPTestLib")]
                private static extern IntPtr CreateVirtualMethodTestClass ();

                [DllImport("CPPTestLib")]
                private static extern void DestroyVirtualMethodTestClass (IntPtr vmtc);
        }

}

