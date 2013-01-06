using System;
using System.Globalization;
using System.Text;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public class CLIMarshalNativeToManagedPrinter : ITypeVisitor<bool>,
                                                    IDeclVisitor<bool>
    {
        public string Support = null;
        public string Return = null;

        Generator Generator { get; set; }
        MarshalContext Context { get; set; }

        public CLIMarshalNativeToManagedPrinter(Generator gen, MarshalContext ctx)
        {
            Generator = gen;
            Context = ctx;
        }

        public bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return false;
        }

        public bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var returnType = function.ReturnType;
            return returnType.Visit(this, quals);
        }

        public bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            if (pointee.IsPrimitiveType(PrimitiveType.Void))
            {
                Return = "IntPtr()";
                return true;
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Char))
            {
                Return = string.Format("clix::marshalString<clix::E_UTF8>({0})",
                                       Context.ReturnVarName);
                return true;
            }

            if (!pointee.Visit(this, quals))
                return false;

            if (pointer.IsReference)
                Return = string.Format("&{0}", Return);

            return true;
        }

        public bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.Int8:
                case PrimitiveType.UInt8:
                case PrimitiveType.Int16:
                case PrimitiveType.UInt16:
                case PrimitiveType.Int32:
                case PrimitiveType.UInt32:
                case PrimitiveType.Int64:
                case PrimitiveType.UInt64:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Return = Context.ReturnVarName;
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            var typeDb = Generator.TypeDatabase;
            if (typeDb.FindTypeMap(decl.QualifiedOriginalName, out typeMap))
            {
                Return = typeMap.MarshalFromNative(Context);
                return typeMap.IsValueType;
            }

            // TODO: How should function pointers behave here?

            return decl.Type.Visit(this);
        }

        public bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            return template.Template.Visit(this);
        }

        public bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public bool VisitClassDecl(Class @class)
        {
            Return = string.Format("gcnew {0}({1})", @class.Name, Context.ReturnVarName);
            return @class.IsValueType;
        }

        public bool VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public bool VisitParameterDecl(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public bool VisitEnumDecl(Enumeration @enum)
        {
            Return = string.Format("({0}){1}", ToCLITypeName(@enum), Context.ReturnVarName);
            return true;
        }

        private string ToCLITypeName(Declaration decl)
        {
            var typePrinter = new CLITypePrinter(Generator);
            return typePrinter.VisitDeclaration(decl);
        }

        public bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }
    }

    public class CLIMarshalManagedToNativePrinter : ITypeVisitor<bool>,
                                                    IDeclVisitor<bool>
    {
        public string Support = null;
        public string Return = null;

        Generator Generator { get; set; }
        MarshalContext Context { get; set; }

        public CLIMarshalManagedToNativePrinter(Generator gen, MarshalContext ctx)
        {
            Generator = gen;
            Context = ctx;
        }

        public bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return false;
        }

        public bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var returnType = function.ReturnType;
            return returnType.Visit(this, quals);
        }

        public bool VisitDelegateType(FunctionType function, string type)
        {
            // We marshal function pointer types by calling
            // GetFunctionPointerForDelegate to get a native function
            // pointer ouf of the delegate. Then we can pass it in the
            // native call. Since references are not tracked in the
            // native side, we need to be careful and protect it with an
            // explicit GCHandle if necessary.

            var sb = new StringBuilder();
            sb.AppendFormat("static_cast<::{0}>(", type);
            sb.Append("System::Runtime::InteropServices::Marshal::");
            sb.Append("GetFunctionPointerForDelegate(");
            sb.AppendFormat("{0}).ToPointer())", Context.Parameter.Name);
            Return = sb.ToString();

            return true;
        }

        public bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            if (pointee.IsPrimitiveType(PrimitiveType.Void))
            {
                Return = string.Format("{0}.ToPointer()", Context.Parameter.Name);
                return true;
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Char))
            {
                Support = string.Format(
                    "auto _{0} = clix::marshalString<clix::E_UTF8>({1});",
                    Context.ArgName, Context.Parameter.Name);

                Return = string.Format("_{0}.c_str()", Context.ArgName);
                return true;
            }

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                // TODO: We have to translate the function type typedef to C/C++
                return VisitDelegateType(function, function.ToString());
            }

            return pointee.Visit(this, quals);
        }

        public bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.Int8:
                case PrimitiveType.UInt8:
                case PrimitiveType.Int16:
                case PrimitiveType.UInt16:
                case PrimitiveType.Int32:
                case PrimitiveType.UInt32:
                case PrimitiveType.Int64:
                case PrimitiveType.UInt64:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Return = Context.Parameter.Name;
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (Generator.TypeDatabase.FindTypeMap(decl.QualifiedOriginalName, out typeMap))
            {
                Return = typeMap.MarshalToNative(Context);
                return typeMap.IsValueType;
            }

            FunctionType func;
            if (typedef.Declaration.Type.IsPointerTo<FunctionType>(out func))
            {
                VisitDelegateType(func, typedef.Declaration.OriginalName);
                return true;
            }

            return decl.Type.Visit(this);
        }

        public bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            return template.Template.Visit(this);
        }

        public bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public bool VisitClassDecl(Class @class)
        {
            if (@class.IsValueType)
            {
                Return = string.Format("(::{0}*)&{1}", @class.OriginalName,
                    Context.Parameter.Name);
            }
            else
            {
                Return = string.Format("{0}->NativePtr", Context.Parameter.Name);
            }

            return true;
        }

        public bool VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public bool VisitParameterDecl(Parameter parameter)
        {
            return parameter.Type.Visit(this);
        }

        public bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public bool VisitEnumDecl(Enumeration @enum)
        {
            Return = string.Format("(::{0}){1}", @enum.QualifiedOriginalName,
                Context.Parameter.Name);
            return true;
        }

        public bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }
    }
}
