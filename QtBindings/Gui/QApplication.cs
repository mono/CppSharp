using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

using Qt.Core;

namespace Qt.Gui {
        public class QApplication : QCoreApplication {
                #region Sync with qapplication.h
                // C++ interface
                public interface IQApplication : ICppClassOverridable<QApplication>, Base<QCoreApplication.IQCoreApplication> {
                        // ...
                        void QApplication (CppInstancePtr @this, [MangleAs ("int&")] IntPtr argc,
                                               [MangleAs ("char**")] IntPtr argv, int version);
                        // ...
                        [Virtual] bool macEventFilter(CppInstancePtr @this, IntPtr eventHandlerCallRef, IntPtr eventRef);
                        // ...
                        [Virtual] void commitData(CppInstancePtr @this, IntPtr qSessionManager); // was QSessionManager&
                        [Virtual] void saveState(CppInstancePtr @this, IntPtr qSessionManager);  // was QSessionManager&
                        // ...
                        [Static] int exec ();
                }
                // C++ fields
                private struct _QApplication {
                }
                #endregion

                private static IQApplication impl = Qt.Libs.QtGui.GetClass<IQApplication,_QApplication,QApplication> ("QApplication");

                public QApplication () : base (IntPtr.Zero)
                {
                        this.native = impl.Alloc (this);
                        InitArgcAndArgv ();
                        impl.QApplication (native, argc, argv, QGlobal.QT_VERSION);
                }

                public QApplication (IntPtr native) : base (native)
                {
                }

                public override int Exec ()
                {
                        return impl.exec ();
                }

                public override int NativeSize {
                        get { return impl.NativeSize + base.NativeSize; }
                }

                public override void Dispose ()
                {
                        impl.Destruct (native);
                        FreeArgcAndArgv ();
                        native.Dispose ();
                }

        }
}

