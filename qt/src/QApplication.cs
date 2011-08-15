using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {
	public partial class QApplication {
			
		
		public QApplication () : base (impl.TypeInfo)
		{
			InitArgcAndArgv ();
			Native = impl.QApplication (impl.Alloc (this), argc, argv, 0x040602);
		}
			
	}
}

