using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {

	[StructLayout (LayoutKind.Sequential)]
	public struct QFlags<T> where T : struct {

		public T Value;
	}
}

