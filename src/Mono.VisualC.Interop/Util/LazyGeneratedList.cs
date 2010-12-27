//
// Mono.VisualC.Interop.Util.LazyGeneratedList.cs: A list whose items are generated and cached on first access
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace Mono.VisualC.Interop.Util {

	public class LazyGeneratedList<TItem> : IList<TItem> where TItem : class {

		private TItem [] cache;
		private Func<int, TItem> generator;

		private int skew;

		public LazyGeneratedList (int count, Func<int, TItem> generator)
		{
			this.cache = new TItem [count];
			this.generator = generator;
			this.skew = 0;
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
			get { return cache.Length + skew; }
		}

		public bool IsReadOnly {
			get { return true; }
		}

		public TItem this [int index] {
			get {
				int realIndex = index - skew;
				if (realIndex < 0) return null;

				if (cache [realIndex] == null)
					cache [realIndex] = generator (realIndex);

				return cache [realIndex];
			}
			set {
				throw new NotSupportedException ("This IList is read only");
			}
		}

		public void Skew (int skew)
		{
			this.skew += skew;
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

