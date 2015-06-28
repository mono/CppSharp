using System.Linq;
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

        public static bool CheckClassIsStructible(Class @class, Driver Driver)
        {
            if (!@class.DeclaredStruct)
                return false;

            if (@class.IsValueType)
                return true;
            if (@class.IsInterface || @class.IsStatic || @class.IsAbstract)
                return false;
            if (@class.Methods.Any(m => m.IsOperator))
                return false;

            var allTrUnits = Driver.ASTContext.TranslationUnits;
            foreach (var trUnit in allTrUnits)
            {
                foreach (var cls in trUnit.Classes)
                {
                    if (cls.Bases.Any(clss => clss.IsClass && clss.Class == @class))
                        return false;
                }
            }

            return true;
        }
    }
}
