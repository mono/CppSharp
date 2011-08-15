using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {

	[StructLayout (LayoutKind.Sequential)]
	public struct QSize {

		public int wd;
		public int ht;

		public QSize (int w, int h)
		{
			wd = w;
			ht = h;
		}
	}
}

