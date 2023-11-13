using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Types;

namespace CppSharp
{
    /// <summary>
    /// This type checker is used to check if a type is ignored.
    /// </summary>
    public class TypeIgnoreChecker : AstVisitor
    {
        ITypeMapDatabase TypeMapDatabase { get; }
        public bool IsIgnored;

        public TypeIgnoreChecker(ITypeMapDatabase database, GeneratorKind generatorKind)
        {
            TypeMapDatabase = database;
            VisitOptions.ClearFlags(VisitFlags.ClassBases | VisitFlags.TemplateArguments);
            this.generatorKind = generatorKind;
        }

        public TypeIgnoreChecker(ITypeMapDatabase database) : this(database, GeneratorKind.CSharp)
        {
        }

        void Ignore()
        {
            IsIgnored = true;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(type, out typeMap)
                && typeMap.IsIgnored)
            {
                Ignore();
                return false;
            }

            return base.VisitType(type, quals);
        }

        public override bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            // we do not support long double yet because its high-level representation is often problematic
            if (type == PrimitiveType.LongDouble)
            {
                Ignore();
                return false;
            }
            return base.VisitPrimitiveType(type, quals);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (AlreadyVisited(decl))
                return false;

            if (decl.CompleteDeclaration != null)
                return VisitDeclaration(decl.CompleteDeclaration);

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                return typeMap.IsIgnored;
            }

            if (!(decl is TypedefDecl) && !decl.IsGenerated)
            {
                Ignore();
                return false;
            }

            return true;
        }

        public override bool VisitDependentNameType(DependentNameType dependent, TypeQualifiers quals)
        {
            Ignore();
            return false;
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
            if (TypeMapDatabase.FindTypeMap(typedef.Type, out typeMap))
            {
                if (typeMap.IsIgnored)
                    Ignore();
                return false;
            }

            return base.VisitTypedefDecl(typedef);
        }

        public override bool VisitMemberPointerType(MemberPointerType member, TypeQualifiers quals)
        {
            Ignore();
            return false;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (parameter.Type.IsPrimitiveType(PrimitiveType.Null))
            {
                Ignore();
                return false;
            }

            return base.VisitParameterDecl(parameter);
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

            return IsIgnored = !base.VisitTemplateSpecializationType(template, quals);
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(array, out typeMap) && typeMap.IsIgnored)
            {
                Ignore();
                return false;
            }

            var arrayElemType = array.Type.Desugar();
            Enumeration @enum;
            FunctionType functionType;
            if (arrayElemType is ArrayType ||
                arrayElemType is FunctionType ||
                arrayElemType.IsPointerTo(out functionType) ||
                (arrayElemType.TryGetEnum(out @enum) &&
                 array.SizeType == ArrayType.ArraySize.Constant))
            {
                Ignore();
                return false;
            }

            if (generatorKind == GeneratorKind.CSharp)
                return true;

            // the C++/CLI generator needs work to support arrays
            if (!array.QualifiedType.Visit(this))
                return false;

            if (array.SizeType != ArrayType.ArraySize.Constant)
                return true;

            Class @class;
            if (arrayElemType.TryGetClass(out @class) && @class.IsRefType)
                return true;

            PrimitiveType primitive;
            if ((arrayElemType.IsPrimitiveType(out primitive) && primitive != PrimitiveType.LongDouble) ||
                arrayElemType.IsPointerToPrimitiveType())
                return true;

            Ignore();
            return false;
        }

        public override bool VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            Ignore();
            return false;
        }

        public override bool VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            Ignore();
            return false;
        }


        private readonly GeneratorKind generatorKind;
    }
}
