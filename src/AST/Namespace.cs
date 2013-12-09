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

        public List<Namespace> Namespaces;
        public List<Enumeration> Enums;
        public List<Function> Functions;
        public List<Class> Classes;
        public List<Template> Templates;
        public List<TypedefDecl> Typedefs;
        public List<Variable> Variables;
        public List<Event> Events;
        public List<TypeReference> TypeReferences;

        // Used to keep track of anonymous declarations.
        public Dictionary<ulong, Declaration> Anonymous; 

        public TranslationUnit TranslationUnit
        {
            get
            {
                if (this is TranslationUnit)
                    return this as TranslationUnit;
                else
                    return Namespace.TranslationUnit;
            }
        }

        protected DeclarationContext()
        {
            Namespaces = new List<Namespace>();
            Enums = new List<Enumeration>();
            Functions = new List<Function>();
            Classes = new List<Class>();
            Templates = new List<Template>();
            Typedefs = new List<TypedefDecl>();
            Variables = new List<Variable>();
            Events = new List<Event>();
            TypeReferences = new List<TypeReference>();
            Anonymous = new Dictionary<ulong, Declaration>();
        }

        public IEnumerable<DeclarationContext> GatherParentNamespaces()
        {
            var children = new List<DeclarationContext>();
            var currentNamespace = this;

            while (currentNamespace != null)
            {
                if (!(currentNamespace is TranslationUnit))
                    children.Add(currentNamespace);

                currentNamespace = currentNamespace.Namespace;
            }

            children.Reverse();
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
                var childNamespace = currentNamespace.Namespaces.Find(
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

                Namespaces.Add(@namespace);
            }

            return @namespace;
        }

        public Enumeration FindEnum(string name, bool createDecl = false)
        {
            var entries = name.Split(new string[] { "::" },
                StringSplitOptions.RemoveEmptyEntries).ToList();

            if (entries.Count <= 1)
            {
                var @enum = Enums.Find(e => e.Name.Equals(name));

                if (@enum == null && createDecl)
                {
                    @enum = new Enumeration() { Name = name, Namespace = this };
                    Enums.Add(@enum);
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

        public Function FindFunction(string name, bool createDecl = false)
        {
            var function =  Functions.Find(e => e.Name.Equals(name));

            if (function == null && createDecl)
            {
                function = new Function() { Name = name, Namespace = this };
                Functions.Add(function);
            }

            return function;
        }

        Class CreateClass(string name, bool isComplete)
        {
            var  @class = new Class
            {
                Name = name,
                Namespace = this,
                IsIncomplete = !isComplete
            };

            return @class;
        }

        public Class FindClass(string name,
            StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var entries = name.Split(new[] { "::" },
                StringSplitOptions.RemoveEmptyEntries).ToList();

            if (entries.Count <= 1)
            {
                return Classes.Find(e => e.Name.Equals(name, stringComparison));
            }

            var className = entries[entries.Count - 1];
            var namespaces = entries.Take(entries.Count - 1);

            DeclarationContext declContext = FindNamespace(namespaces);
            if (declContext == null)
            {
                declContext = FindClass(entries[0]);
                if (declContext == null)
                    return null;
            }

            return declContext.FindClass(className);
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
                    Classes.Add(@class);
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
                var index = Classes.FindIndex(c => c == @class);
                @class.CompleteDeclaration = newClass;
                Classes[index] = newClass;
            }

            return newClass;
        }

        public FunctionTemplate FindFunctionTemplate(IntPtr ptr)
        {
            return Templates.FirstOrDefault(template =>
                template.OriginalPtr == ptr) as FunctionTemplate;
        }

        public ClassTemplate FindClassTemplate(IntPtr ptr)
        {
            return Templates.FirstOrDefault(template =>
                template.OriginalPtr == ptr) as ClassTemplate;
        }

        public TypedefDecl FindTypedef(string name, bool createDecl = false)
        {
            var typedef = Typedefs.Find(e => e.Name.Equals(name));
            
            if (typedef == null && createDecl)
            {
                typedef = new TypedefDecl { Name = name, Namespace = this };
                Typedefs.Add(typedef);
            }

            return typedef;
        }

        public T FindType<T>(string name) where T : Declaration
        {
            var type = FindEnum(name)
                ?? FindFunction(name)
                ?? (Declaration)FindClass(name)
                ?? FindTypedef(name);

            return type as T;
        }

        public Enumeration FindEnumWithItem(string name)
        {
            return Enums.Find(e => e.ItemsByName.ContainsKey(name));
        }

        public virtual IEnumerable<Function> GetFunctionOverloads(Function function)
        {
            if (function.OperatorKind == CXXOperatorKind.Conversion)
                return Functions.Where(fn => fn.OperatorKind == CXXOperatorKind.Conversion);
            return Functions.Where(fn => fn.Name == function.Name);
        }

        public bool HasDeclarations
        {
            get
            {
                Predicate<Declaration> pred = (t => !t.Ignore);
                return Enums.Exists(pred) || HasFunctions || Typedefs.Exists(pred)
                    || Classes.Exists(pred) || Namespaces.Exists(n => n.HasDeclarations);
            }
        }

        public bool HasFunctions
        {
            get
            {
                Predicate<Declaration> pred = (t => !t.Ignore);
                return Functions.Exists(pred) || Namespaces.Exists(n => n.HasFunctions);
            }
        }

        public bool IsRoot { get { return Namespace == null; } }
    }

    /// <summary>
    /// Represents a C++ namespace.
    /// </summary>
    public class Namespace : DeclarationContext
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitNamespace(this);
        }
    }
}