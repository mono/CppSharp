using System;
using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

namespace Qt.Gui {
        public class QPaintDevice : ICppObject {
                #region Sync with qpaintdevice.h
                // C++ interface
                public interface IQPaintDevice : ICppClassOverridable<QPaintDevice> {
                        [Virtual] int devType (CppInstancePtr @this);
                        [Virtual] /*QPaintEngine */ IntPtr paintEngine (CppInstancePtr @this); // abstract
                        //...
                        void QPaintDevice (CppInstancePtr @this);
                        [Virtual] int metric (CppInstancePtr @this, /*PaintDeviceMetric*/ IntPtr metric);
                }
                // C++ fields
                private struct _QPaintDevice {
                        public ushort painters;
                }
                #endregion

                private static IQPaintDevice impl = Qt.Libs.QtGui.GetClass<IQPaintDevice,_QPaintDevice,QPaintDevice> ("QPaintDevice");
                protected CppInstancePtr native;

                /* no point to this - subclasses will call QPaintDevice (IntPtr.Zero)
                protected QPaintDevice ()
                {

                }
                */

                public QPaintDevice (IntPtr native)
                {
                        this.native = native;
                }

                public IntPtr Native {
                        get { return (IntPtr)native; }
                }

                public virtual int NativeSize {
                        get { return impl.NativeSize; }
                }

                public void Dispose ()
                {
                        throw new Exception ("This should never be called!");
                }
        }
}

