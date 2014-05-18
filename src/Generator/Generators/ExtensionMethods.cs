using CppSharp.AST;
using Interop = System.Runtime.InteropServices;

namespace CppSharp.Generators
{
    public static class ExtensionMethods
    {
        public static Interop.CallingConvention ToInteropCallConv(this CallingConvention convention)
        {
            switch (convention)
            {
                case CallingConvention.Default:
                    return Interop.CallingConvention.Winapi;
                case CallingConvention.C:
                    return Interop.CallingConvention.Cdecl;
                case CallingConvention.StdCall:
                    return Interop.CallingConvention.StdCall;
                case CallingConvention.ThisCall:
                    return Interop.CallingConvention.ThisCall;
                case CallingConvention.FastCall:
                    return Interop.CallingConvention.FastCall;
            }

            return Interop.CallingConvention.Winapi;
        }
    }
}
