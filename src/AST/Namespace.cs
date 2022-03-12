using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a declaration context.
    /// </summary>
    public abstract class DeclarationContext : Declaration
    {
        public bool IsAnonymous { get; set; }

        public DeclarationsList Declarations;
        public List<TypeReference> TypeReferences;

        public IEnumerable<Namespace> Namespaces => Declarations.Namespaces;

        public IEnumerable<Enumeration> Enums => Declarations.Enums;

        public IEnumerable<Function> Functions => Declarations.Functions;

        public IEnumerable<Class> Classes => Declarations.Classes;

        public IEnumerable<Template> Templates => Declarations.Templates;

        public IEnumerable<TypedefNameDecl> Typedefs => Declarations.Typedefs;

        public IEnumerable<Variable> Variables => Declarations.Variables;

        public IEnumerable<Event> Events => Declarations.Events;

        // Used to keep track of anonymous declarations.
        public Dictionary<ulong, Declaration> Anonymous;

        // True if the context is inside an extern "C" context.
        public bool IsExternCContext;

        public override string LogicalName
        {
            get { return IsAnonymous ? "<anonymous>" : base.Name; }
        }

        public override string LogicalOriginalName
        {
            get { return IsAnonymous ? "<anonymous>" : base.OriginalName; }
        }

        protected DeclarationContext()
        {
            Declarations = new DeclarationsList();
            TypeReferences = new List<TypeReference>();
            Anonymous = new Dictionary<ulong, Declaration>();
        }

        protected DeclarationContext(DeclarationContext dc)
            : base(dc)
        {
            Declarations = dc.Declarations;
            TypeReferences = new List<TypeReference>(dc.TypeReferences);
            Anonymous = new Dictionary<ulong, Declaration>(dc.Anonymous);
            IsAnonymous = dc.IsAnonymous;
        }

        public IEnumerable<DeclarationContext> GatherParentNamespaces()
        {
            var children = new Stack<DeclarationContext>();
            var currentNamespace = this;

            while (currentNamespace != null)
            {
                if (!(currentNamespace is TranslationUnit))
                    children.Push(currentNamespace);

                currentNamespace = currentNamespace.Namespace;
            }

            return children;
        }

        public Declaration FindAnonymous(ulong key)
        {
            return Anonymous.ContainsKey(key) ? Anonymous[key] : null;
        }

        public Namespace FindNamespace(string name)
        {
            var namespaces = name.Split(new string[] { "::" },
                StringSplitOptions.RemoveEmptyEntries);

            return FindNamespace(namespaces);
        }

        public Namespace FindNamespace(IEnumerable<string> namespaces)
        {
            DeclarationContext currentNamespace = this;

            foreach (var @namespace in namespaces)
            {
                var childNamespace = currentNamespace.Namespaces.FirstOrDefault(
                    e => e.Name.Equals(@namespace));

                if (childNamespace == null)
                    return null;

                currentNamespace = childNamespace;
            }

            return currentNamespace as Namespace;
        }

        public Namespace FindCreateNamespace(string name)
        {
            var @namespace = FindNamespace(name);

            if (@namespace == null)
            {
                @namespace = new Namespace
                {
                    Name = name,
                    Namespace = this,
                };

                Declarations.Add(@namespace);
            }

            return @namespace;
        }

        public Enumeration FindEnum(string name, bool createDecl = false)
        {
            var entries = name.Split(new string[] { "::" },
                StringSplitOptions.RemoveEmptyEntries).ToList();

            if (entries.Count <= 1)
            {
                var @enum = Enums.FirstOrDefault(e => e.Name.Equals(name));

                if (@enum == null && createDecl)
                {
                    @enum = new Enumeration() { Name = name, Namespace = this };
                    Declarations.Add(@enum);
                }

                return @enum;
            }

            var enumName = entries[entries.Count - 1];
            var namespaces = entries.Take(entries.Count - 1);

            var @namespace = FindNamespace(namespaces);
            if (@namespace == null)
                return null;

            return @namespace.FindEnum(enumName, createDecl);
        }

        public Enumeration FindEnum(IntPtr ptr)
        {
            return Enums.FirstOrDefault(f => f.OriginalPtr == ptr);
        }

        public IEnumerable<Function> FindFunction(string name, bool createDecl = false)
        {
            if (string.IsNullOrEmpty(name))
                return Enumerable.Empty<Function>();

            var entries = name.Split(new string[] { "::" },
                StringSplitOptions.RemoveEmptyEntries).ToList();

            if (entries.Count <= 1)
            {
                var functions = Functions.Where(e => e.Name.Equals(name));

                if (!functions.Any() && createDecl)
                {
                    var function = new Function() { Name = name, Namespace = this };
                    Declarations.Add(function);
                }

                return functions;
            }

            var funcName = entries[^1];
            var namespaces = entries.Take(entries.Count - 1);

            var @namespace = FindNamespace(namespaces);
            if (@namespace == null)
                return Enumerable.Empty<Function>();

            return @namespace.FindFunction(funcName, createDecl);
        }

        public Function FindFunctionByUSR(string usr)
        {
            return Functions
                .Concat(Templates.OfType<FunctionTemplate>()
                    .Select(t => t.TemplatedFunction))
                .FirstOrDefault(f => f.USR == usr);
        }

        Class CreateClass(string name, bool isComplete)
        {
            return new Class
            {
                Name = name,
                Namespace = this,
                IsIncomplete = !isComplete
            };
        }

        public Class FindClass(string name,
            StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var @class = Classes.FirstOrDefault(c => c.Name.Equals(name, stringComparison)) ??
                Namespaces.Select(n => n.FindClass(name, stringComparison)).FirstOrDefault(c => c != null);
            if (@class != null)
                return @class.CompleteDeclaration == null ?
                    @class : (Class)@class.CompleteDeclaration;
            return null;
        }

        public Class FindClass(string name, bool isComplete,
            bool createDecl = false)
        {
            var @class = FindClass(name);

            if (@class == null)
            {
                if (createDecl)
                {
                    @class = CreateClass(name, isComplete);
                    Declarations.Add(@class);
                }

                return @class;
            }

            if (@class.IsIncomplete == !isComplete)
                return @class;

            if (!createDecl)
                return null;

            var newClass = CreateClass(name, isComplete);

            // Replace the incomplete declaration with the complete one.
            if (@class.IsIncomplete)
            {
                @class.CompleteDeclaration = newClass;
                Declarations[Declarations.IndexOf(@class)] = newClass;
            }

            return newClass;
        }

        public FunctionTemplate FindFunctionTemplate(string name)
        {
            return Templates.OfType<FunctionTemplate>()
                .FirstOrDefault(t => t.Name == name);
        }

        public FunctionTemplate FindFunctionTemplateByUSR(string usr)
        {
            return Templates.OfType<FunctionTemplate>()
                .FirstOrDefault(t => t.USR == usr);
        }

        public IEnumerable<ClassTemplate> FindClassTemplate(string name)
        {
            foreach (var template in Templates.OfType<ClassTemplate>().Where(t => t.Name == name))
                yield return template;
            foreach (var @namespace in Namespaces)
                foreach (var template in @namespace.FindClassTemplate(name))
                    yield return template;
        }

        public ClassTemplate FindClassTemplateByUSR(string usr)
        {
            return Templates.OfType<ClassTemplate>()
                .FirstOrDefault(t => t.USR == usr);
        }

        public TypedefNameDecl FindTypedef(string name, bool createDecl = false)
        {
            var entries = name.Split(new string[] { "::" },
                StringSplitOptions.RemoveEmptyEntries).ToList();

            if (entries.Count <= 1)
            {
                var typeDef = Typedefs.FirstOrDefault(e => e.Name.Equals(name));

                if (typeDef == null && createDecl)
                {
                    typeDef = new TypedefDecl { Name = name, Namespace = this };
                    Declarations.Add(typeDef);
                }

                return typeDef;
            }

            var typeDefName = entries[entries.Count - 1];
            var namespaces = entries.Take(entries.Count - 1);

            var @namespace = FindNamespace(namespaces);
            if (@namespace == null)
                return null;

            return @namespace.FindTypedef(typeDefName, createDecl);
        }

        public T FindType<T>(string name) where T : Declaration
        {
            var type = FindEnum(name)
                ?? FindFunction(name).FirstOrDefault()
                ?? (Declaration)FindClass(name)
                ?? FindTypedef(name);

            return type as T;
        }

        public Enumeration FindEnumWithItem(string name)
        {
            return Enums.FirstOrDefault(e => e.ItemsByName.ContainsKey(name)) ??
                (from declContext in Namespaces.Union<DeclarationContext>(Classes)
                 let @enum = declContext.FindEnumWithItem(name)
                 where @enum != null
                 select @enum).FirstOrDefault();
        }

        public virtual IEnumerable<Function> FindOperator(CXXOperatorKind kind)
        {
            return Functions.Where(fn => fn.OperatorKind == kind);
        }

        public virtual IEnumerable<Function> GetOverloads(Function function)
        {
            if (function.IsOperator)
                return FindOperator(function.OperatorKind);

            return Functions.Where(fn => fn.Name == function.Name);
        }

        public bool HasDeclarations
        {
            get
            {
                Func<Declaration, bool> pred = (t => t.IsGenerated);
                return Enums.Any(pred) || HasFunctions || Typedefs.Any(pred)
                    || Classes.Any() || Namespaces.Any(n => n.HasDeclarations) ||
                    Templates.Any(pred);
            }
        }

        public bool HasFunctions
        {
            get
            {
                Func<Declaration, bool> pred = (t => t.IsGenerated);
                return Functions.Any(pred) || Namespaces.Any(n => n.HasFunctions);
            }
        }

        public bool IsRoot { get { return Namespace == null; } }
    }

    /// <summary>
    /// Represents a C++ namespace.
    /// </summary>
    public class Namespace : DeclarationContext
    {
        public override string LogicalName
        {
            get { return IsInline ? string.Empty : base.Name; }
        }

        public override string LogicalOriginalName
        {
            get { return IsInline ? string.Empty : base.OriginalName; }
        }

        public bool IsInline;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitNamespace(this);
        }
    }
}
