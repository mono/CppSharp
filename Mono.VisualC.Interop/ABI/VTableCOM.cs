//
// Mono.VisualC.Interop.ABI.VTableCOM.cs: vtable implementation based on COM interop (reqiures mono patch)
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop.ABI {
        public class VTableCOM : VTable {

                public VTableCOM (Delegate[] entries) : base(entries)
                {
                }

                public override int EntrySize {
                        get { return Marshal.SizeOf (typeof (IntPtr)); }
                }

                public override T GetDelegateForNative<T> (IntPtr native, int index)
                {
                        return default(T);
                }
        }
}
