using CppSharp.AST;
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

            if (decl.Ignore)
            {
                Ignore();
                return false;
            }

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            return VisitDeclaration(@class);
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
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
            if (TypeMapDatabase.FindTypeMap(typedef, out typeMap))
            {
                if (typeMap.IsIgnored)
                    Ignore();
                return false;
            }

            return base.VisitTypedefDecl(typedef);
        }

        public override bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            FunctionType functionType;
            if (!member.IsPointerTo(out functionType))
            {
                Ignore();
                return false;
            }
            return base.VisitMemberPointerType(member, quals);
        }

        public override bool VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(template, out typeMap))
            {
                if (typeMap.IsIgnored)
                    Ignore();
                return false;
            }

            Ignore();
            return base.VisitTemplateSpecializationType(template, quals);
        }
    }

}
