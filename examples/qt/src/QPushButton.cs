using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {
	public partial class QPushButton {
		
		public QPushButton (QString text, QWidget parent)
			: this (ref text, parent)
		{
		}
		
		public QPushButton (QString text)
			: this (ref text, null)
		{
		}
		
	}
}

