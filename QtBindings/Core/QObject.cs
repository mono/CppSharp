using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

namespace Qt.Core {
        public class QObject : ICppObject {
                #region Sync with qobject.h
                // C++ interface
                public interface IQObject : ICppClassOverridable<QObject> {
                        // ...
                        [Virtual] /*QMetaObject */ IntPtr metaObject(CppInstancePtr @this);
                        [Virtual] /*void */ IntPtr qt_metacast(CppInstancePtr @this, string s);
                        [Virtual] int qt_metacall(CppInstancePtr @this, /*QMetaObject::Call */ int qMetaObjectCall, int x, /*void **/ IntPtr p);
                        // ...
                        void QObject (CppInstancePtr @this, QObject parent);
                        // ...
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

                public QObject (QObject parent)
                {
                        native = impl.Alloc (this);
                        impl.QObject (native, parent);
                }

                public QObject () : this (null)
                {
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

