using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {
	public partial class QWidget {
		
		public void Resize (int width, int height)
		{
			var size = new QSize (width, height);
			impl.resize (Native, ref size);
		}
		
	}
}

