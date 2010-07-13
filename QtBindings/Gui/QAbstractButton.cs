using System;
using Mono.VisualC.Interop;

namespace Qt.Gui {
        public class QAbstractButton : QWidget {
                 #region Sync with qabstractbutton.h
                // C++ interface
                public interface IQAbstractButton : ICppClassOverridable<QAbstractButton>, Base<QWidget.IQWidget> {
                        // ...
                        void QAbstractButton (CppInstancePtr @this, QWidget parent);
                        // ...
                        [Virtual] void paintEvent (CppInstancePtr @this, /*QPaintEvent */ IntPtr e); // abstract
                        [Virtual] bool hitButton (CppInstancePtr @this, /*const QPoint &*/ IntPtr pos);
                        [Virtual] void checkStateSet (CppInstancePtr @this);
                        [Virtual] void nextCheckState (CppInstancePtr @this);
                }
                private struct _QAbstractButton {
                }
                #endregion

        

                public QAbstractButton (IntPtr native) : base (native)
                {
                }
                /*
                public override int NativeSize {
                        get { return impl.NativeSize + base.NativeSize; }
                }
                 */
                public override void Dispose ()
                {
                        throw new Exception ("This should never be called!");
                }
        }
}

