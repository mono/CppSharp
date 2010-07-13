using System;
using Mono.VisualC.Interop;
using Qt.Core;

namespace Qt.Gui {
        public class QPushButton : QAbstractButton {
                 #region Sync with qpushbutton.h
                // C++ interface
		[VirtualDestructor]
                public interface IQPushButton : ICppClassOverridable<QPushButton>, Base<QAbstractButton.IQAbstractButton> {
                        // ...
                        void QPushButton (CppInstancePtr @this, [MangleAs ("const QString &")] ref QString text, QWidget parent);
                        // ...
                }
                // C++ fields
                private struct _QPushButton {
                }
                #endregion

                private IQPushButton impl = Qt.Libs.QtGui.GetClass<IQPushButton,_QPushButton,QPushButton> ("QPushButton");

                public QPushButton (string btnText, QWidget parent) : base (IntPtr.Zero)
                {
                        this.native = impl.Alloc (this);

                        QString text = btnText;
                        impl.QPushButton (native, ref text, parent);
                }

                public QPushButton (string text) : this (text, (QWidget)null)
                {
                }

                public QPushButton (IntPtr native) : base (native)
                {
                }

                public override int NativeSize {
                        get { return impl.NativeSize + base.NativeSize; }
                }

                public override void Dispose ()
                {
                        impl.Destruct (native);
                        native.Dispose ();
                }
        }
}

