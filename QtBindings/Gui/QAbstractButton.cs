using System;
using Mono.VisualC.Interop;

namespace Qt.Gui {
        public class QAbstractButton : QWidget {
                 #region Sync with qabstractbutton.h
                // C++ interface
                public interface IQAbstractButton : ICppClassOverridable<QAbstractButton> {
                        [Virtual] void paintEvent (CppInstancePtr @this, /*QPaintEvent */ IntPtr e); // abstract
                        [Virtual] bool hitButton (CppInstancePtr @this, /*const QPoint &*/ IntPtr pos);
                        [Virtual] void checkStateSet (CppInstancePtr @this);
                        [Virtual] void nextCheckState (CppInstancePtr @this);
                }
                private struct _QAbstractButton {
                }
                #endregion

		private static IQAbstractButton impl = Qt.Libs.QtGui.GetClass<IQAbstractButton, _QAbstractButton, QAbstractButton> ("QAbstractButton");

                public QAbstractButton (IntPtr native) : base (impl.TypeInfo)
                {
			Native = native;
                }

		internal QAbstractButton (CppTypeInfo subClass) : base (impl.TypeInfo)
		{
			subClass.AddBase (impl.TypeInfo);
		}

                public override void Dispose ()
                {
                }
        }
}

