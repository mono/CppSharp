//
// Mono.Cxxi.Abi.SymbolResolver.cs: Platform-independent dynamic symbol lookup
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright 2011 Xamarin Inc  (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Runtime.InteropServices;

namespace Mono.Cxxi.Abi {
	internal static class SymbolResolver {

		static readonly string [] formats;
		static readonly Func<string,IntPtr> load_image;
		static readonly Func<IntPtr,string,IntPtr> resolve_symbol;

		static SymbolResolver ()
		{
			switch (Environment.OSVersion.Platform) {

			case PlatformID.Unix:
			case PlatformID.MacOSX:
				load_image = dlopen;
				resolve_symbol = dlsym;
				formats = new[] {
					"{0}",
					"{0}.so",
					"{0}.dylib",
					"lib{0}.so",
					"lib{0}.dylib",
					"{0}.bundle"
				};
				break;

			default:
				load_image = LoadLibrary;
				resolve_symbol = GetProcAddress;
				formats = new[] { "{0}", "{0}.dll" };
				break;
			}
		}

		// will fix up name with a more precise name to speed up later p/invokes (hopefully?)
		public static IntPtr LoadImage (ref string name)
		{
			foreach (var format in formats) {
				var attempted = string.Format (format, name);
				var ptr = load_image (attempted);
				if (ptr != IntPtr.Zero) {
					name = attempted;
					return ptr;
				}
			}
			return IntPtr.Zero;
		}

		public static IntPtr ResolveSymbol (IntPtr image, string symbol)
		{
			if (image != IntPtr.Zero)
				return resolve_symbol (image, symbol);
			return IntPtr.Zero;
		}

		#region POSIX

		static IntPtr dlopen (string path)
		{
			return dlopen (path, 0x0);
		}

		[DllImport ("dl", CharSet=CharSet.Ansi)]
		static extern IntPtr dlopen (string path, int flags);

		[DllImport ("dl", CharSet=CharSet.Ansi)]
		static extern IntPtr dlsym (IntPtr handle, string symbol);

		#endregion

		#region Win32

		[DllImport("kernel32", SetLastError=true)]
		static extern IntPtr LoadLibrary (string lpFileName);

		[DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
		static extern IntPtr GetProcAddress (IntPtr hModule, string procName);

		#endregion

	}
}

