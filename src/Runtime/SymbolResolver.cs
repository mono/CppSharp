/* Copyright (c) 2013 Xamarin, Inc and contributors
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
  * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. */

using System;
using System.Runtime.InteropServices;

namespace CppSharp
{
    public static class SymbolResolver
    {
        static readonly string[] formats;
        static readonly Func<string, IntPtr> loadImage;
        static readonly Func<IntPtr, string, IntPtr> resolveSymbol;

        static SymbolResolver ()
        {
            switch (Environment.OSVersion.Platform)
            {
            case PlatformID.Unix:
            case PlatformID.MacOSX:
                loadImage = dlopen;
                resolveSymbol = dlsym;
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
                loadImage = LoadLibrary;
                resolveSymbol = GetProcAddress;
                formats = new[] { "{0}", "{0}.dll" };
                break;
            }
        }

        public static IntPtr LoadImage (ref string name)
        {
            foreach (var format in formats)
            {
                var attempted = string.Format (format, name);
                var ptr = loadImage (attempted);

                if (ptr == IntPtr.Zero)
                    continue;

                name = attempted;
                return ptr;
            }

            return IntPtr.Zero;
        }

        public static IntPtr ResolveSymbol (string name, string symbol)
        {
            var image = LoadImage(ref name);
            return ResolveSymbol(image, symbol);
        }

        public static IntPtr ResolveSymbol (IntPtr image, string symbol)
        {
            if (image != IntPtr.Zero)
                return resolveSymbol (image, symbol);

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
