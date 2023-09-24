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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CppSharp
{
    public static class SymbolResolver
    {
        static readonly string[] formats;
        static readonly Func<string, IntPtr> loadImage;
        static readonly Func<IntPtr, string, IntPtr> resolveSymbol;

        static SymbolResolver()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                loadImage = LoadLibrary;
                resolveSymbol = GetProcAddress;
                formats = new[] { "{0}", "{0}.dll" };
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    loadImage = dlopen_linux;
                    resolveSymbol = dlsym_linux;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    loadImage = dlopen_macos;
                    resolveSymbol = dlsym_macos;
                }
                else
                {
                    throw new NotImplementedException();
                }

                formats = new[] {
                    "{0}",
                    "{0}.so",
                    "{0}.dylib",
                    "lib{0}.so",
                    "lib{0}.dylib",
                    "{0}.bundle"
                };
            }
        }

        public static IntPtr LoadImage(ref string name)
        {
            var pathValues = Environment.GetEnvironmentVariable("PATH");
            var paths = new List<string>(pathValues == null ? new string[0] :
                pathValues.Split(Path.PathSeparator));
            paths.Insert(0, Directory.GetCurrentDirectory());
            paths.Insert(0, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            foreach (var format in formats)
            {
                // Search the Current or specified directory for the library
                string filename = string.Format(format, name);
                string attempted = null;
                foreach (var path in paths)
                {
                    var fullPath = Path.Combine(path, filename);
                    if (File.Exists(fullPath))
                    {
                        attempted = fullPath;
                        break;
                    }
                }
                if (attempted == null)
                    attempted = filename;

                var ptr = loadImage(attempted);

                if (ptr == IntPtr.Zero)
                    continue;

                name = attempted;
                return ptr;
            }

            return IntPtr.Zero;
        }

        public static IntPtr ResolveSymbol(string name, string symbol)
        {
            var image = LoadImage(ref name);
            return ResolveSymbol(image, symbol);
        }

        public static IntPtr ResolveSymbol(IntPtr image, string symbol)
        {
            if (image != IntPtr.Zero)
                return resolveSymbol(image, symbol);

            return IntPtr.Zero;
        }

        #region POSIX

        private const int RTLD_LAZY = 0x1;

        #region LINUX

        static IntPtr dlopen_linux(string path)
        {
            return dlopen_linux(path, RTLD_LAZY);
        }

        [DllImport("dl.so.2", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        static extern IntPtr dlopen_linux(string path, int flags);

        [DllImport("dl.so.2", EntryPoint = "dlsym", CharSet = CharSet.Ansi)]
        static extern IntPtr dlsym_linux(IntPtr handle, string symbol);

        #endregion

        #region MACOS

        static IntPtr dlopen_macos(string path)
        {
            return dlopen_macos(path, RTLD_LAZY);
        }

        [DllImport("dl", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        static extern IntPtr dlopen_macos(string path, int flags);

        [DllImport("dl", EntryPoint = "dlsym", CharSet = CharSet.Ansi)]
        static extern IntPtr dlsym_macos(IntPtr handle, string symbol);

        #endregion

        #endregion

        #region Win32

        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        #endregion

    }
}
