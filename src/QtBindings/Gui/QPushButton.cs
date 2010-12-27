using System;
using Mono.VisualC.Interop;
using Qt.Core;

namespace Qt.Gui {
        public class QPushButton : QAbstractButton {
                 #region Sync with qpushbutton.h
                // C++ interface
                public interface IQPushButton : ICppClassOverridable<QPushButton> {
                        // ...
                        [Constructor] void QPushButton (CppInstancePtr @this, [MangleAs ("const QString &")] ref QString text, QWidget parent);
                        // ...
			[Virtual, Destructor] void Destruct (CppInstancePtr @this);
                }
                // C++ fields
                private struct _QPushButton {
                }
                #endregion

                private static IQPushButton impl = Qt.Libs.QtGui.GetClass<IQPushButton,_QPushButton,QPushButton> ("QPushButton");

                public QPushButton (string btnText, QWidget parent) : base (impl.TypeInfo)
                {
                        Native = impl.Alloc (this);

                        QString text = btnText;
                        impl.QPushButton (Native, ref text, parent);
                }

                public QPushButton (string text) : this (text, (QWidget)null)
                {
                }

                public QPushButton (IntPtr native) : base (impl.TypeInfo)
                {
			Native = native;
                }

		internal QPushButton (CppTypeInfo subClass) : base (impl.TypeInfo)
		{
			subClass.AddBase (impl.TypeInfo);
		}

                public override void Dispose ()
                {
                        impl.Destruct (Native);
                        Native.Dispose ();
                }
        }
}

