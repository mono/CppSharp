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

#define MSVC

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CppSharp.Runtime;

namespace Std
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Vector
    {
#if MSVC
        public byte* _Myfirst;
        public byte* _Mylast;
        public byte* _Myend;
#endif

        public long Size()
        {
            return _Mylast - _Myfirst;
        }

        public bool Capacity()
        {
            return (_Myend - _Myfirst) != 0;
        }

        public bool UnusedCapacity()
        {
            return (_Myend - _Mylast) != 0;
        }

        public bool HasUnusedCapacity()
        {
            return _Myend != _Mylast;
        }

        public void Add<T>(T item) where T : ICppMarshal
        {
            item.MarshalManagedToNative(new IntPtr(_Mylast));
            _Mylast += item.NativeDataSize;
        }

        public void ReserveNew(long count, int dataSize)
        {
            // Allocate some storage if we do not have any yet.
            if (_Myfirst == null)
            {
                _Myfirst = (byte*) Marshal.AllocHGlobal(dataSize);
                _Mylast = _Myfirst;
                _Myend = _Myfirst + dataSize;
            }

            // Ensure room for count new elements, grow exponentially.

        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector<T> : IList<T> where T : ICppMarshal
    {
        public Vector Internal;

        public int Count
        {
            get { return (int)Internal.Size(); }
        }

        public bool IsReadOnly { get; private set; }

        public Vector(Vector vector) : this()
        {
            Internal = vector;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var enumerator = new VectorEnumerator<T>(this);
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (!Internal.HasUnusedCapacity())
                Internal.ReserveNew(1, item.NativeDataSize);

            Internal.Add(item);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void Resize(int newSize)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public T this[int index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public static class ListExtensions
    {
        public static Std.Vector<T> ToStd<T>(this List<T> list)
            where T : ICppMarshal
        {
            var vec = new Std.Vector<T>();
            return vec;
        }
    }

    public unsafe class VectorEnumerator<T> : IEnumerator<T>
        where T : ICppMarshal
    {
        public Vector<T> Vector;
        public byte* Position;
        public int DataSize;

        public VectorEnumerator(Vector<T> vector)
        {
            Vector = vector;

            if (Vector.Count > 0)
                DataSize = Vector[0].NativeDataSize;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            Position = (byte*)Vector.Internal._Myfirst;

            if (Position == Vector.Internal._Mylast)
                return false;

            Position += DataSize;
            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public T Current { get; private set; }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}
