using System;
using System.Text;
using CppSharp.AST;
using CppSharp.Types;
using Delegate = CppSharp.AST.Delegate;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    public class CLIMarshalNativeToManagedPrinter : MarshalPrinter
    {
        public CLIMarshalNativeToManagedPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
            Context.MarshalToManaged = this;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap))
            {
                typeMap.Type = type;
                typeMap.CLIMarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    Context.Return.Write("nullptr");
                    break;
                case ArrayType.ArraySize.Variable:
                    Context.Return.Write("nullptr");
                    break;
            }

            return true;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var returnType = function.ReturnType;
            return returnType.Visit(this);
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            var pointee = pointer.Pointee.Desugar();

            PrimitiveType primitive;
            var param = Context.Parameter;
            if (param != null && (param.IsOut || param.IsInOut) &&
                pointee.IsPrimitiveType(out primitive))
            {
                Context.Return.Write(Context.ReturnVarName);
                return true;
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Void))
            {
                Context.Return.Write("IntPtr({0})", Context.ReturnVarName);
                return true;
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Char))
            {
                Context.Return.Write("clix::marshalString<clix::E_UTF8>({0})",
                             Context.ReturnVarName);
                return true;
            }

            if (pointee.IsPrimitiveType(out primitive))
            {
                Context.Return.Write("IntPtr({0})", Context.ReturnVarName);
                return true;
            }

            Class @class;
            if (pointee.IsTagDecl(out @class))
            {
                var instance = (pointer.IsReference) ? "&" + Context.ReturnVarName
                    : Context.ReturnVarName;
                WriteClassInstance(@class, instance);
                return true;
            }

            return pointer.Pointee.Visit(this, quals);
        }

        public override bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public override bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
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
                    Context.Return.Write(Context.ReturnVarName);
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Type = typedef;
                typeMap.CLIMarshalToManaged(Context);
                return typeMap.IsValueType;
            }

            FunctionType function;
            if (decl.Type.IsPointerTo(out function))
            {
                Context.Return.Write("safe_cast<{0}>(", typedef);
                Context.Return.Write("System::Runtime::InteropServices::Marshal::");
                Context.Return.Write("GetDelegateForFunctionPointer(");
                Context.Return.Write("IntPtr({0}), {1}::typeid))",Context.ReturnVarName,
                    typedef.ToString().TrimEnd('^'));
                return true;
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(template, out typeMap))
            {
                typeMap.Type = template;
                typeMap.CLIMarshalToManaged(Context);
                return true;
            }

            return template.Template.Visit(this);
        }

        public override bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            var instance = string.Empty;

            if (!Context.ReturnType.Type.IsPointer())
                instance += "&";

            instance += Context.ReturnVarName;
            var needsCopy = !(Context.Declaration is Field);

            if (@class.IsRefType && needsCopy)
            {
                var name = Generator.GeneratedIdentifier(Context.ReturnVarName);
                Context.SupportBefore.WriteLine("auto {0} = new ::{1}({2});", name,
                    @class.QualifiedOriginalName, Context.ReturnVarName);
                instance = name;
            }

            WriteClassInstance(@class, instance);
            return true;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            if (Context.Driver.Options.GenerateLibraryNamespace)
                return string.Format("{0}::{1}", Context.Driver.Options.OutputNamespace,
                    decl.QualifiedName);
            return string.Format("{0}", decl.QualifiedName);
        }

        public void WriteClassInstance(Class @class, string instance)
        {
            if (@class.IsRefType)
                Context.Return.Write("gcnew ");

            Context.Return.Write("{0}(", QualifiedIdentifier(@class));
            Context.Return.Write("(::{0}*)", @class.QualifiedOriginalName);
            Context.Return.Write("{0})", instance);
        }

        public override bool VisitFieldDecl(Field field)
        {
            return field.Type.Visit(this);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public override bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            Context.Parameter = parameter;
            var ret = parameter.Type.Visit(this, parameter.QualifiedType.Qualifiers);
            Context.Parameter = null;

            return ret;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write("({0}){1}", ToCLITypeName(@enum),
                Context.ReturnVarName);
            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            return variable.Type.Visit(this, variable.QualifiedType.Qualifiers);
        }

        private string ToCLITypeName(Declaration decl)
        {
            var typePrinter = new CLITypePrinter(Context.Driver);
            return typePrinter.VisitDeclaration(decl);
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
        }

        public override bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public override bool VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public bool VisitDelegate(Delegate @delegate)
        {
            throw new NotImplementedException();
        }
    }

    public class CLIMarshalManagedToNativePrinter : MarshalPrinter
    {
        public readonly TextGenerator VarPrefix;
        public readonly TextGenerator ArgumentPrefix;

        public CLIMarshalManagedToNativePrinter(MarshalContext ctx) 
            : base(ctx)
        {
            VarPrefix = new TextGenerator();
            ArgumentPrefix = new TextGenerator();

            Context.MarshalToNative = this;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap))
            {
                typeMap.Type = type;
                typeMap.CLIMarshalToNative(Context);
                return false;
            }

            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return false;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var returnType = function.ReturnType;
            return returnType.Visit(this);
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
            sb.AppendFormat("static_cast<{0}>(", type);
            sb.Append("System::Runtime::InteropServices::Marshal::");
            sb.Append("GetFunctionPointerForDelegate(");
            sb.AppendFormat("{0}).ToPointer())", Context.Parameter.Name);
            Context.Return.Write(sb.ToString());

            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            var pointee = pointer.Pointee.Desugar();

            if ((pointee.IsPrimitiveType(PrimitiveType.Char) ||
                pointee.IsPrimitiveType(PrimitiveType.WideChar)) &&
                pointer.QualifiedPointee.Qualifiers.IsConst)
            {
                Context.SupportBefore.WriteLine(
                    "auto _{0} = clix::marshalString<clix::E_UTF8>({1});",
                    Context.ArgName, Context.Parameter.Name);

                Context.Return.Write("_{0}.c_str()", Context.ArgName);
                return true;
            }

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;

                var cppTypePrinter = new CppTypePrinter(Context.Driver.TypeDatabase);
                var cppTypeName = pointer.Visit(cppTypePrinter, quals);

                return VisitDelegateType(function, cppTypeName);
            }

            Class @class;
            if (pointee.IsTagDecl(out @class) && @class.IsValueType)
            {
                if (Context.Function == null)
                    Context.Return.Write("&");
                return pointee.Visit(this, quals);
            }

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive))
            {
                var cppTypePrinter = new CppTypePrinter(Context.Driver.TypeDatabase);
                var cppTypeName = pointer.Visit(cppTypePrinter, quals);

                Context.Return.Write("({0})", cppTypeName);
                Context.Return.Write("{0}.ToPointer()", Context.Parameter.Name);
                return true;
            }

            return pointer.Pointee.Visit(this, quals);
        }

        public override bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public override bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
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
                    Context.Return.Write(Context.Parameter.Name);
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.CLIMarshalToNative(Context);
                return typeMap.IsValueType;
            }

            FunctionType func;
            if (decl.Type.IsPointerTo(out func))
            {
                VisitDelegateType(func, "::" + typedef.Declaration.QualifiedOriginalName);
                return true;
            }

            PrimitiveType primitive;
            if (decl.Type.Desugar().IsPrimitiveType(out primitive))
            {
                Context.Return.Write("(::{0})", typedef.Declaration.QualifiedOriginalName);
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap = null;
            if (Context.Driver.TypeDatabase.FindTypeMap(template, out typeMap))
            {
                typeMap.Type = template;
                typeMap.CLIMarshalToNative(Context);
                return true;
            }

            return template.Template.Visit(this);
        }

        public override bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            Context.Return.Write(param.Parameter.Name);
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            if (@class.IsValueType)
            {
                MarshalValueClass(@class);
            }
            else
            {
                MarshalRefClass(@class);
            }

            return true;
        }

        private void MarshalRefClass(Class @class)
        {
            TypeMap typeMap = null;
            if (Context.Driver.TypeDatabase.FindTypeMap(@class, out typeMap))
            {
                typeMap.CLIMarshalToNative(Context);
                return;
            }

            if (!Context.Parameter.Type.IsPointer())
            {
                Context.Return.Write("*");

                if (Context.Parameter.Type.IsReference())
                    VarPrefix.Write("&");
            }

            var method = Context.Function as Method;
            if (method != null
                && method.Conversion == MethodConversionKind.FunctionToInstanceMethod
                && Context.ParameterIndex == 0)
            {
                Context.Return.Write("(::{0}*)", @class.QualifiedOriginalName);
                Context.Return.Write("NativePtr");
                return;
            }

            Context.Return.Write("(::{0}*)", @class.QualifiedOriginalName);
            Context.Return.Write("{0}->NativePtr", Context.Parameter.Name);
        }

        public void MarshalValueClass(Class @class)
        {
            var marshalVar = Context.MarshalVarPrefix + "_marshal" +
                Context.ParameterIndex++;

            Context.SupportBefore.WriteLine("auto {0} = ::{1}();", marshalVar,
                @class.QualifiedOriginalName);

            MarshalValueClassFields(@class, marshalVar);

            Context.Return.Write(marshalVar);

            var param = Context.Parameter;
            if (param.Kind == ParameterKind.OperatorParameter)
                return;

            if (param.Type.IsPointer())
                ArgumentPrefix.Write("&");
        }

        public void MarshalValueClassFields(Class @class, string marshalVar)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass || @base.Class.Ignore) 
                    continue;

                var baseClass = @base.Class;
                MarshalValueClassFields(baseClass, marshalVar);
            }

            foreach (var field in @class.Fields)
            {
                if(field.Ignore)
                    continue;

                MarshalValueClassField(field, marshalVar);
            }
        }

        private void MarshalValueClassField(Field field, string marshalVar)
        {
            var fieldRef = string.Format("{0}.{1}", Context.Parameter.Name,
                                         field.Name);

            var marshalCtx = new MarshalContext(Context.Driver)
                                 {
                                     ArgName = fieldRef,
                                     ParameterIndex = Context.ParameterIndex++,
                                     MarshalVarPrefix = Context.MarshalVarPrefix
                                 };

            var marshal = new CLIMarshalManagedToNativePrinter(marshalCtx);
            field.Visit(marshal);

            Context.ParameterIndex = marshalCtx.ParameterIndex;

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Context.SupportBefore.Write(marshal.Context.SupportBefore);

            Type type;
            Class @class;
            var isRef = field.Type.IsPointerTo(out type) &&
                !(type.IsTagDecl(out @class) && @class.IsValueType) &&
                !type.IsPrimitiveType();

            if (isRef)
            {
                Context.SupportBefore.WriteLine("if ({0} != nullptr)", fieldRef);
                Context.SupportBefore.PushIndent();
            }

            Context.SupportBefore.WriteLine("{0}.{1} = {2};", marshalVar,
                field.OriginalName, marshal.Context.Return);

            if (isRef)
                Context.SupportBefore.PopIndent();
        }

        public override bool VisitFieldDecl(Field field)
        {
            Context.Parameter = new Parameter
                {
                    Name = Context.ArgName,
                    QualifiedType = field.QualifiedType
                };

            return field.Type.Visit(this);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public override bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            return parameter.Type.Visit(this);
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write("(::{0}){1}", @enum.QualifiedOriginalName,
                         Context.Parameter.Name);
            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            throw new NotImplementedException();
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
        }

        public override bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public override bool VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public bool VisitDelegate(Delegate @delegate)
        {
            throw new NotImplementedException();
        }
    }
}
