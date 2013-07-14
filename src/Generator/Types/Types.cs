using System.Collections.Generic;
using System.Linq;
using CppSharp.Types;

namespace CppSharp
{
    /// <summary>
    /// This type checker is used to check if a type is complete.
    /// </summary>
    public class TypeCompletionChecker : AstVisitor
    {
        public TypeCompletionChecker()
        {
            Options.VisitClassBases = false;
            Options.VisitTemplateArguments = false;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (decl.CompleteDeclaration != null)
                return true;

            return !decl.IsIncomplete;
        }
    }

    /// <summary>
    /// This type checker is used to check if a type is ignored.
    /// </summary>
    public class TypeIgnoreChecker : AstVisitor
    {
        ITypeMapDatabase TypeMapDatabase { get; set; }
        public bool IsIgnored;

        public TypeIgnoreChecker(ITypeMapDatabase database)
        {
            TypeMapDatabase = database;
            Options.VisitClassBases = false;
            Options.VisitTemplateArguments = false;
        }

        void Ignore()
        {
            IsIgnored = true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (decl.CompleteDeclaration != null)
                return VisitDeclaration(decl.CompleteDeclaration);

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                if (typeMap.IsIgnored)
                    Ignore();
                return false;
            }

            if (decl.Ignore)
            {
                Ignore();
                return false;
            }

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            VisitDeclaration(@class);
            return false;
        }

        public override bool VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(typedef, out typeMap)
                && typeMap.IsIgnored)
            {
                Ignore();
                return false;
            }

            return base.VisitTypedefType(typedef, quals);
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(typedef, out typeMap)
                && typeMap.IsIgnored)
            {
                Ignore();
                return false;
            }

            return base.VisitTypedefDecl(typedef);
        }

        public override bool VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                if (typeMap.IsIgnored)
                    Ignore();
                return false;
            }

            return base.VisitTemplateSpecializationType(template, quals);
        }
    }

    public struct TypeReference
    {
        public Declaration Declaration;
        public Namespace Namespace;
    }

    /// <summary>
    /// This is used to get the declarations that each file needs to forward
    /// reference or include from other header files. Since in C++ everything
    /// that is referenced needs to have been declared previously, it can happen
    /// that a file needs to be reference something that has not been declared
    /// yet. In that case, we need to declare it before referencing it.
    /// </summary>
    public class TypeRefsVisitor : AstVisitor
    {
        public ISet<TypeReference> References;
        public ISet<Class> Bases;
        private TranslationUnit unit;
        private Namespace currentNamespace;

        public TypeRefsVisitor()
        {
            References = new HashSet<TypeReference>();
            Bases = new HashSet<Class>();
        }

        public void Collect(Declaration declaration)
        {
            var @namespace = declaration.Namespace;

            if (@namespace != null)
                if (@namespace.TranslationUnit.IsSystemHeader)
                    return;

            References.Add(new TypeReference()
                {
                    Declaration = declaration,
                    Namespace = currentNamespace
                });
        }

        public bool VisitTranslationUnit(TranslationUnit unit)
        {
            this.unit = unit;
            unit.TypeReferences = this;

            VisitNamespace(unit);

            foreach (var @namespace in unit.Namespaces)
                VisitNamespace(@namespace);

            return true;
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            currentNamespace = @namespace;
            return base.VisitNamespace(@namespace);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.Ignore)
            {
                Visited.Add(@class);
                return false;
            }

            if (Visited.Contains(@class))
                return References.Any(reference => reference.Declaration == @class);

            Collect(@class);

            // If the class is incomplete, then we cannot process the record
            // members, else it will add references to declarations that
            // should have not been found.
            if (@class.IsIncomplete)
                goto OutVisited;

            if (string.IsNullOrWhiteSpace(@class.Name))
                goto OutVisited;

            var unitClass = unit.FindClass(@class.Name);
            if (unitClass == null || unitClass.IsIncomplete)
                goto OutVisited;

            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass)
                    continue;

                Bases.Add(@base.Class);
            }

            return base.VisitClassDecl(@class);

        OutVisited:

            Visited.Add(@class);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Collect(@enum);
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (base.VisitFieldDecl(field))
                Collect(field);

            return true;
        }

        public override bool VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            if (decl.Type == null)
                return false;

            FunctionType function;
            if (decl.Type.IsPointerTo<FunctionType>(out function))
            {
                Collect(decl);
                return true;
            }

            return decl.Type.Visit(this);
        }
    }
}
