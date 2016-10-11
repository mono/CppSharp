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

    public class CLITypePrinter : ITypePrinter<string>, IDeclVisitor<string>
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

        public string VisitTagType(TagType tag, TypeQualifiers quals)
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

        public string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            // const char* and const char[] are the same so we can use a string
            if (array.SizeType == ArrayType.ArraySize.Incomplete &&
                array.Type.Desugar().IsPrimitiveType(PrimitiveType.Char) &&
                array.QualifiedType.Qualifiers.IsConst)
                return "System::String^";

            return string.Format("cli::array<{0}>^", array.Type.Visit(this));
        }

        public string VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false);

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

        public string VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames)
        {
            var args = new List<string>();

            foreach (var param in @params)
                args.Add(VisitParameter(param, hasNames));

            return string.Join(", ", args);
        }

        public string VisitParameter(Parameter param, bool hasName = true)
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

        public string VisitDelegate(FunctionType function)
        {
            return string.Format("delegate {0} {{0}}({1})",
                function.ReturnType.Visit(this),
                VisitParameters(function.Parameters, hasNames: true));
        }

        public string VisitPointerType(PointerType pointer, TypeQualifiers quals)
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
                    return pointee.Visit(this, quals);

                if (pointee.IsPrimitiveType(PrimitiveType.Void))
                    return "::System::IntPtr";

                var result = pointee.Visit(this, quals);
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

            return pointer.Pointee.Visit(this, quals);
        }

        public string VisitMemberPointerType(MemberPointerType member,
                                             TypeQualifiers quals)
        {
            return member.Pointee.Visit(this);
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
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar: return "System::Char";
                case PrimitiveType.Char: return Options.MarshalCharAsManagedChar ? "System::Char" : "char";
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

        public string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
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

        public string VisitAttributedType(AttributedType attributed, TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public string VisitDecayedType(DecayedType decayed, TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public string VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                      TypeQualifiers quals)
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

        public string VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            if (template.Desugared.Type != null)
                return template.Desugared.Visit(this);
            return string.Empty;
        }

        public string VisitTemplateParameterType(TemplateParameterType param,
            TypeQualifiers quals)
        {
            return param.Parameter.Name;
        }

        public string VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            return param.Replacement.Visit(this);
        }

        public string VisitInjectedClassNameType(InjectedClassNameType injected, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitDependentNameType(DependentNameType dependent, TypeQualifiers quals)
        {
            return dependent.Desugared.Type != null ? dependent.Desugared.Visit(this) : string.Empty;
        }

        public string VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitUnaryTransformType(UnaryTransformType unaryTransformType, TypeQualifiers quals)
        {
            if (unaryTransformType.Desugared.Type != null)
                return unaryTransformType.Desugared.Visit(this);
            return unaryTransformType.BaseType.Visit(this);
        }

        public string VisitVectorType(VectorType vectorType, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitCILType(CILType type, TypeQualifiers quals)
        {
            var result = type.Type.FullName.Replace(".", "::");
            if (!type.Type.IsValueType)
                result += "^";
            return result;
        }

        public string VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            return VisitPrimitiveType(type);
        }

        public string VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public string VisitDeclaration(Declaration decl)
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

            names.Add(decl.Visit(this));

            var result = string.Join("::", names);
            var translationUnit = decl.Namespace as TranslationUnit;
            if (translationUnit != null && translationUnit.HasFunctions &&
                rootNamespace == translationUnit.FileNameWithoutExtension)
                return "::" + result;
            return result;
        }

        public string VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            return string.Format("{0}{1}", @class.Name, @class.IsRefType ? "^"
                : string.Empty);
        }

        public string VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
        {
            return VisitClassDecl(specialization);
        }

        public string VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public string VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public string VisitParameterDecl(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public string VisitTypedefDecl(TypedefDecl typedef)
        {
            return typedef.Name;
        }

        public string VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return typeAlias.Name;
        }

        public string VisitEnumDecl(Enumeration @enum)
        {
            return @enum.Name;
        }

        public string VisitEnumItemDecl(Enumeration.Item item)
        {
            return string.Format("{0}::{1}",
                VisitEnumDecl((Enumeration) item.Namespace), VisitDeclaration(item));
        }

        public string VisitVariableDecl(Variable variable)
        {
            throw new NotImplementedException();
        }

        public string VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public string VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public string VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public string VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public string VisitProperty(Property property)
        {
            throw new NotImplementedException();
        }

        public string VisitFriend(Friend friend)
        {
            throw new NotImplementedException();
        }

        public string ToString(Type type)
        {
            return type.Visit(this);
        }

        public string VisitTemplateTemplateParameterDecl(TemplateTemplateParameter templateTemplateParameter)
        {
            return templateTemplateParameter.Name;
        }

        public string VisitTemplateParameterDecl(TypeTemplateParameter templateParameter)
        {
            return templateParameter.Name;
        }

        public string VisitNonTypeTemplateParameterDecl(NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            return nonTypeTemplateParameter.Name;
        }

        public string VisitTypeAliasTemplateDecl(TypeAliasTemplate typeAliasTemplate)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionTemplateSpecializationDecl(FunctionTemplateSpecialization specialization)
        {
            throw new NotImplementedException();
        }

        public string VisitVarTemplateDecl(VarTemplate template)
        {
            throw new NotImplementedException();
        }

        public string VisitVarTemplateSpecializationDecl(VarTemplateSpecialization template)
        {
            throw new NotImplementedException();
        }
    }
}
