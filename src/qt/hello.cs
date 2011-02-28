using System;
using Qt.Gui;

using Mono.VisualC.Interop;

namespace QtTest {
	class MainClass {
		public static void Main (string[] args)
		{
			int argc = args.Length;
			using (QApplication app = new QApplication (ref argc, args, 0x040602)) {
				QPushButton hello = new QPushButton (QString.FromUtf8 ("Hello", 5), null);
				hello.Resize (100, 30);
				hello.Show ();
				CppLibrary.SaveInteropAssembly ();
				QApplication.Exec ();
			}
						/*
				using (QPushButton hello = new QPushButton ("Hello world!"),
						   hello2 = new QPushButton ("Another button")) {

					hello.Resize (100, 30);
					hello2.Resize (200, 30);

					//CppLibrary.SaveInteropAssembly ();
					hello.Visible = true;
					hello2.Visible = true;

					app.Exec ();


				}
			}
						*/
		}
	}
}

