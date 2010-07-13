using System;
using Qt.Gui;

using Mono.VisualC.Interop;

namespace QtTest {
        class MainClass {
                public static void Main (string[] args)
                {
                    //System.Diagnostics.Debug.Assert(false, "Whao");
                        using (QApplication app = new QApplication ()) {
                                using (QPushButton hello = new QPushButton ("Hello world!")) {

                                        hello.Resize (100, 30);
					CppLibrary.SaveInteropAssembly ();
                                        hello.Visible = true;

                                        app.Exec ();


                                }
                        }
                }
        }
}

