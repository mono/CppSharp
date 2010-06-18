using System;
using Qt.Core;


namespace QtTest {
        class MainClass {
                public static int Main (string[] args)
                {
                        using (QCoreApplication app = new QCoreApplication ()) {

                                return app.Exec ();
                        }
                }
        }
}

