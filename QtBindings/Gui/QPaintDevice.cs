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
                public CppInstancePtr Native { get; protected set; }

                public QPaintDevice (IntPtr native)
                {
                        Native = native;
                }

		internal QPaintDevice (CppTypeInfo subClass)
		{
			subClass.AddBase (impl.TypeInfo);
		}

                public void Dispose ()
                {
                }
        }
}

