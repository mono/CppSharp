using System;

namespace Templates {
	[Flags]
	public enum Context {
		Generic   = 0,
		Parameter = 1 << 0,
		Return    = 1 << 1,
		Interface = 1 << 2,
		Wrapper   = 1 << 3
	}
	public static class ContextExtensions {
		public static bool Is (this Context bits, Context bit)
		{
			return (bits & bit) == bit;
		}
	}
}

