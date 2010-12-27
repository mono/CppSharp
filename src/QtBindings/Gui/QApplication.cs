using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

using Qt.Core;

namespace Qt.Gui {
        public class QApplication : QCoreApplication {
                #region Sync with qapplication.h
                // C++ interface
                public interface IQApplication : ICppClassOverridable<QApplication> {
                        // ...
                        [Constructor] void QApplication (CppInstancePtr @this, [MangleAs ("int&")] IntPtr argc,
                                                         [MangleAs ("char**")] IntPtr argv, int version);
                        // ...
                        [Virtual] bool macEventFilter(CppInstancePtr @this, IntPtr eventHandlerCallRef, IntPtr eventRef);
                        // ...
                        [Virtual] void commitData(CppInstancePtr @this, IntPtr qSessionManager); // was QSessionManager&
                        [Virtual] void saveState(CppInstancePtr @this, IntPtr qSessionManager);  // was QSessionManager&
                        // ...
                        [Static] int exec ();

			[Virtual, Destructor] void Destruct (CppInstancePtr @this);
                }
                // C++ fields
                private struct _QApplication {
                }
                #endregion

                private static IQApplication impl = Qt.Libs.QtGui.GetClass<IQApplication,_QApplication,QApplication> ("QApplication");

                public QApplication () : base (impl.TypeInfo)
                {
                        Native = impl.Alloc (this);
                        InitArgcAndArgv ();
                        impl.QApplication (Native, argc, argv, QGlobal.QT_VERSION);
                }

                public QApplication (IntPtr native) : base (impl.TypeInfo)
                {
			Native = native;
                }

		internal QApplication (CppTypeInfo subClass) : base (impl.TypeInfo)
		{
			subClass.AddBase (impl.TypeInfo);
		}

                public override int Exec ()
                {
                        return impl.exec ();
                }

                public override void Dispose ()
                {
                        impl.Destruct (Native);
                        FreeArgcAndArgv ();
                        Native.Dispose ();
                }

        }
}

