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
using System.Runtime.InteropServices;
using CppSharp.Runtime;

namespace Std
{
    [StructLayout(LayoutKind.Sequential)]
    public struct String : ICppMarshal
    {
        public int _Mysize;
        public int _Myres;

        public int Length
        {
            get { return (int)_Mysize; }
        }

        public String(string data)
        {
            _Mysize = 0;
            _Myres = 0;
        }

        public static implicit operator string(Std.String str)
        {
            return string.Empty;
        }

        public static implicit operator String(string str)
        {
            return new String(str);
        }

        public char this[ulong index]
        {
            get { return '0'; }
            set { }
        }

        public int NativeDataSize { get { return Marshal.SizeOf(this); } }

        public void MarshalManagedToNative(IntPtr instance)
        {
            unsafe
            {
                var ptr = (String*) instance.ToPointer();
                ptr->_Mysize = _Mysize;
                ptr->_Myres = _Myres;
            }
        }

        public void MarshalNativeToManaged(IntPtr instance)
        {
            unsafe
            {
                var ptr = (String*) instance.ToPointer();
                _Mysize = ptr->_Mysize;
                _Myres = ptr->_Myres;
            }
        }
    }

    public static class ListStringExtensions
    {
        public static Std.Vector<Std.String> ToStd(this List<string> list)
        {
            var vec = new Std.Vector<Std.String>();

            foreach (var @string in list)
                vec.Add(new Std.String(@string));

            return vec;
        }
    }

    public struct WString
    {
        public IntPtr Instance;

        public WString(IntPtr instance)
        {
            Instance = instance;
        }

        public static implicit operator string(Std.WString str)
        {
            return string.Empty;
        }

        public static implicit operator WString(string str)
        {
            return new WString(IntPtr.Zero);
        }

        public char this[ulong index]
        {
            get { return '0'; }
            set { }
        }
    }
}