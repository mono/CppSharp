//
// Mono.VisualC.Interop.Util.LazyGeneratedList.cs: A list whose items are generated and cached on first access
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010 Alexander Corrado
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
using System.Collections;
using System.Collections.Generic;

namespace Mono.VisualC.Interop.Util {

	public class LazyGeneratedList<TItem> : IList<TItem> where TItem : class {

		private TItem [] cache;
		private Func<int, TItem> generator;

		private LazyGeneratedList<TItem> previous;
		private LazyGeneratedList<TItem> next;
		private int lead, follow;

		public LazyGeneratedList (int count, Func<int, TItem> generator)
		{
			this.cache = new TItem [count];
			this.generator = generator;
			this.lead = 0;
			this.follow = 0;
		}

		public IEnumerator<TItem> GetEnumerator ()
		{
			for (int i = 0; i < Count; i++)
				yield return this [i];
		}
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return this.GetEnumerator ();
		}

		public int Count {
			get { return lead + cache.Length + follow; }
		}

		public bool IsReadOnly {
			get { return true; }
		}

		public TItem this [int index] {
			get {
				if (index < lead) {
					if (previous != null && index > 0)
						return previous [index];
					throw new IndexOutOfRangeException (index.ToString ());
				}

				int realIndex = index - lead;
				if (realIndex > cache.Length) {
					int followIndex = realIndex - cache.Length;
					if (next != null && followIndex < follow)
						return next [followIndex];
					throw new IndexOutOfRangeException (index.ToString ());
				}

				if (cache [realIndex] == null)
					cache [realIndex] = generator (realIndex);

				return cache [realIndex];
			}
			set {
				throw new NotSupportedException ("This IList is read only");
			}
		}

		/* Visual aid for the behavior of the following methods:

			|<Prepended Range><Generated Range><Appended Range>|
                         ^               ^                 ^              ^
                         PrependFirst    PrependLast       AppendFirst    AppendLast
		*/

		public void AppendFirst (LazyGeneratedList<TItem> list)
		{
			follow += list.Count;
			if (next != null)
				list.AppendLast (next);

			next = list;
		}

		public void AppendLast (LazyGeneratedList<TItem> list)
		{
			if (next == null)
				next = list;
			else
				next.AppendLast (list);

			follow += list.Count;
		}

		public void PrependFirst (LazyGeneratedList<TItem> list)
		{
			if (previous == null)
				previous = list;
			else
				previous.PrependFirst (list);

			lead += list.Count;
		}

		public void PrependLast (LazyGeneratedList<TItem> list)
		{
			lead += list.Count;
			if (previous != null)
				list.PrependFirst (previous);

			previous = list;
		}

		// FIXME: Should probably implement these 3 at some point
		public bool Contains (TItem item)
		{
			throw new NotImplementedException ();
		}
		public void CopyTo (TItem[] array, int arrayIndex)
		{
			throw new NotImplementedException ();
		}
		public int IndexOf (TItem item)
		{
			throw new NotImplementedException ();
		}


		public void Insert (int index, TItem item)
		{
			throw new NotImplementedException ();
		}
		public void RemoveAt (int index)
		{
			throw new NotImplementedException ();
		}
		public void Add (TItem item)
		{
			throw new NotImplementedException ();
		}
		public void Clear ()
		{
			throw new NotImplementedException ();
		}
		public bool Remove (TItem item)
		{
			throw new NotImplementedException ();
		}
	}
}

