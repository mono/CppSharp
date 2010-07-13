using System;
using System.Text;
using System.Runtime.InteropServices;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

namespace Qt.Core {
        //TODO: Will this leak?
        [StructLayout (LayoutKind.Sequential)]
        public unsafe struct QString {
                #region Sync with qstring.h
                public interface IQString : ICppClass {
                        void QString(ref QString @this, [MangleAs ("const QChar*")] IntPtr unicode, int size);
                }

                [StructLayout (LayoutKind.Sequential)]
                public struct Data {
                        public int @ref;
                        public int alloc, size;
                        public IntPtr data;
                        public ushort clean;
                        public ushort simpletext;
                        public ushort righttoleft;
                        public ushort asciiCache;
                        public ushort capacity;
                        public ushort reserved;
                        public IntPtr array;
                }


                public Data* d;
                #endregion

                private static IQString impl = Qt.Libs.QtCore.GetClass<IQString> ("QString");

                public QString (string str)
                {
                        IntPtr strPtr = Marshal.StringToHGlobalUni (str);
                        impl.QString (ref this, strPtr, str.Length);
                        Marshal.FreeHGlobal (strPtr);

                        // TODO: I deref this on construction to let Qt free it when it's done with it.
                        //  My assumption is that this struct will only be used to interop with Qt and
                        //  no managed class is going to hold on to it.
                        this.DeRef ();
                }

                public static implicit operator QString (string str)
                {
                        return new QString (str);
                }

                public QString AddRef ()
                {
                        d->@ref++;
                        return this;
                }

                public QString DeRef ()
                {
                        d->@ref--;
                        return this;
                }
        }
}

