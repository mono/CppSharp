using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CppSharp.Generators.Registrable
{
    public class InfoMapStack<T> : InfoMap<Stack<T>>
    {
        public InfoMapStack() : base()
        {
        }

        public T1 Peek<T1>(InfoEntry infoEntry, T1 defaultValue = default) where T1 : T
        {
            if (TryGetValue(infoEntry, out Stack<T> stack))
            {
                return (T1)stack.Peek();
            }
            return defaultValue;
        }

        public T1 Pop<T1>(InfoEntry infoEntry) where T1 : T
        {
            if (TryGetValue(infoEntry, out Stack<T> stack))
            {
                return (T1)stack.Pop();
            }
            throw new InvalidOperationException();
        }

        public void Push<T1>(InfoEntry infoEntry, T1 item) where T1 : T
        {
            if (!TryGetValue(infoEntry, out Stack<T> stack))
            {
                this[infoEntry] = stack = new Stack<T>();
            }
            stack.Push(item);
        }

        public bool TryPeek<T1>(InfoEntry infoEntry, [MaybeNullWhen(false)] out T1 result) where T1 : T
        {
            if (TryGetValue(infoEntry, out Stack<T> stack))
            {
                bool tempReturn = stack.TryPop(out T tempResult);
                result = (T1)tempResult;
                return tempReturn;
            }
            result = default;
            return false;
        }

        public bool TryPop<T1>(InfoEntry infoEntry, [MaybeNullWhen(false)] out T1 result) where T1 : T
        {
            if (TryGetValue(infoEntry, out Stack<T> stack))
            {
                bool tempReturn = stack.TryPop(out T tempResult);
                result = (T1)tempResult;
                return tempReturn;
            }
            result = default;
            return false;
        }

        public void Scoped<T1>(InfoEntry infoEntry, T1 item, Action action) where T1 : T
        {
            Push(infoEntry, item);
            action();
            Pop<T>(infoEntry);
        }
    }
}
