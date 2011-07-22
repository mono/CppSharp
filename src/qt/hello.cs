using System;
using Qt.Gui;

using Mono.Cxxi;

namespace QtTest {
	class MainClass {
		public static void Main (string[] args)
		{
			int argc = args.Length;
			using (QApplication app = new QApplication (ref argc, args, 0x040602)) {
				QPushButton hello = new QPushButton ("Hello", null);
				hello.Resize (100, 30);
				hello.Show ();
				QApplication.Exec ();
			}
		}
	}
}

