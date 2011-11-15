using System;
using System.Diagnostics;
using Qt.Gui;
using Qt.Gui.Qt;

using Mono.Cxxi;

namespace QtTest {
	class MainClass {
		public static void Main (string[] args)
		{
			using (QApplication app = new QApplication ()) {
				using (QPushButton hello = new QPushButton ("Hello world!")) {

					hello.Resize (200, 30);
					QObject.Connect (hello, "2clicked()", app, "1aboutQt()", ConnectionType.AutoConnection);
					
					hello.SetVisible (true);
					QApplication.Exec ();
				}
			}
		}
	}
}

