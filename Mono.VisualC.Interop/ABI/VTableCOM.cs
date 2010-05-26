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

                public VTableCOM (Delegate[] entries) : base(entries)
                {
                        int managedOverrides = (from entry in entries
                                                where entry != null
                                                select entry).Count();

                        vtPtr = Marshal.AllocHGlobal ((EntryCount + managedOverrides) * EntrySize);
                        WriteOverrides (entries);
                }

               public override MethodInfo PrepareVirtualCall (MethodInfo target, ILGenerator callsite, FieldInfo vtableField,
                                                              LocalBuilder native, int vtableIndex)
               {
                        throw new System.NotImplementedException ();
               }

        }
}
