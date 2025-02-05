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
            foreach (var declaration in declarations)
            {
                Add(declaration);
            }
        }

        protected override void InsertItem(int index, Declaration item)
        {
            var kind = GetKind(item);
            var offset = GetOffset(kind);

            // USR null means an artificial declaration, add at the end
            if (item.USR == null)
            {
                base.InsertItem(offset, item);
            }
            else
            {
                var i = BinarySearch(GetStart(kind), offset, item);
                base.InsertItem(i, item);
            }

            for (var i = kind; i <= Kind.Event; i++)
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
            for (var i = Kind.Namespace; i <= Kind.Event; i++)
            {
                if (!offsets.TryGetValue(i, out int start))
                    continue;
                
                if (index < start)
                {
                    offsets[i]--;
                }
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            offsets.Clear();
        }

        private IEnumerable<T> OfType<T>(Kind kind) where T : Declaration
        {
            if (!offsets.TryGetValue(kind, out var offset))
            {
                yield break;
            }

            for (var i = GetStart(kind); i < offset; i++)
            {
                yield return (T)this[i];
            }
        }

        private static Kind GetKind(Declaration item)
        {
            return item switch
            {
                Namespace _ => Kind.Namespace,
                Enumeration _ => Kind.Enum,
                Function _ => Kind.Function,
                Class _ => Kind.Class,
                Template _ => Kind.Template,
                TypedefNameDecl _ => Kind.Typedef,
                Variable _ => Kind.Variable,
                Friend _ => Kind.Friend,
                Event _ => Kind.Event,
                _ => throw new System.ArgumentOutOfRangeException(nameof(item), "Unsupported type of declaration.")
            };
        }

        private int GetOffset(Kind kind)
        {
            if (offsets.TryGetValue(kind, out int o))
                return o;

            // First time we see this kind of declaration. Insert it between the previous and next kinds.
            for (var i = kind - 1; i >= Kind.Namespace; i--)
            {
                if (!offsets.TryGetValue(i, out var start)) 
                    continue;

                offsets[kind] = start;
                return start;
            }

            offsets[kind] = 0;
            return 0;
        }

        private int GetStart(Kind kind)
        {
            for (var i = kind - 1; i >= Kind.Namespace; i--)
            {
                if (offsets.TryGetValue(i, out var start))
                {
                    return start;
                }
            }
            return 0;
        }

        private int BinarySearch(int start, int end, Declaration item)
        {
            int middle = end;

            while (start < end)
            {
                middle = (start + end) / 2;

                if (item.DefinitionOrder < this[middle].DefinitionOrder &&
                    (middle == 0 ||
                     item.DefinitionOrder >= this[middle - 1].DefinitionOrder))
                    break;

                if (item.DefinitionOrder < this[middle].DefinitionOrder)
                    end = middle;
                else
                    start = ++middle;
            }

            return middle;
        }

        private readonly Dictionary<Kind, int> offsets = new();

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
