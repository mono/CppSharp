using System;
using System.Collections.Generic;
using CppSharp.AST;
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
        public Driver Driver { get; set; }
        public CLITypePrinterContext Context { get; set; }

        readonly ITypeMapDatabase TypeMapDatabase;
        readonly DriverOptions Options;

        public CLITypePrinter(Driver driver)
        {
            Driver = driver;
            TypeMapDatabase = driver.TypeDatabase;
            Options = driver.Options;
            Context = new CLITypePrinterContext();
        }

        public CLITypePrinter(Driver driver, CLITypePrinterContext context)
            : this(driver)
        {
            Context = context;
        }

        public string VisitTagType(TagType tag, TypeQualifiers quals)
        {
            Declaration decl = tag.Declaration;

            if (decl == null)
                return string.Empty;

            return VisitDeclaration(decl, quals);
        }

        public string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
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
            Context.Parameter = param;
            var type = param.Type.Visit(this, param.QualifiedType.Qualifiers);
            Context.Parameter = null;

            var str = string.Empty;
            if(param.Usage == ParameterUsage.Out)
                str += "[System::Runtime::InteropServices::Out] ";

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
            var pointee = pointer.Pointee;

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return string.Format("{0}^", function.Visit(this, quals));
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Char) && quals.IsConst)
            {
                return "System::String^";
            }

            PrimitiveType primitive;
            if (pointee.Desugar().IsPrimitiveType(out primitive))
            {
                var param = Context.Parameter;
                if (param != null && (param.IsOut || param.IsInOut))
                    return VisitPrimitiveType(primitive);

                return "System::IntPtr";
            }

            return pointee.Visit(this, quals);
        }

        public string VisitMemberPointerType(MemberPointerType member,
                                             TypeQualifiers quals)
        {
            throw new NotImplementedException();
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
                case PrimitiveType.WideChar: return "char";
                case PrimitiveType.Int8: return "char";
                case PrimitiveType.UInt8: return "unsigned char";
                case PrimitiveType.Int16: return "short";
                case PrimitiveType.UInt16: return "unsigned short";
                case PrimitiveType.Int32: return "int";
                case PrimitiveType.UInt32: return "unsigned int";
                case PrimitiveType.Int64: return "long long";
                case PrimitiveType.UInt64: return "unsigned long long";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.IntPtr: return "IntPtr";
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
                Context.Type = typedef;
                return typeMap.CLISignature(Context);
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

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(template, out typeMap))
            {
                typeMap.Declaration = decl;
                typeMap.Type = template;
                Context.Type = template;
                return typeMap.CLISignature(Context);
            }

            return decl.Name;
        }

        public string VisitTemplateParameterType(TemplateParameterType param,
            TypeQualifiers quals)
        {
            return param.Parameter.Name;
        }

        public string VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitInjectedClassNameType(InjectedClassNameType injected, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitDependentNameType(DependentNameType dependent, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitCILType(CILType type, TypeQualifiers quals)
        {
            return type.Type.FullName.Replace(".", "::") + "^";
        }

        public string VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            return VisitPrimitiveType(type);
        }

        public string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public string VisitDeclaration(Declaration decl)
        {
            var names = new List<string>();

            if (Options.GenerateLibraryNamespace)
                names.Add(Driver.Options.OutputNamespace);

            if (!string.IsNullOrEmpty(decl.Namespace.QualifiedName))
                names.Add(decl.Namespace.QualifiedName);

            names.Add(decl.Visit(this));

            return string.Join("::", names);
        }

        public string VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            return string.Format("{0}{1}", @class.Name, @class.IsRefType ? "^"
                : string.Empty);
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

        public string VisitEnumDecl(Enumeration @enum)
        {
            return @enum.Name;
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

        public string ToString(Type type)
        {
            return type.Visit(this);
        }
    }
}
