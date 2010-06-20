using System;
using Qt.Gui;


namespace QtTest {
        class MainClass {
                public static void Main (string[] args)
                {
                        using (QApplication app = new QApplication ()) {
                                using (QPushButton hello = new QPushButton ("Hello world!")) {

                                        hello.Resize (100, 30);
                                        hello.Visible = true;

                                        app.Exec ();
                                }
                        }
                }
        }
}

