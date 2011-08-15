using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {
	public partial class QApplication {

		public partial interface IQApplication {
			[Constructor] CppInstancePtr QApplication (CppInstancePtr @this, [MangleAs ("int&")] IntPtr argc, [MangleAs ("char**")] IntPtr argv, int version);
		}

		public QApplication () : base (impl.TypeInfo)
		{
			InitArgcAndArgv ();
			Native = impl.QApplication (impl.Alloc (this), argc, argv, 0x040602);
		}
			
	}
}

