using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CppSharp.Runtime
{
    public struct VTables
    {
        public Delegate[][] Methods { get; set; }
        public IntPtr[] Tables { get; set; }
        private ConcurrentDictionary<(short, short, int), Delegate> Specializations;

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
                    Methods[table][slot] = method = MarshalUtil.GetDelegate<T>(Tables, table, slot);

                return (T)method;
            }
            else
            {
                if (Specializations == null)
                    Specializations = new ConcurrentDictionary<(short, short, int), Delegate>();

                var key = (specialiation, table, slot);

                if (!Specializations.TryGetValue(key, out var method))
                    Specializations[key] = method = MarshalUtil.GetDelegate<T>(Tables, table, slot);

                return (T)method;
            }
        }

        public unsafe struct ManagedVTable
        {
            private static readonly List<SafeUnmanagedMemoryHandle> cache = new List<SafeUnmanagedMemoryHandle>();
            public IntPtr* Entries { get; }

            public ManagedVTable(IntPtr instance, int offset, int size, bool useGlobalCache = true)
            {
                var sizeInBytes = size * sizeof(IntPtr);                
                var src = ((*(IntPtr*)instance) + offset).ToPointer();
                Entries = (IntPtr*)Marshal.AllocHGlobal(sizeInBytes);

                Buffer.MemoryCopy(src, Entries, sizeInBytes, sizeInBytes);

                if (useGlobalCache)
                { 
                    lock (cache)
                        cache.Add(new SafeUnmanagedMemoryHandle((IntPtr)Entries, true));
                }
            }
        }
    }
}
