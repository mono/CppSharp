using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public enum CSharpTypePrinterContextKind
    {
        Native,
        Managed,
        ManagedPointer,
        GenericDelegate,
        PrimitiveIndexer
    }

    public class CSharpTypePrinterContext : TypePrinterContext
    {
        public CSharpTypePrinterContextKind CSharpKind;
        public QualifiedType FullType;

        public CSharpTypePrinterContext()
        {
            
        }

        public CSharpTypePrinterContext(TypePrinterContextKind kind,
            CSharpTypePrinterContextKind csharpKind) : base(kind)
        {
            CSharpKind = csharpKind;
        }
    }

    public class CSharpTypePrinterResult
    {
        public string Type;
        public TypeMap TypeMap;
        public string NameSuffix;

        public static implicit operator CSharpTypePrinterResult(string type)
        {
            return new CSharpTypePrinterResult() {Type = type};
        }

        public override string ToString()
        {
            return Type;
        }
    }

    public class CSharpTypePrinter : ITypePrinter<CSharpTypePrinterResult>,
        IDeclVisitor<CSharpTypePrinterResult>
    {
        public ASTContext AstContext { get; set; }
        private readonly ITypeMapDatabase TypeMapDatabase;

        private readonly Stack<CSharpTypePrinterContextKind> contexts;

        public CSharpTypePrinterContextKind ContextKind
        {
            get { return contexts.Peek(); }
        }

        public CSharpTypePrinterContext Context;

        public CSharpTypePrinter(ITypeMapDatabase database, ASTContext context)
        {
            TypeMapDatabase = database;
            AstContext = context;

            contexts = new Stack<CSharpTypePrinterContextKind>();
            PushContext(CSharpTypePrinterContextKind.Managed);

            Context = new CSharpTypePrinterContext();
        }

        public void PushContext(CSharpTypePrinterContextKind contextKind)
        {
            contexts.Push(contextKind);
        }

        public CSharpTypePrinterContextKind PopContext()
        {
            return contexts.Pop();
        }

        public CSharpTypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (tag.Declaration == null)
                return string.Empty;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(tag.Declaration, out typeMap))
            {
                typeMap.Type = tag;
                Context.CSharpKind = ContextKind;
                Context.Type = tag;

                return new CSharpTypePrinterResult()
                {
                    Type = typeMap.CSharpSignature(Context),
                    TypeMap = typeMap
                };
            }

            return tag.Declaration.Visit(this);
        }

        public CSharpTypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            if (ContextKind == CSharpTypePrinterContextKind.Native &&
                array.SizeType == ArrayType.ArraySize.Constant)
            {
                return new CSharpTypePrinterResult()
                {
                    Type = string.Format("fixed {0}", array.Type.Visit(this, quals)),
                    NameSuffix = string.Format("[{0}]", array.Size)
                };
            }

            return string.Format("{0}[]", array.Type.Visit(this));

            // C# only supports fixed arrays in unsafe sections
            // and they are constrained to a set of built-in types.
        }

        public CSharpTypePrinterResult VisitFunctionType(FunctionType function,
            TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            PushContext(CSharpTypePrinterContextKind.GenericDelegate);

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false).Type;

            PopContext();

            if (ContextKind != CSharpTypePrinterContextKind.Managed)
                return "global::System.IntPtr";

            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                if (!string.IsNullOrEmpty(args))
                    args = string.Format("<{0}>", args);
                return string.Format("Action{0}", args);
            }

            if (!string.IsNullOrEmpty(args))
                args = string.Format(", {0}", args);

            return string.Format("Func<{0}{1}>", returnType.Visit(this), args);
        }

        public static bool IsConstCharString(PointerType pointer)
        {
            var pointee = pointer.Pointee.Desugar();

            return (pointee.IsPrimitiveType(PrimitiveType.Char) ||
                    pointee.IsPrimitiveType(PrimitiveType.WideChar)) &&
                   pointer.QualifiedPointee.Qualifiers.IsConst;
        }

        public static bool IsConstCharString(QualifiedType qualType)
        {
            var desugared = qualType.Type.Desugar();

            if (!(desugared is PointerType))
                return false;

            var pointer = desugared as PointerType;
            return IsConstCharString(pointer);
        }

        public CSharpTypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return string.Format("{0}", function.Visit(this, quals));
            }

            var isManagedContext = ContextKind == CSharpTypePrinterContextKind.Managed;

            if (IsConstCharString(pointer))
                return isManagedContext ? "string" : "global::System.IntPtr";

            PrimitiveType primitive;
            var desugared = pointee.Desugar();
            if (desugared.IsPrimitiveType(out primitive))
            {
                if (isManagedContext && Context.Parameter != null &&
                    (Context.Parameter.IsOut || Context.Parameter.IsInOut))
                    return VisitPrimitiveType(primitive, quals);

                if (ContextKind == CSharpTypePrinterContextKind.GenericDelegate)
                    return "global::System.IntPtr";
                return VisitPrimitiveType(primitive, quals) + "*";
            }

            Class @class;
            if (desugared.IsTagDecl(out @class)
                && ContextKind == CSharpTypePrinterContextKind.Native)
            {
                return "global::System.IntPtr";
            }

            return pointee.Visit(this, quals);
        }

        public CSharpTypePrinterResult VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            FunctionType functionType;
            if (member.IsPointerTo(out functionType))
            {
                return functionType.Visit(this, quals);
            }
            throw new InvalidOperationException("A function pointer not pointing to a function type.");
        }

        public CSharpTypePrinterResult VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type, quals);
        }

        public CSharpTypePrinterResult VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Type = typedef;
                Context.CSharpKind = ContextKind;
                Context.Type = typedef;

                return new CSharpTypePrinterResult()
                    {
                        Type = typeMap.CSharpSignature(Context),
                        TypeMap = typeMap
                    };
            }

            FunctionType func;
            if (decl.Type.IsPointerTo<FunctionType>(out func))
            {
                if (ContextKind == CSharpTypePrinterContextKind.Native)
                    return "global::System.IntPtr";
                // TODO: Use SafeIdentifier()
                return VisitDeclaration(decl);
            }

            return decl.Type.Visit(this);
        }

        public CSharpTypePrinterResult VisitAttributedType(AttributedType attributed, TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public CSharpTypePrinterResult VisitDecayedType(DecayedType decayed, TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public CSharpTypePrinterResult VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(template, out typeMap))
            {
                typeMap.Declaration = decl;
                typeMap.Type = template;
                Context.Type = template;
                Context.CSharpKind = ContextKind;

                return new CSharpTypePrinterResult()
                    {
                        Type = GetCSharpSignature(typeMap),
                        TypeMap = typeMap
                    };
            }

            return decl.Name;
        }

        private string GetCSharpSignature(TypeMap typeMap)
        {
            Context.CSharpKind = ContextKind;
            return typeMap.CSharpSignature(Context);
        }

        public CSharpTypePrinterResult VisitTemplateParameterType(
            TemplateParameterType param, TypeQualifiers quals)
        {
            return param.Parameter.Name;
        }

        public CSharpTypePrinterResult VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            var type = param.Replacement.Type;
            return type.Visit(this, param.Replacement.Qualifiers);
        }

        public CSharpTypePrinterResult VisitInjectedClassNameType(
            InjectedClassNameType injected, TypeQualifiers quals)
        {
            return injected.Class.Name;
        }

        public CSharpTypePrinterResult VisitDependentNameType(DependentNameType dependent,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
        {
            return type.Type.FullName;
        }

        public CSharpTypePrinterResult VisitPrimitiveType(PrimitiveType primitive,
            TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool: return "bool";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.WideChar: return "char";
                case PrimitiveType.Int8: return "sbyte";
                case PrimitiveType.UInt8: return "byte";
                case PrimitiveType.Int16: return "short";
                case PrimitiveType.UInt16: return "ushort";
                case PrimitiveType.Int32: return "int";
                case PrimitiveType.UInt32: return "uint";
                case PrimitiveType.Int64: return "long";
                case PrimitiveType.UInt64: return "ulong";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.IntPtr: return "global::System.IntPtr";
            }

            throw new NotSupportedException();
        }

        public CSharpTypePrinterResult VisitDeclaration(Declaration decl,
            TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public CSharpTypePrinterResult VisitDeclaration(Declaration decl)
        {
            return GetNestedQualifiedName(decl);
        }

        public CSharpTypePrinterResult VisitClassDecl(Class @class)
        {
            if (ContextKind == CSharpTypePrinterContextKind.Native)
                return string.Format("{0}.Internal",
                    GetNestedQualifiedName(@class.OriginalClass ?? @class));

            return GetNestedQualifiedName(@class);
        }

        public CSharpTypePrinterResult VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            var paramType = parameter.Type;

            if (parameter.Kind == ParameterKind.IndirectReturnType)
                return "global::System.IntPtr";

            Context.Parameter = parameter;
            var ret = paramType.Visit(this);
            Context.Parameter = null;

            return ret;
        }

        public CSharpTypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            return GetNestedQualifiedName(typedef);
        }

        public CSharpTypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            return GetNestedQualifiedName(@enum);
        }

        static private string GetNestedQualifiedName(Declaration decl)
        {
            var names = new List<string> { decl.Name };

            var ctx = decl.Namespace;
            while (ctx != null)
            {
                if (ctx is TranslationUnit)
                    break;

                if (!string.IsNullOrWhiteSpace(ctx.Name))
                    names.Add(ctx.Name);

                ctx = ctx.Namespace;
            }

            names.Reverse();
            return string.Join(".", names);
        }

        public CSharpTypePrinterResult VisitVariableDecl(Variable variable)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitProperty(Property property)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames)
        {
            var args = new List<string>();

            foreach (var param in @params)
            {
                Context.Parameter = param;
                args.Add(VisitParameter(param, hasNames).Type);
            }

            Context.Parameter = null;
            return string.Join(", ", args);
        }

        public CSharpTypePrinterResult VisitParameter(Parameter arg, bool hasName)
        {
            var type = arg.Type.Visit(this, arg.QualifiedType.Qualifiers);
            var name = Helpers.SafeIdentifier(arg.Name);

            if (hasName && !string.IsNullOrEmpty(name))
                return string.Format("{0} {1}", type, name);

            return type;
        }

        public CSharpTypePrinterResult VisitDelegate(FunctionType function)
        {
            return string.Format("delegate {0} {{0}}({1})",
                function.ReturnType.Visit(this),
                VisitParameters(function.Parameters, hasNames: true));
        }

        public string ToString(Type type)
        {
            return type.Visit(this).Type;
        }
    }

    public static class CSharpTypePrinterExtensions
    {
        public static CSharpTypePrinterResult CSharpType(this QualifiedType type,
            CSharpTypePrinter printer)
        {
            printer.Context.FullType = type;
            return type.Visit(printer);
        }

        public static CSharpTypePrinterResult CSharpType(this Type type,
            CSharpTypePrinter printer)
        {
            return CSharpType(new QualifiedType(type), printer);
        }

        public static CSharpTypePrinterResult CSharpType(this Declaration decl,
            CSharpTypePrinter printer)
        {
            if (decl is ITypedDecl)
            {
                var type = (decl as ITypedDecl).QualifiedType;
                printer.Context.FullType = type;
            }

            return decl.Visit(printer);
        }
    }
}