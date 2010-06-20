using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

namespace Qt.Core {
        [StructLayout (LayoutKind.Sequential)]
        public struct QSize {
                public int wd;
                public int ht;

                public QSize (int w, int h)
                {
                        this.wd = w;
                        this.ht = h;
                }
        }
}

