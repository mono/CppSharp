using System.Collections;
using System.Collections.Generic;

namespace CppSharp.Utils
{
    /// <summary>
    /// Works just like an HashSet but preserves insertion order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OrderedSet<T> : ICollection<T>
    {
        private readonly IDictionary<T, LinkedListNode<T>> dictionary;
        private readonly LinkedList<T> linkedList;

        public OrderedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            linkedList = new LinkedList<T>();
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return dictionary.IsReadOnly; }
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public bool Add(T item)
        {
            if (dictionary.ContainsKey(item)) return false;
            LinkedListNode<T> node = linkedList.AddLast(item);
            dictionary.Add(item, node);
            return true;
        }

        public void Clear()
        {
            linkedList.Clear();
            dictionary.Clear();
        }

        public bool Remove(T item)
        {
            LinkedListNode<T> node;
            bool found = dictionary.TryGetValue(item, out node);
            if (!found) return false;
            dictionary.Remove(item);
            linkedList.Remove(node);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return linkedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            linkedList.CopyTo(array, arrayIndex);
        }
    }
}
