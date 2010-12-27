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
                        [Constructor] void QObject (CppInstancePtr @this, QObject parent);
			[Virtual, Destructor] void Destruct (CppInstancePtr @this);
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
		public CppInstancePtr Native { get; protected set; }

                public QObject (QObject parent)
                {
                        Native = impl.Alloc (this);
                        impl.QObject (Native, parent);
                }

                public QObject () : this ((QObject)null)
                {
                }

                public QObject (IntPtr native)
                {
                        Native = native;
                }

		internal QObject (CppTypeInfo subClass)
		{
			subClass.AddBase (impl.TypeInfo);
		}

                public virtual void Dispose ()
                {
                        impl.Destruct (Native);
                        Native.Dispose ();
                }

        }
}

