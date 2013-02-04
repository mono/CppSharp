using System;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public class CLITypePrinter : ITypePrinter, IDeclVisitor<string>
    {
        public Library Library { get; set; }
        private readonly ITypeMapDatabase TypeMapDatabase;

        public CLITypePrinter(ITypeMapDatabase database, Library library)
        {
            TypeMapDatabase = database;
            Library = library;
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
            var arguments = function.Arguments;
            var returnType = function.ReturnType;

            string args = string.Empty;

            if (arguments.Count > 0)
                args = GetArgumentsString(function, hasNames: false);

            if (returnType.IsPrimitiveType(PrimitiveType.Void))
            {
                if (!string.IsNullOrEmpty(args))
                    args = string.Format("<{0}>", args);
                return string.Format("System::Action{0}", args);
            }

            if (!string.IsNullOrEmpty(args))
                args = string.Format(", {0}", args);

            return string.Format("System::Func<{0}{1}>", returnType.Visit(this), args);
        }

        public string GetArgumentsString(FunctionType function, bool hasNames)
        {
            var arguments = function.Arguments;
            var s = string.Empty;

            for (var i = 0; i < arguments.Count; ++i)
            {
                s += GetArgumentString(arguments[i], hasNames);
                if (i < arguments.Count - 1)
                    s += ", ";
            }

            return s;
        }

        public string GetArgumentString(Parameter arg, bool hasName = true)
        {
            var quals = new TypeQualifiers { IsConst = arg.IsConst };

            var type = arg.Type.Visit(this, quals);
            var name = arg.Name;

            if (hasName && !string.IsNullOrEmpty(name))
                return string.Format("{0} {1}", type, name);

            return type;
        }

        public string ToDelegateString(FunctionType function)
        {
            return string.Format("delegate {0} {{0}}({1})",
                function.ReturnType.Visit(this),
                GetArgumentsString(function, hasNames: true));
        }

        public string VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return string.Format("{0}^", function.Visit(this, quals));
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Void, walkTypedefs: true) ||
                pointee.IsPrimitiveType(PrimitiveType.UInt8, walkTypedefs: true))
            {
                return "System::IntPtr";
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Char))
            {
                return "System::String^";
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
                case PrimitiveType.Int64: return "long";
                case PrimitiveType.UInt64: return "unsigned long";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
            }

            return string.Empty;
        }

        public string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            if (string.IsNullOrEmpty(decl.Name))
                return null;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                return typeMap.Signature();
            }

            FunctionType func;
            if (decl.Type.IsPointerTo<FunctionType>(out func))
            {
                // TODO: Use SafeIdentifier()
                return string.Format("{0}^", VisitDeclaration(decl));
            }

            return decl.Type.Visit(this);
        }

        public string VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                      TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Declaration = decl;
                typeMap.Type = template;
                return typeMap.Signature();
            }

            return decl.Name;
        }

        public string VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public string VisitDeclaration(Declaration decl)
        {
            var name = decl.Visit(this);
            return string.Format("{0}::{1}", Library.Name, name);
        }

        public string VisitClassDecl(Class @class)
        {
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
    }
}
