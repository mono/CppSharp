using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CppSharp.AST
{
    public class DeclarationsList : ObservableCollection<Declaration>
    {
        public IEnumerable<Namespace> Namespaces => OfType<Namespace>(Kind.Namespace);

        public IEnumerable<Enumeration> Enums => OfType<Enumeration>(Kind.Enum);

        public IEnumerable<Function> Functions => OfType<Function>(Kind.Function);

        public IEnumerable<Class> Classes => OfType<Class>(Kind.Class);

        public IEnumerable<Template> Templates => OfType<Template>(Kind.Template);

        public IEnumerable<TypedefNameDecl> Typedefs => OfType<TypedefNameDecl>(Kind.Typedef);

        public IEnumerable<Variable> Variables => OfType<Variable>(Kind.Variable);

        public IEnumerable<Event> Events => OfType<Event>(Kind.Event);

        public void AddRange(IEnumerable<Declaration> declarations)
        {
            foreach (Declaration declaration in declarations)
            {
                Add(declaration);
            }
        }

        protected override void InsertItem(int index, Declaration item)
        {
            Kind kind = GetKind(item);
            int offset = GetOffset(kind);
            if (GetStart(kind) < index && index < offset)
            {
                base.InsertItem(index, item);
            }
            else
            {
                base.InsertItem(offset, item);
            }
            for (Kind i = kind; i <= Kind.Event; i++)
            {
                if (offsets.ContainsKey(i))
                {
                    offsets[i]++;
                }
            }
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            for (Kind i = Kind.Namespace; i <= Kind.Event; i++)
            {
                if (offsets.ContainsKey(i) && index < offsets[i])
                {
                    offsets[i]--;
                }
            }
        }

        private IEnumerable<T> OfType<T>(Kind kind) where T : Declaration
        {
            if (!offsets.ContainsKey(kind))
            {
                yield break;
            }
            int offset = offsets[kind];
            for (int i = GetStart(kind); i < offset; i++)
            {
                yield return (T) this[i];
            }
        }

        private static Kind GetKind(Declaration item)
        {
            if (item is Namespace)
                return Kind.Namespace;
            if (item is Enumeration)
                return Kind.Enum;
            if (item is Function)
                return Kind.Function;
            if (item is Class)
                return Kind.Class;
            if (item is Template)
                return Kind.Template;
            if (item is TypedefNameDecl)
                return Kind.Typedef;
            if (item is Variable)
                return Kind.Variable;
            if (item is Friend)
                return Kind.Friend;
            if (item is Event)
                return Kind.Event;
            throw new System.ArgumentOutOfRangeException(nameof(item),
                "Unsupported type of declaration.");
        }

        private int GetOffset(Kind kind)
        {
            if (!offsets.ContainsKey(kind))
            {
                for (Kind i = kind - 1; i >= Kind.Namespace; i--)
                {
                    if (offsets.ContainsKey(i))
                    {
                        return offsets[kind] = offsets[i];
                    }
                }
                offsets[kind] = 0;
            }
            return offsets[kind];
        }

        private int GetStart(Kind kind)
        {
            for (Kind i = kind - 1; i >= Kind.Namespace; i--)
            {
                if (offsets.ContainsKey(i))
                {
                    return offsets[i];
                }
            }
            return 0;
        }

        private Dictionary<Kind, int> offsets = new Dictionary<Kind, int>();

        private enum Kind
        {
            Namespace,
            Enum,
            Function,
            Class,
            Template,
            Typedef,
            Variable,
            Friend,
            Event
        }
    }
}
