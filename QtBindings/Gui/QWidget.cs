using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

using Qt.Core;

namespace Qt.Gui {
        public class QWidget : QObject {
                #region Sync with qwidget.h
                // C++ interface
                public interface IQWidget : ICppClassOverridable<QWidget>, Base<QObject.IQObject>, Base<QPaintDevice.IQPaintDevice> {
                        // ...
                        void QWidget (CppInstancePtr @this, QWidget parent, /*Qt::WindowFlags */ int f);
                        // ...
                        [Virtual] void setVisible (CppInstancePtr @this, bool visible);
                        // ...
                        void resize (CppInstancePtr @this, [MangleAs (typeof (QSize), ByRef=true, Modifiers=CppModifiers.Const)] ref QSize size);
                        // ...
                        [Virtual] /*QSize*/ int sizeHint (CppInstancePtr @this);
                        [Virtual] /*QSize*/ int minimumSizeHint (CppInstancePtr @this);
                        // ...
                        [Virtual] int heightForWidth (CppInstancePtr @this, int width);
                        // ... protected:
                        [Virtual] void mousePressEvent (CppInstancePtr @this, /*QMouseEvent */ IntPtr p);
                        [Virtual] void mouseReleaseEvent (CppInstancePtr @this, /*QMouseEvent */ IntPtr p);
                        [Virtual] void mouseDoubleClickEvent (CppInstancePtr @this, /*QMouseEvent */ IntPtr p);
                        [Virtual] void mouseMoveEvent (CppInstancePtr @this, /*QMouseEvent */ IntPtr p);
                        [Virtual] void wheelEvent (CppInstancePtr @this, /*QWheelEvent */ IntPtr p);
                        [Virtual] void keyPressEvent (CppInstancePtr @this, /*QKeyEvent */ IntPtr p);
                        [Virtual] void keyReleaseEvent (CppInstancePtr @this, /*QKeyEvent */ IntPtr p);
                        [Virtual] void focusInEvent (CppInstancePtr @this, /*QFocusEvent */ IntPtr p);
                        [Virtual] void focusOutEvent (CppInstancePtr @this, /*QFocusEvent */ IntPtr p);
                        [Virtual] void enterEvent (CppInstancePtr @this, /*QEvent */ IntPtr p);
                        [Virtual] void leaveEvent (CppInstancePtr @this, /*QEvent */ IntPtr p);
                        [Virtual] void paintEvent (CppInstancePtr @this, /*QPaintEvent */ IntPtr p);
                        [Virtual] void moveEvent (CppInstancePtr @this, /*QMoveEvent */ IntPtr p);
                        [Virtual] void resizeEvent (CppInstancePtr @this, /*QResizeEvent */ IntPtr p);
                        [Virtual] void closeEvent (CppInstancePtr @this, /*QCloseEvent */ IntPtr p);
                        [Virtual] void contextMenuEvent (CppInstancePtr @this, /*QContextMenuEvent */ IntPtr p);
                        [Virtual] void tabletEvent (CppInstancePtr @this, /*QTabletEvent */ IntPtr p);
                        [Virtual] void actionEvent (CppInstancePtr @this, /*QActionEvent */ IntPtr p);
                        [Virtual] void dragEnterEvent (CppInstancePtr @this, /*QDragEnterEvent */ IntPtr p);
                        [Virtual] void dragMoveEvent (CppInstancePtr @this, /*QDragMoveEvent */ IntPtr p);
                        [Virtual] void dragLeaveEvent (CppInstancePtr @this, /*QDragLeaveEvent */ IntPtr p);
                        [Virtual] void dropEvent (CppInstancePtr @this, /*QDropEvent */ IntPtr p);
                        [Virtual] void showEvent (CppInstancePtr @this, /*QShowEvent */ IntPtr p);
                        [Virtual] void hideEvent (CppInstancePtr @this, /*QHideEvent */ IntPtr p);
                        [Virtual] bool macEvent (CppInstancePtr @this, /*EventHandlerCallRef */ IntPtr p1, /*EventRef */ IntPtr p2);
                        [Virtual] void changeEvent (CppInstancePtr @this, /*QEvent */ IntPtr p);
                        // ...
                        [Virtual] void inputMethodEvent (CppInstancePtr @this, /*QInputMethodEvent */ IntPtr p);

                        //public:
                        [Virtual] /*QVariant*/ IntPtr inputMethodQuery (CppInstancePtr @this, /*Qt::InputMethodQuery */ int x);
                        // ... protected:
                        [Virtual] bool focusNextPrevChild (CppInstancePtr @this, bool next);

                        // TODO: Determine correct number of vtable slots here too...
                        [Virtual] void foo1 (CppInstancePtr @this);
                        [Virtual] void foo2 (CppInstancePtr @this);
                        [Virtual] void foo3 (CppInstancePtr @this);
                        [Virtual] void foo4 (CppInstancePtr @this);
                        [Virtual] void foo5 (CppInstancePtr @this);
                        [Virtual] void foo6 (CppInstancePtr @this);
                        [Virtual] void foo7 (CppInstancePtr @this);
                        [Virtual] void foo8 (CppInstancePtr @this);
                }
                // C++ fields
                private struct _QWidget {
                        public IntPtr data;
                }
                #endregion

                private static IQWidget impl = Qt.Libs.QtGui.GetClass<IQWidget,_QWidget,QWidget> ("QWidget");

                // TODO: ctor ...

                public QWidget (IntPtr native) : base (native)
                {
                }

                public bool Visible {
                        get {
                                throw new NotImplementedException ();
                        }
                        set {
                                impl.setVisible (native, value);
                        }
                }

                public void Resize (int width, int height)
                {
                        QSize s = new QSize (width, height);
                        impl.resize (native, ref s);
                }

                // TODO: HELP! I think this really should be:
                //  sizeof(QWidget) [impl.NativeSize] + sizeof(QObject) [base.NativeSize] + sizeof(QPaintDevice) [????]
                // Works for now because we're already alloc'ing too much memory!!? (NativeSize property contains vtbl pointer)
                public override int NativeSize {
                        get { return impl.NativeSize + base.NativeSize; }
                }

                public override void Dispose ()
                {
                        throw new NotImplementedException ();
                }

        }
}

