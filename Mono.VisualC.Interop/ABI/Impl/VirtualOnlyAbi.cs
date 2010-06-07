using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop.ABI;

namespace Mono.VisualC.Interop {

        public class VirtualOnlyAbi : CppAbi {

                public VirtualOnlyAbi (MakeVTableDelegate makeVTable, MemberFilter vtableOverrideFilter)
                {
                        this.makeVTableMethod = makeVTable;
                        this.vtableOverrideFilter = vtableOverrideFilter;
                }
                public VirtualOnlyAbi () { }

                public override MethodType GetMethodType (MethodInfo imethod)
                {
                        MethodType defaultType = base.GetMethodType (imethod);
                        if (defaultType == MethodType.NativeCtor || defaultType == MethodType.NativeDtor)
                                return MethodType.NoOp;
                        return defaultType;
                }

                public override string GetMangledMethodName (MethodInfo methodInfo)
                {
                        throw new NotSupportedException ("Name mangling is not supported by this class. All C++ interface methods must be declared virtual.");
                }

                public override CallingConvention DefaultCallingConvention {
                        get {
                                throw new NotSupportedException ("This class does not support this property.");
                        }
                }
        }
}

