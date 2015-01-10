using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public struct DeclIterator<T> : IEnumerable<T> where T : Declaration
    {
        private readonly List<Declaration> Declarations;

        public DeclIterator(List<Declaration> declarations)
        {
            Declarations = declarations;
        }

        public int Count
        {
            get { return Declarations.OfType<T>().ToArray().Length; }
        }

        public T this[int index]
        {
            get { return Declarations.OfType<T>().ToArray()[index]; }
        }

        public void Add(T declaration)
        {
            Declarations.Add(declaration);
        }

        public void AddRange(IEnumerable<T> range)
        {
            Declarations.AddRange(range);
        }

        public T Find(Func<T, bool> predicate)
        {
            return Declarations.OfType<T>().SingleOrDefault<T>(predicate);
        }

        public int FindIndex(Predicate<T> predicate)
        {
            return Declarations.OfType<T>().ToList().FindIndex(predicate);
        }

        public bool Exists(Func<T, bool> predicate)
        {
            return Declarations.OfType<T>().Any(predicate);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Declarations.OfType<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Replace(T decl, T newDecl)
        {
            Declarations[Declarations.FindIndex(d => d == decl)] = newDecl;
        }
    }
}
