using System;
using System.Diagnostics;
using Qt.Gui;

using Mono.Cxxi;

namespace QtTest {
	class MainClass {
		public static void Main (string[] args)
		{
			using (QApplication app = new QApplication ()) {
				using (QPushButton hello = new QPushButton ("Hello world!", null)) {

					hello.Resize (new QSize (100, 30));
					
					hello.SetVisible (true);
					QApplication.Exec ();
				}
			}
		}
	}
}

