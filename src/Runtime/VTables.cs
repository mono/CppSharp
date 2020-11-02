using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CppSharp.Runtime
{
    public unsafe struct VTables
    {
        public Delegate[][] Methods { get; }
        public IntPtr[] Tables { get; }
        private Dictionary<(short, short, int), Delegate> Specializations;

        public VTables(IntPtr[] tables, Delegate[][] methods = null)
        {
            Tables = tables;
            Methods = methods;
            Specializations = null;
        }

        public bool IsEmpty => Tables == null;
        public bool IsTransient => Methods == null;

        public T GetMethodDelegate<T>(short table, int slot, short specialiation = 0) where T : Delegate
        {
            if (specialiation == 0 && !IsTransient)
            {
                var method = Methods[table][slot];

                if (method == null)
                    method = Methods[table][slot] = MarshalUtil.GetDelegate<T>(Tables, table, slot);

                return (T)method;
            }
            else
            {
                if (Specializations == null)
                    Specializations = new Dictionary<(short, short, int), Delegate>();

                var key = (specialiation, table, slot);

                if (!Specializations.TryGetValue(key, out var method))
                    method = Specializations[key] = MarshalUtil.GetDelegate<T>(Tables, table, slot);

                return (T)method;
            }
        }
    }
}