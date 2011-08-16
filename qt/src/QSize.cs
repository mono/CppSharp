using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {

	[StructLayout (LayoutKind.Sequential)]
	public struct QSize {

		public int Width;
		public int Height;

		public QSize (int w, int h)
		{
			this.Width = w;
			this.Height = h;
		}
	}
}

