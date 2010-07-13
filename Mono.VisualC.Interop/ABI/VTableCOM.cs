//
// Mono.VisualC.Interop.ABI.VTableCOM.cs: vtable implementation based on COM interop (reqiures mono patch)
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop.ABI {
        public class VTableCOM : VTable {

                public static MakeVTableDelegate Implementation = (types, overrides, paddingTop, paddingBottom) => { return new VTableCOM (types, overrides, paddingTop, paddingBottom); };
                private VTableCOM (IList<Type> types, IList<Delegate> overrides, int paddingTop, int paddingBottom) : base(types, overrides, paddingTop, paddingBottom)
                {
                }

                public override MethodInfo PrepareVirtualCall (MethodInfo target, CallingConvention? callingConvention, ILGenerator callsite,
		                                               LocalBuilder nativePtr, FieldInfo vtableField, int vtableIndex)
               {
                        throw new System.NotImplementedException ();
               }

        }
}
