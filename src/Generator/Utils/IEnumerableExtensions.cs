using System;
using System.Collections.Generic;

namespace CppSharp.Utils
{
    public static class IEnumerableExtensions
    {
        public static IList<T> TopologicalSort<T>(this ICollection<T> source, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle = false)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            foreach (var item in source)
                Visit(item, source, visited, sorted, dependencies, throwOnCycle);

            return sorted;
        }

        private static void Visit<T>(T item, ICollection<T> source, ISet<T> visited, ICollection<T> sorted, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle)
        {
            if (!visited.Contains(item))
            {
                visited.Add(item);

                foreach (var dep in dependencies(item))
                    Visit(dep, source, visited, sorted, dependencies, throwOnCycle);

                if (source.Contains(item))
                {
                    sorted.Add(item);                    
                }
            }
            else
            {
                if (throwOnCycle && !sorted.Contains(item))
                    throw new Exception("Cyclic dependency found");
            }
        }
    }
}
