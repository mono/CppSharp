using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

namespace Qt.Core {
        public class QObject : ICppObject {
                #region Sync with qobject.h
                // C++ interface
                protected interface IQObject : ICppClassOverridable<QObject> {
                    void QObject (CppInstancePtr @this, [MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof (CppObjectMarshaler))] QObject parent);
                    [Virtual] bool @event (CppInstancePtr @this, IntPtr qEvent);
                    [Virtual] bool eventFilter (CppInstancePtr @this, IntPtr qObject, IntPtr qEvent);
                    [Virtual] void timerEvent (CppInstancePtr @this, IntPtr qTimerEvent);
                    [Virtual] void childEvent (CppInstancePtr @this, IntPtr qChildEvent);
                    [Virtual] void customEvent (CppInstancePtr @this, IntPtr qEvent);
                    [Virtual] void connectNotify (CppInstancePtr @this, string signal);
                    [Virtual] void disconnectNotify (CppInstancePtr @this, string signal);
                }
                // C++ fields
                private struct _QObject {
                        public IntPtr d_ptr;
                }
                #endregion

                private static IQObject impl = Qt.Libs.QtCore.GetClass<IQObject,_QObject,QObject> ("QObject");
                protected CppInstancePtr native;

                public QObject ()
                {
                        native = impl.Alloc (this);
                        impl.QObject (native, null);
                }

                public QObject (QObject parent)
                {
                        native = impl.Alloc (this);
                        impl.QObject (native, parent);
                }

                public QObject (IntPtr native)
                {
                        this.native = native;
                }

                public IntPtr Native {
                        get { return (IntPtr)native; }
                }

                public virtual int NativeSize {
                        get { return impl.NativeSize; }
                }

                public virtual void Dispose ()
                {
                        impl.Destruct (native);
                        native.Dispose ();
                }

        }
}

