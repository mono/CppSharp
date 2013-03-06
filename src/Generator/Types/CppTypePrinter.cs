using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cxxi.Types
{
    public class CppTypePrinter : ITypePrinter, IDeclVisitor<string>
    {
        private readonly ITypeMapDatabase TypeMapDatabase;

        public CppTypePrinter(ITypeMapDatabase database)
        {
            TypeMapDatabase = database;
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
            var s = string.Empty;

            if (quals.IsConst)
                s += "const ";

            var mod = ConvertModifierToString(pointer.Modifier);
            var pointee = pointer.Pointee.Visit(this, quals);

            s += string.Format("{0}{1}", pointee, mod);
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
            }

            throw new NotSupportedException();
        }

        public string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            return typedef.Declaration.Type.Visit(this);
        }

        public string VisitTemplateSpecializationType(TemplateSpecializationType template, TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;
            return decl.Visit(this);
        }

        public string VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            if (param.Parameter.Name == null)
                return string.Empty;

            return param.Parameter.Name;
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
            var arguments = function.Arguments;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Arguments, hasNames: false);

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

        public string VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public string VisitClassDecl(Class @class)
        {
            return string.Format("::{0}", @class.QualifiedOriginalName);
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
            throw new NotImplementedException();
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

        public string VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }
    }
}
