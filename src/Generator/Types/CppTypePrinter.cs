using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using Type = CppSharp.AST.Type;

namespace CppSharp.Types
{
    public enum CppTypePrintScopeKind
    {
        Local,
        Qualified,
        GlobalQualified
    }

    public class CppTypePrinter : ITypePrinter<string>, IDeclVisitor<string>
    {
        public CppTypePrintScopeKind PrintScopeKind;
        public bool PrintLogicalNames;
        public bool PrintTypeQualifiers;

        public CppTypePrinter(ITypeMapDatabase database, bool printTypeQualifiers = true)
        {
            PrintScopeKind = CppTypePrintScopeKind.GlobalQualified;
            PrintTypeQualifiers = printTypeQualifiers;
        }

        public string VisitTagType(TagType tag, TypeQualifiers quals)
        {
            return tag.Declaration.Visit(this);
        }

        public string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            var typeName = array.Type.Visit(this);

            switch (array.SizeType)
            {
            case ArrayType.ArraySize.Constant:
                return string.Format("{0}[{1}]", typeName, array.Size);
            case ArrayType.ArraySize.Variable:
            case ArrayType.ArraySize.Dependent:
            case ArrayType.ArraySize.Incomplete:
                return string.Format("{0}[]", typeName);
            }

            throw new NotSupportedException();
        }

        static string ConvertModifierToString(PointerType.TypeModifier modifier)
        {
            switch (modifier)
            {
                case PointerType.TypeModifier.Value: return string.Empty;
                case PointerType.TypeModifier.Pointer: return "*";
                case PointerType.TypeModifier.LVReference: return "&";
                case PointerType.TypeModifier.RVReference: return "&&";
            }

            return string.Empty;
        }

        public string VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            var function = pointee as FunctionType;
            if (function != null)
            {
                var arguments = function.Parameters;
                var returnType = function.ReturnType;
                var args = string.Empty;

                if (arguments.Count > 0)
                    args = VisitParameters(function.Parameters, hasNames: false);

                return string.Format("{0} (*)({1})", returnType.Visit(this), args);
            }

            var pointeeType = pointer.Pointee.Visit(this, quals);
            var mod = ConvertModifierToString(pointer.Modifier);

            var s = PrintTypeQualifiers && quals.IsConst ? "const " : string.Empty;
            s += string.Format("{0}{1}", pointeeType, mod);

            return s;
        }

        public string VisitMemberPointerType(MemberPointerType member, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public string VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool: return "bool";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.Char16:
                case PrimitiveType.WideChar: return "wchar_t";
                case PrimitiveType.Char: return "char";
                case PrimitiveType.UChar: return "unsigned char";
                case PrimitiveType.Short: return "short";
                case PrimitiveType.UShort: return "unsigned short";
                case PrimitiveType.Int: return "int";
                case PrimitiveType.UInt: return "unsigned int";
                case PrimitiveType.Long: return "long";
                case PrimitiveType.ULong: return "unsigned long";
                case PrimitiveType.LongLong: return "long long";
                case PrimitiveType.ULongLong: return "unsigned long long";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.IntPtr: return "void*";
                case PrimitiveType.UIntPtr: return "uintptr_t";
                case PrimitiveType.Null: return "std::nullptr_t";
            }

            throw new NotSupportedException();
        }

        public string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            return GetDeclName(typedef.Declaration);
        }

        public string VisitAttributedType(AttributedType attributed, TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public string VisitDecayedType(DecayedType decayed, TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public string VisitTemplateSpecializationType(TemplateSpecializationType template, TypeQualifiers quals)
        {
            return string.Format("{0}<{1}>", template.Template.TemplatedDecl.Visit(this),
                string.Join(", ",
                template.Arguments.Where(
                    a => a.Type.Type != null &&
                        !(a.Type.Type is DependentNameType)).Select(a => a.Type.Visit(this))));
        }

        public string VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            if (param.Parameter.Name == null)
                return string.Empty;

            return param.Parameter.Name;
        }

        public string VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            return param.Replacement.Visit(this);
        }

        public string VisitInjectedClassNameType(InjectedClassNameType injected, TypeQualifiers quals)
        {
            return injected.Class.Visit(this);
        }

        public string VisitDependentNameType(DependentNameType dependent, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitCILType(CILType type, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new System.NotImplementedException();
        }

        public string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new System.NotImplementedException();
        }

        public string VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false);

            return string.Format("{0} ({1})", returnType.Visit(this), args);
        }

        public string VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames)
        {
            var args = new List<string>();

            foreach (var param in @params)
                args.Add(VisitParameter(param, hasNames));

            return string.Join(", ", args);
        }

        public string VisitParameter(Parameter arg, bool hasName = true)
        {
            var type = arg.Type.Visit(this, arg.QualifiedType.Qualifiers);
            var name = arg.Name;

            if (hasName && !string.IsNullOrEmpty(name))
                return string.Format("{0} {1}", type, name);

            return type;
        }

        public string VisitDelegate(FunctionType function)
        {
            throw new System.NotImplementedException();
        }

        public string GetDeclName(Declaration declaration)
        {
            switch (PrintScopeKind)
            {
            case CppTypePrintScopeKind.Local:
                return PrintLogicalNames ? declaration.LogicalOriginalName
                    : declaration.OriginalName;
            case CppTypePrintScopeKind.Qualified:
                return PrintLogicalNames ? declaration.QualifiedLogicalOriginalName
                    : declaration.QualifiedOriginalName;
            case CppTypePrintScopeKind.GlobalQualified:
                return "::" + (PrintLogicalNames ? declaration.QualifiedLogicalOriginalName
                    : declaration.QualifiedOriginalName);
            }

            throw new NotSupportedException();
        }

        public string VisitDeclaration(Declaration decl)
        {
            return GetDeclName(decl);
        }

        public string VisitClassDecl(Class @class)
        {
            return VisitDeclaration(@class);
        }

        public string VisitFieldDecl(Field field)
        {
            return VisitDeclaration(field);
        }

        public string VisitFunctionDecl(Function function)
        {
            return VisitDeclaration(function);
        }

        public string VisitMethodDecl(Method method)
        {
            return VisitDeclaration(method);
        }

        public string VisitParameterDecl(Parameter parameter)
        {
            return VisitParameter(parameter, hasName: false);
        }

        public string VisitTypedefDecl(TypedefDecl typedef)
        {
            return VisitDeclaration(typedef);
        }

        public string VisitEnumDecl(Enumeration @enum)
        {
            return VisitDeclaration(@enum);
        }

        public string VisitVariableDecl(Variable variable)
        {
            return VisitDeclaration(variable);
        }

        public string VisitClassTemplateDecl(ClassTemplate template)
        {
            return VisitDeclaration(template);
        }

        public string VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return VisitDeclaration(template);
        }

        public string VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public string VisitNamespace(Namespace @namespace)
        {
            return VisitDeclaration(@namespace);
        }

        public string VisitEvent(Event @event)
        {
            return string.Empty;
        }

        public string VisitProperty(Property property)
        {
            return VisitDeclaration(property);
        }

        public string VisitFriend(Friend friend)
        {
            throw new NotImplementedException();
        }

        public string ToString(Type type)
        {
            return type.Visit(this);
        }
    }
}
