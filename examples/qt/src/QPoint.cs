using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {

	[StructLayout (LayoutKind.Sequential)]
	public struct QPoint {

		private int xy, yx; //Wtf.. on Mac the order is y, x; elsewhere x, y

		public QPoint (int x, int y)
		{
			//FIXME: do some snazzy stuff to get this right.. for now, I'm a mac user :P
			this.yx = x;
			this.xy = y;
		}
	}
}

