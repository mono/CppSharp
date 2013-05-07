using System;
using System.Collections.Generic;
using CppSharp.Types;

namespace CppSharp.Generators.CSharp
{
    public enum CSharpTypePrinterContextKind
    {
        Native,
        Managed,
        ManagedPointer
    }

    public class CSharpTypePrinterContext : TypePrinterContext
    {
        public CSharpTypePrinterContextKind CSharpKind;

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
        public Library Library { get; set; }
        private readonly ITypeMapDatabase TypeMapDatabase;

        private readonly Stack<CSharpTypePrinterContextKind> contexts;

        public CSharpTypePrinterContextKind ContextKind
        {
            get { return contexts.Peek(); }
        }

        public CSharpTypePrinterContext Context;

        public CSharpTypePrinter(ITypeMapDatabase database, Library library)
        {
            TypeMapDatabase = database;
            Library = library;

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

            return VisitDeclaration(tag.Declaration, quals);
        }

        public CSharpTypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
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

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false).Type;

            if (returnType.IsPrimitiveType(PrimitiveType.Void))
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

            if (pointee.IsPrimitiveType(PrimitiveType.Void) ||
                pointee.IsPrimitiveType(PrimitiveType.UInt8))
                return true;

            if (pointee.IsPrimitiveType(PrimitiveType.Char) &&
                pointer.QualifiedPointee.Qualifiers.IsConst)
                return true;

            return false;
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
                return isManagedContext ? "string" : "System.IntPtr";

            Class @class;
            if (pointee.IsTagDecl(out @class)
                && ContextKind == CSharpTypePrinterContextKind.Native)
            {
                return "System.IntPtr";
            }

            return pointee.Visit(this, quals);
        }

        public CSharpTypePrinterResult VisitMemberPointerType(
            MemberPointerType member, TypeQualifiers quals)
        {
            throw new NotImplementedException();
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
                var ctx = new CSharpTypePrinterContext
                    {
                        CSharpKind = ContextKind,
                        Type = typedef
                    };
                return new CSharpTypePrinterResult()
                    {
                        Type = typeMap.CSharpSignature(ctx),
                        TypeMap = typeMap
                    };
            }

            FunctionType func;
            if (decl.Type.IsPointerTo<FunctionType>(out func))
            {
                // TODO: Use SafeIdentifier()
                return string.Format("{0}", VisitDeclaration(decl));
            }

            return decl.Type.Visit(this);
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
                return new CSharpTypePrinterResult()
                    {
                        Type = typeMap.CSharpSignature(Context),
                        TypeMap = typeMap
                    };
            }

            return decl.Name;
        }

        public CSharpTypePrinterResult VisitTemplateParameterType(
            TemplateParameterType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public CSharpTypePrinterResult VisitInjectedClassNameType(InjectedClassNameType injected, TypeQualifiers quals)
        {
            throw new NotImplementedException();
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
                case PrimitiveType.IntPtr: return "System.IntPtr";
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
            var name = decl.Visit(this);
            return string.Format("{0}", name);
        }

        public CSharpTypePrinterResult VisitClassDecl(Class @class)
        {
            return @class.Name;
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

            Class @class;
            if (paramType.Desugar().IsTagDecl(out @class)
                && ContextKind == CSharpTypePrinterContextKind.Native)
            {
                return string.Format("{0}.Internal", @class.Name);
            }

            return paramType.Visit(this);
        }

        public CSharpTypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            return typedef.Name;
        }

        public CSharpTypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            return @enum.Name;
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

        public CSharpTypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames)
        {
            var args = new List<string>();

            foreach (var param in @params)
                args.Add(VisitParameter(param, hasNames).Type);

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
}