using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cxxi
{
    /// <summary>
    /// Represents a C++ namespace.
    /// </summary>
    public class Namespace : Declaration
    {
        public Namespace Parent { get; set; }
        public bool IsAnonymous { get; set; }

        public List<Namespace> Namespaces;
        public List<Enumeration> Enums;
        public List<Function> Functions;
        public List<Class> Classes;
        public List<Template> Templates;
        public List<TypedefDecl> Typedefs;

        // Translation unit the declaration is contained in.
        public TranslationUnit TranslationUnit
        {
            get
            {
                if (this is TranslationUnit)
                    return this as TranslationUnit;
                else
                    return Parent.TranslationUnit;
            }
        }

        /// If the namespace should be ignored.
        public override bool Ignore
        {
            get { return ExplicityIgnored || Parent.Ignore; }
        }

        public Namespace()
            : this(null, String.Empty)
        {
        }

        public Namespace(Namespace parent, string name, bool isAnonymous = false)
        {
            Name = name;
            Parent = parent;
            IsAnonymous = isAnonymous;

            Namespaces = new List<Namespace>();
            Enums = new List<Enumeration>();
            Functions = new List<Function>();
            Classes = new List<Class>();
            Templates = new List<Template>();
            Typedefs = new List<TypedefDecl>();
        }

        public Namespace FindNamespace(string name)
        {
            return Namespaces.Find(e => e.Name.Equals(name));
        }

        public Namespace FindCreateNamespace(string name, Namespace parent)
        {
            var @namespace = FindNamespace(name);

            if (@namespace == null)
            {
                @namespace = new Namespace(parent, name);
                Namespaces.Add(@namespace);
            }

            return @namespace;
        }

        public Enumeration FindEnum(string name, bool createDecl = false)
        {
            var @enum = Enums.Find(e => e.Name.Equals(name));

            if (@enum == null && createDecl)
            {
                @enum = new Enumeration() { Name = name, Namespace = this };
                Enums.Add(@enum);
            }

            return @enum;
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

        public Class FindClass(string name)
        {
            var @class = Classes.Find(e => e.Name.Equals(name));
            return @class;
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

        public ClassTemplate FindClassTemplate(string name)
        {
            return null;
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

        public bool HasDeclarations
        {
            get
            {
                Predicate<Declaration> pred = (t => !t.Ignore);
                return Enums.Exists(pred) || HasFunctions
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

        public bool IsRoot { get { return Parent == null; } }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitNamespace(this);
        }
    }
}