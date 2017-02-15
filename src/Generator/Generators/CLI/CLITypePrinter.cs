using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    public class CLITypePrinterContext : TypePrinterContext
    {
        public CLITypePrinterContext()
        {
            
        }

        public CLITypePrinterContext(TypePrinterContextKind kind)
            : base(kind)
        {
        }
    }

    public class CLITypePrinter : TypePrinter
    {
        public CLITypePrinterContext TypePrinterContext { get; set; }

        public BindingContext Context { get; private set; }

        public DriverOptions Options { get { return Context.Options; } }
        public TypeMapDatabase TypeMapDatabase { get { return Context.TypeMaps; } }

        public CLITypePrinter(BindingContext context)
        {
            Context = context;
            TypePrinterContext = new CLITypePrinterContext();
        }

        public CLITypePrinter(BindingContext context, CLITypePrinterContext typePrinterContext)
            : this(context)
        {
            TypePrinterContext = typePrinterContext;
        }

        public override TypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(tag, out typeMap))
            {
                typeMap.Type = tag;
                TypePrinterContext.Type = tag;
                return typeMap.CLISignature(TypePrinterContext);
            }

            Declaration decl = tag.Declaration;

            if (decl == null)
                return string.Empty;

            return VisitDeclaration(decl, quals);
        }

        public override TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            // const char* and const char[] are the same so we can use a string
            if (array.SizeType == ArrayType.ArraySize.Incomplete &&
                array.Type.Desugar().IsPrimitiveType(PrimitiveType.Char) &&
                array.QualifiedType.Qualifiers.IsConst)
                return "System::String^";

            return string.Format("cli::array<{0}>^", array.Type.Visit(this));
        }

        public override TypePrinterResult VisitFunctionType(FunctionType function,
            TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false).ToString();

            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                if (!string.IsNullOrEmpty(args))
                    args = string.Format("<{0}>", args);
                return string.Format("System::Action{0}", args);
            }

            if (!string.IsNullOrEmpty(args))
                args = string.Format(", {0}", args);

            return string.Format("System::Func<{0}{1}>", returnType.Visit(this), args);
        }

        public override TypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames)
        {
            var args = new List<string>();

            foreach (var param in @params)
                args.Add(VisitParameter(param, hasNames).ToString());

            return string.Join(", ", args);
        }

        public override TypePrinterResult VisitParameter(Parameter param,
            bool hasName = true)
        {
            TypePrinterContext.Parameter = param;
            var type = param.Type.Visit(this, param.QualifiedType.Qualifiers);
            TypePrinterContext.Parameter = null;

            var str = string.Empty;
            if(param.Usage == ParameterUsage.Out)
                str += "[System::Runtime::InteropServices::Out] ";
            else if (param.Usage == ParameterUsage.InOut)
                str += "[System::Runtime::InteropServices::In, System::Runtime::InteropServices::Out] ";

            str += type;

            if(param.Usage == ParameterUsage.Out ||
               param.Usage == ParameterUsage.InOut)
                str += "%";

            if (hasName && !string.IsNullOrEmpty(param.Name))
                str += " " + param.Name;

            return str;
        }

        public override TypePrinterResult VisitDelegate(FunctionType function)
        {
            return string.Format("delegate {0} {{0}}({1})",
                function.ReturnType.Visit(this),
                VisitParameters(function.Parameters, hasNames: true));
        }

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee.Desugar();

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return string.Format("{0}^", function.Visit(this, quals));
            }

            if (CSharpTypePrinter.IsConstCharString(pointer))
                return "System::String^";

            // From http://msdn.microsoft.com/en-us/library/y31yhkeb.aspx
            // Any of the following types may be a pointer type:
            // * sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal, or bool.
            // * Any enum type.
            // * Any pointer type.
            // * Any user-defined struct type that contains fields of unmanaged types only.
            var finalPointee = pointer.GetFinalPointee();
            if (finalPointee.IsPrimitiveType())
            {
                // Skip one indirection if passed by reference
                var param = TypePrinterContext.Parameter;
                bool isRefParam = param != null && (param.IsOut || param.IsInOut);
                if (isRefParam)
                    return pointer.QualifiedPointee.Visit(this);

                if (pointee.IsPrimitiveType(PrimitiveType.Void))
                    return "::System::IntPtr";

                var result = pointer.QualifiedPointee.Visit(this).ToString();
                return !isRefParam && result == "::System::IntPtr" ? "void**" : result + "*";
            }

            Enumeration @enum;
            if (pointee.TryGetEnum(out @enum))
            {
                var typeName = @enum.Visit(this);

                // Skip one indirection if passed by reference
                var param = TypePrinterContext.Parameter;
                if (param != null && (param.IsOut || param.IsInOut)
                    && pointee == finalPointee)
                    return string.Format("{0}", typeName);

                return string.Format("{0}*", typeName);
            }

            return pointer.QualifiedPointee.Visit(this);
        }

        public override TypePrinterResult VisitMemberPointerType(
            MemberPointerType member, TypeQualifiers quals)
        {
            return member.QualifiedPointee.Visit(this);
        }

        public override TypePrinterResult VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
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
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar: return "System::Char";
                case PrimitiveType.Char: return Options.MarshalCharAsManagedChar ? "System::Char" : "char";
                case PrimitiveType.SChar: return "signed char";
                case PrimitiveType.UChar: return "unsigned char";
                case PrimitiveType.Short: return "short";
                case PrimitiveType.UShort: return "unsigned short";
                case PrimitiveType.Int: return "int";
                case PrimitiveType.UInt: return "unsigned int";
                case PrimitiveType.Long: return "long";
                case PrimitiveType.ULong: return "unsigned long";
                case PrimitiveType.LongLong: return "long long";
                case PrimitiveType.ULongLong: return "unsigned long long";
                case PrimitiveType.Int128: return "__int128";
                case PrimitiveType.UInt128: return "__uint128";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.LongDouble: return "long double";
                case PrimitiveType.IntPtr: return "IntPtr";
                case PrimitiveType.UIntPtr: return "UIntPtr";
                case PrimitiveType.Null: return "void*";
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Type = typedef;
                TypePrinterContext.Type = typedef;
                return typeMap.CLISignature(TypePrinterContext);
            }

            FunctionType func;
            if (decl.Type.IsPointerTo<FunctionType>(out func))
            {
                // TODO: Use SafeIdentifier()
                return string.Format("{0}^", VisitDeclaration(decl));
            }

            return decl.Type.Visit(this);
        }

        public override TypePrinterResult VisitAttributedType(AttributedType attributed,
            TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public override TypePrinterResult VisitDecayedType(DecayedType decayed,
            TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public override TypePrinterResult VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;
            if (decl == null)
                return string.Empty;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(template, out typeMap) && !typeMap.IsIgnored)
            {
                typeMap.Declaration = decl;
                typeMap.Type = template;
                TypePrinterContext.Type = template;
                return typeMap.CLISignature(TypePrinterContext);
            }

            return decl.Name;
        }

        public override TypePrinterResult VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            if (template.Desugared.Type != null)
                return template.Desugared.Visit(this);
            return string.Empty;
        }

        public override TypePrinterResult VisitTemplateParameterType(
            TemplateParameterType param, TypeQualifiers quals)
        {
            return param.Parameter.Name;
        }

        public override TypePrinterResult VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            return param.Replacement.Visit(this);
        }

        public override TypePrinterResult VisitInjectedClassNameType(
            InjectedClassNameType injected, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public override TypePrinterResult VisitDependentNameType(
            DependentNameType dependent, TypeQualifiers quals)
        {
            return dependent.Desugared.Type != null
                ? dependent.Desugared.Visit(this) : string.Empty;
        }

        public override TypePrinterResult VisitPackExpansionType(
            PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public override TypePrinterResult VisitUnaryTransformType(
            UnaryTransformType unaryTransformType, TypeQualifiers quals)
        {
            if (unaryTransformType.Desugared.Type != null)
                return unaryTransformType.Desugared.Visit(this);
            return unaryTransformType.BaseType.Visit(this);
        }

        public override TypePrinterResult VisitVectorType(VectorType vectorType,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
        {
            var result = type.Type.FullName.Replace(".", "::");
            if (!type.Type.IsValueType)
                result += "^";
            return result;
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(type);
        }

        public override TypePrinterResult VisitUnsupportedType(UnsupportedType type,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl,
            TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl)
        {
            var names = new List<string>();

            string rootNamespace = null;
            if (!string.IsNullOrEmpty(decl.TranslationUnit.Module.OutputNamespace))
                names.Add(rootNamespace = decl.TranslationUnit.Module.OutputNamespace);

            if (!string.IsNullOrEmpty(decl.Namespace.QualifiedName))
            {
                names.Add(decl.Namespace.QualifiedName);
                if (string.IsNullOrEmpty(rootNamespace))
                    rootNamespace = decl.Namespace.QualifiedName;
            }

            names.Add(decl.Visit(this).ToString());

            var result = string.Join("::", names);
            var translationUnit = decl.Namespace as TranslationUnit;
            if (translationUnit != null && translationUnit.HasFunctions &&
                rootNamespace == translationUnit.FileNameWithoutExtension)
                return "::" + result;
            return result;
        }

        public override TypePrinterResult VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            return string.Format("{0}{1}", @class.Name, @class.IsRefType ? "^"
                : string.Empty);
        }

        public override TypePrinterResult VisitClassTemplateSpecializationDecl(
            ClassTemplateSpecialization specialization)
        {
            return VisitClassDecl(specialization);
        }

        public override TypePrinterResult VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            return typedef.Name;
        }

        public override TypePrinterResult VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return typeAlias.Name;
        }

        public override TypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            return @enum.Name;
        }

        public override TypePrinterResult VisitEnumItemDecl(Enumeration.Item item)
        {
            return string.Format("{0}::{1}",
                VisitEnumDecl((Enumeration) item.Namespace), VisitDeclaration(item));
        }

        public override TypePrinterResult VisitVariableDecl(Variable variable)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitFunctionTemplateDecl(
            FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitProperty(Property property)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitFriend(Friend friend)
        {
            throw new NotImplementedException();
        }

        public override string ToString(Type type)
        {
            return type.Visit(this).ToString();
        }

        public override TypePrinterResult VisitTemplateTemplateParameterDecl(
            TemplateTemplateParameter templateTemplateParameter)
        {
            return templateTemplateParameter.Name;
        }

        public override TypePrinterResult VisitTemplateParameterDecl(
            TypeTemplateParameter templateParameter)
        {
            return templateParameter.Name;
        }

        public override TypePrinterResult VisitNonTypeTemplateParameterDecl(
            NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            return nonTypeTemplateParameter.Name;
        }

        public override TypePrinterResult VisitTypeAliasTemplateDecl(
            TypeAliasTemplate typeAliasTemplate)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitFunctionTemplateSpecializationDecl(
            FunctionTemplateSpecialization specialization)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitVarTemplateDecl(VarTemplate template)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitVarTemplateSpecializationDecl(
            VarTemplateSpecialization template)
        {
            throw new NotImplementedException();
        }
    }
}
