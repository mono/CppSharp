using System;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Types;
using Delegate = CppSharp.AST.Delegate;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    public class CLIMarshalNativeToManagedPrinter : MarshalPrinter<MarshalContext>
    {
        public CLIMarshalNativeToManagedPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = type;
                typeMap.CLIMarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (!VisitType(tag, quals))
                return false;

            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    var supportBefore = Context.SupportBefore;
                    string value = Generator.GeneratedIdentifier("array") + Context.ParameterIndex;
                    supportBefore.WriteLine("cli::array<{0}>^ {1} = nullptr;", array.Type, value, array.Size);
                    supportBefore.WriteLine("if ({0} != 0)", Context.ReturnVarName);
                    supportBefore.WriteStartBraceIndent();
                    supportBefore.WriteLine("{0} = gcnew cli::array<{1}>({2});", value, array.Type, array.Size);
                    supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                    if (array.Type.IsPointerToPrimitiveType(PrimitiveType.Void))
                        supportBefore.WriteLineIndent("{0}[i] = new ::System::IntPtr({1}[i]);",
                            value, Context.ReturnVarName);
                    else
                        supportBefore.WriteLineIndent("{0}[i] = {1}[i];", value, Context.ReturnVarName);
                    supportBefore.WriteCloseBraceIndent();
                    Context.Return.Write(value);
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

            if (pointee.IsPrimitiveType(PrimitiveType.Void))
            {
                Context.Return.Write("::System::IntPtr({0})", Context.ReturnVarName);
                return true;
            }

            if (CSharpTypePrinter.IsConstCharString(pointer))
            {
                Context.Return.Write(MarshalStringToManaged(Context.ReturnVarName,
                    pointer.Pointee.Desugar() as BuiltinType));
                return true;
            }

            PrimitiveType primitive;
            var param = Context.Parameter;
            if (param != null && (param.IsOut || param.IsInOut) &&
                pointee.IsPrimitiveType(out primitive))
            {
                Context.Return.Write(Context.ReturnVarName);
                return true;
            }

            if (pointee.IsPrimitiveType(out primitive))
            {
                var returnVarName = Context.ReturnVarName;
                if (quals.IsConst != Context.ReturnType.Qualifiers.IsConst)
                {
                    var nativeTypePrinter = new CppTypePrinter(Context.Driver.TypeDatabase, false);
                    var returnType = Context.ReturnType.Type.Desugar();
                    var constlessPointer = new PointerType()
                    {
                        IsDependent = pointer.IsDependent,
                        Modifier = pointer.Modifier,
                        QualifiedPointee = new QualifiedType(returnType.GetPointee())
                    };
                    var nativeConstlessTypeName = constlessPointer.Visit(nativeTypePrinter, new TypeQualifiers());
                    returnVarName = string.Format("const_cast<{0}>({1})",
                        nativeConstlessTypeName, Context.ReturnVarName);
                }
                if (pointer.Pointee is TypedefType)
                {
                    var desugaredPointer = new PointerType()
                    {
                        IsDependent = pointer.IsDependent,
                        Modifier = pointer.Modifier,
                        QualifiedPointee = new QualifiedType(pointee)
                    };
                    var nativeTypePrinter = new CppTypePrinter(Context.Driver.TypeDatabase);
                    var nativeTypeName = desugaredPointer.Visit(nativeTypePrinter, quals);
                    Context.Return.Write("reinterpret_cast<{0}>({1})", nativeTypeName,
                        returnVarName);
                }
                else
                    Context.Return.Write(returnVarName);
                return true;
            }

            TypeMap typeMap = null;
            Context.Driver.TypeDatabase.FindTypeMap(pointee, out typeMap);

            Class @class;
            if (pointee.TryGetClass(out @class) && typeMap == null)
            {
                var instance = (pointer.IsReference) ? "&" + Context.ReturnVarName
                    : Context.ReturnVarName;
                WriteClassInstance(@class, instance);
                return true;
            }

            return pointer.Pointee.Visit(this, quals);
        }

        private string MarshalStringToManaged(string varName, BuiltinType type)
        {
            var encoding = type.Type == PrimitiveType.Char ?
                Encoding.ASCII : Encoding.Unicode;

            if (Equals(encoding, Encoding.ASCII))
                encoding = Context.Driver.Options.Encoding;

            if (Equals(encoding, Encoding.ASCII))
                return string.Format("clix::marshalString<clix::E_UTF8>({0})", varName);

            if (Equals(encoding, Encoding.Unicode) ||
                Equals(encoding, Encoding.BigEndianUnicode))
                return string.Format("clix::marshalString<clix::E_UTF16>({0})", varName);

            throw new NotSupportedException(string.Format("{0} is not supported yet.",
                Context.Driver.Options.Encoding.EncodingName));
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
                case PrimitiveType.Char:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.Null:
                    Context.Return.Write(Context.ReturnVarName);
                    return true;
            }

            throw new NotSupportedException();
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap) && typeMap.DoesMarshalling)
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
            if (Context.Driver.TypeDatabase.FindTypeMap(template, out typeMap) && typeMap.DoesMarshalling)
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
                Context.Return.Write("({0} == nullptr) ? nullptr : gcnew ",
                    instance);

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

    public class CLIMarshalManagedToNativePrinter : MarshalPrinter<MarshalContext>
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
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = type;
                typeMap.CLIMarshalToNative(Context);
                return false;
            }

            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (!VisitType(tag, quals))
                return false;

            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    if (string.IsNullOrEmpty(Context.ReturnVarName))
                    {
                        const string pinnedPtr = "__pinnedPtr";
                        Context.SupportBefore.WriteLine("cli::pin_ptr<{0}> {1} = &{2}[0];",
                            array.Type, pinnedPtr, Context.Parameter.Name);
                        const string arrayPtr = "__arrayPtr";
                        Context.SupportBefore.WriteLine("{0}* {1} = {2};", array.Type, arrayPtr, pinnedPtr);
                        Context.Return.Write("({0} (&)[{1}]) {2}", array.Type, array.Size, arrayPtr);
                    }
                    else
                    {
                        var supportBefore = Context.SupportBefore;
                        supportBefore.WriteLine("if ({0} != nullptr)", Context.ArgName);
                        supportBefore.WriteStartBraceIndent();
                        supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                        supportBefore.WriteLineIndent("{0}[i] = {1}[i]{2};",
                            Context.ReturnVarName, Context.ArgName,
                            array.Type.IsPointerToPrimitiveType(PrimitiveType.Void) ? ".ToPointer()" : string.Empty);
                        supportBefore.WriteCloseBraceIndent();   
                    }
                    break;
                default:
                    Context.Return.Write("null");
                    break;
            }
            return true;
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

            Enumeration @enum;
            if (pointee.TryGetEnum(out @enum))
            {
                var isRef = Context.Parameter.Usage == ParameterUsage.Out ||
                    Context.Parameter.Usage == ParameterUsage.InOut;

                ArgumentPrefix.Write("&");
                Context.Return.Write("(::{0}){1}{2}", @enum.QualifiedOriginalName,
                    isRef ? string.Empty : "*", Context.Parameter.Name);
                return true;
            }

            Class @class;
            if (pointee.TryGetClass(out @class) && @class.IsValueType)
            {
                if (Context.Function == null)
                    Context.Return.Write("&");
                return pointee.Visit(this, quals);
            }

            var finalPointee = pointer.GetFinalPointee();
            if (finalPointee.IsPrimitiveType())
            {
                var cppTypePrinter = new CppTypePrinter(Context.Driver.TypeDatabase);
                var cppTypeName = pointer.Visit(cppTypePrinter, quals);

                Context.Return.Write("({0})", cppTypeName);
                Context.Return.Write(Context.Parameter.Name);
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
                case PrimitiveType.Char:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Context.Return.Write(Context.Parameter.Name);
                    return true;
            }

            return false;
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.CLIMarshalToNative(Context);
                return typeMap.IsValueType;
            }

            FunctionType func;
            if (decl.Type.IsPointerTo(out func))
            {
                // Use the original typedef name if available, otherwise just use the function pointer type
                string cppTypeName;
                if (!decl.IsSynthetized)
                    cppTypeName = "::" + typedef.Declaration.QualifiedOriginalName;
                else
                {
                    var cppTypePrinter = new CppTypePrinter(Context.Driver.TypeDatabase);
                    cppTypeName = decl.Type.Visit(cppTypePrinter, quals);
                }

                VisitDelegateType(func, cppTypeName);
                return true;
            }

            PrimitiveType primitive;
            if (decl.Type.IsPrimitiveType(out primitive))
            {
                Context.Return.Write("(::{0})", typedef.Declaration.QualifiedOriginalName);
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(template, out typeMap) && typeMap.DoesMarshalling)
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
            if (Context.Driver.TypeDatabase.FindTypeMap(@class, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.CLIMarshalToNative(Context);
                return;
            }

            if (!Context.Parameter.Type.Desugar().SkipPointerRefs().IsPointer())
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

            MarshalValueClassProperties(@class, marshalVar);

            Context.Return.Write(marshalVar);

            var param = Context.Parameter;
            if (param.Kind == ParameterKind.OperatorParameter)
                return;

            if (param.Type.IsPointer())
                ArgumentPrefix.Write("&");
        }

        public void MarshalValueClassProperties(Class @class, string marshalVar)
        {
            foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class.IsDeclared))
            {
                MarshalValueClassProperties(@base.Class, marshalVar);
            }

            foreach (var property in @class.Properties.Where( p => !ASTUtils.CheckIgnoreProperty(p)))
            {
                if (property.Field == null)
                    continue;

                MarshalValueClassProperty(property, marshalVar);
            }
        }

        private void MarshalValueClassProperty(Property property, string marshalVar)
        {
            var fieldRef = string.Format("{0}.{1}", Context.Parameter.Name,
                                         property.Name);

            var marshalCtx = new MarshalContext(Context.Driver)
                                 {
                                     ArgName = fieldRef,
                                     ParameterIndex = Context.ParameterIndex++,
                                     MarshalVarPrefix = Context.MarshalVarPrefix
                                 };

            var marshal = new CLIMarshalManagedToNativePrinter(marshalCtx);
            property.Visit(marshal);

            Context.ParameterIndex = marshalCtx.ParameterIndex;

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Context.SupportBefore.Write(marshal.Context.SupportBefore);

            Type type;
            Class @class;
            var isRef = property.Type.IsPointerTo(out type) &&
                !(type.TryGetClass(out @class) && @class.IsValueType) &&
                !type.IsPrimitiveType();

            if (isRef)
            {
                Context.SupportBefore.WriteLine("if ({0} != nullptr)", fieldRef);
                Context.SupportBefore.PushIndent();
            }

            Context.SupportBefore.WriteLine("{0}.{1} = {2};", marshalVar,
                property.Field.OriginalName, marshal.Context.Return);

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

        public override bool VisitProperty(Property property)
        {
            Context.Parameter = new Parameter
                {
                    Name = Context.ArgName,
                    QualifiedType = property.QualifiedType
                };

            return base.VisitProperty(property);
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
