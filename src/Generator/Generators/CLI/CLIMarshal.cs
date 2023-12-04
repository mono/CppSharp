using System;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Types;
using Delegate = CppSharp.AST.Delegate;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    public class CLIMarshalNativeToManagedPrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
    {
        public CLIMarshalNativeToManagedPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.MarshalToManaged(Context);
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
                    string value = Generator.GeneratedIdentifier("array") + Context.ParameterIndex;
                    Context.Before.WriteLine("cli::array<{0}>^ {1} = nullptr;", array.Type, value, array.Size);
                    Context.Before.WriteLine("if ({0} != 0)", Context.ReturnVarName);
                    Context.Before.WriteOpenBraceAndIndent();
                    Context.Before.WriteLine("{0} = gcnew cli::array<{1}>({2});", value, array.Type, array.Size);
                    Context.Before.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                    if (array.Type.IsPointerToPrimitiveType(PrimitiveType.Void))
                        Context.Before.WriteLineIndent("{0}[i] = new ::System::IntPtr({1}[i]);",
                            value, Context.ReturnVarName);
                    else if (!array.Type.IsPrimitiveType())
                        Context.Before.WriteLineIndent($"{value}[i] = gcnew {array.Type.ToString().TrimEnd('^')}(&{Context.ReturnVarName}[i]);");
                    else
                        Context.Before.WriteLineIndent("{0}[i] = {1}[i];", value, Context.ReturnVarName);
                    Context.Before.UnindentAndWriteCloseBrace();
                    Context.Return.Write(value);
                    break;
                case ArrayType.ArraySize.Incomplete:
                    // const char* and const char[] are the same so we can use a string
                    if (Context.Context.Options.MarshalConstCharArrayAsString &&
                        array.Type.Desugar().IsPrimitiveType(PrimitiveType.Char) &&
                        array.QualifiedType.Qualifiers.IsConst)
                    {
                        var pointer = new PointerType { QualifiedPointee = array.QualifiedType };
                        Context.ReturnType = new QualifiedType(pointer);
                        return this.VisitPointerType(pointer, quals);
                    }
                    goto case ArrayType.ArraySize.Variable;

                case ArrayType.ArraySize.Variable:
                    Context.Return.Write("nullptr");
                    break;
            }

            return true;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var delegateType = function.ToString();

            Context.Return.Write("safe_cast<{0}>(", delegateType + "^");
            Context.Return.Write("::System::Runtime::InteropServices::Marshal::");
            Context.Return.Write("GetDelegateForFunctionPointer(");
            Context.Return.Write("::System::IntPtr({0}), {1}::typeid))", Context.ReturnVarName,
                delegateType.TrimEnd('^'));

            return true;
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
                if (pointer.GetFinalQualifiedPointee().Qualifiers.IsConst !=
                    Context.ReturnType.Qualifiers.IsConst)
                {
                    var nativeTypePrinter = new CppTypePrinter(Context.Context) { PrintTypeQualifiers = false };
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
                    var nativeTypeName = desugaredPointer.Visit(typePrinter, quals);
                    Context.Return.Write("reinterpret_cast<{0}>({1})", nativeTypeName,
                        returnVarName);
                }
                else
                    Context.Return.Write(returnVarName);
                return true;
            }

            TypeMap typeMap = null;
            Context.Context.TypeMaps.FindTypeMap(pointee, out typeMap);

            Class @class;
            if (pointee.TryGetClass(out @class) && typeMap == null)
            {
                var instance = (pointer.IsReference) ? "&" + Context.ReturnVarName
                    : Context.ReturnVarName;
                WriteClassInstance(@class, instance);
                return true;
            }

            return pointer.QualifiedPointee.Visit(this);
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
                case PrimitiveType.Char16:
                case PrimitiveType.WideChar:
                case PrimitiveType.SChar:
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
                case PrimitiveType.LongDouble:
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
            if (Context.Context.TypeMaps.FindTypeMap(decl.Type, out typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.Type = typedef;
                typeMap.MarshalToManaged(Context);
                return typeMap.IsValueType;
            }

            FunctionType function;
            if (decl.Type.IsPointerTo(out function))
            {
                Context.Return.Write($"{Context.ReturnVarName} == nullptr ? nullptr : safe_cast<{typedef}>(");
                Context.Return.Write("::System::Runtime::InteropServices::Marshal::");
                Context.Return.Write("GetDelegateForFunctionPointer(");
                Context.Return.Write("::System::IntPtr({0}), {1}::typeid))", Context.ReturnVarName,
                    typedef.ToString().TrimEnd('^'));
                return true;
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(template, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = template;
                typeMap.MarshalToManaged(Context);
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
            var needsCopy = Context.MarshalKind != MarshalKind.NativeField;

            bool ownNativeInstance = false;

            if (@class.IsRefType && needsCopy)
            {
                var name = Generator.GeneratedIdentifier(Context.ReturnVarName);
                Context.Before.WriteLine($"auto {name} = new {typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}({Context.ReturnVarName});");
                instance = name;

                ownNativeInstance = true;
            }

            WriteClassInstance(@class, instance, ownNativeInstance);
            return true;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            if (!string.IsNullOrEmpty(decl.TranslationUnit.Module.OutputNamespace))
                return $"{decl.TranslationUnit.Module.OutputNamespace}::{decl.QualifiedName}";

            return decl.QualifiedName;
        }

        public void WriteClassInstance(Class @class, string instance, bool ownNativeInstance = false)
        {
            if (@class.IsRefType)
                Context.Return.Write("({0} == nullptr) ? nullptr : gcnew ",
                    instance);

            Context.Return.Write("::{0}(", QualifiedIdentifier(@class));
            Context.Return.Write($"({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*)");
            Context.Return.Write("{0}{1})", instance, ownNativeInstance ? ", true" : "");
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
            var typePrinter = new CLITypePrinter(Context.Context);
            return typePrinter.VisitDeclaration(decl).ToString();
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

    public class CLIMarshalManagedToNativePrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
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
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.MarshalToNative(Context);
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
                        string arrayPtrRet = $"__{Context.ParameterIndex}ArrayPtr";
                        Context.Before.WriteLine($"{array.Type} {arrayPtrRet}[{array.Size}];");

                        Context.ReturnVarName = arrayPtrRet;

                        Context.Return.Write(arrayPtrRet);
                    }

                    bool isPointerToPrimitive = array.Type.IsPointerToPrimitiveType(PrimitiveType.Void);
                    bool isPrimitive = array.Type.IsPrimitiveType();
                    var supportBefore = Context.Before;
                    supportBefore.WriteLine("if ({0} != nullptr)", Context.Parameter.Name);
                    supportBefore.WriteOpenBraceAndIndent();

                    supportBefore.WriteLine($"if ({Context.Parameter.Name}->Length != {array.Size})");
                    supportBefore.WriteOpenBraceAndIndent();
                    supportBefore.WriteLine($"throw gcnew ::System::InvalidOperationException(\"Source array size must equal destination array size.\");");
                    supportBefore.UnindentAndWriteCloseBrace();

                    string nativeVal = string.Empty;
                    if (isPointerToPrimitive)
                    {
                        nativeVal = ".ToPointer()";
                    }
                    else if (!isPrimitive)
                    {
                        nativeVal = "->NativePtr";
                    }

                    supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                    supportBefore.WriteLineIndent("{0}[i] = {1}{2}[i]{3};",
                        Context.ReturnVarName,
                        isPointerToPrimitive || isPrimitive ? string.Empty : "*",
                        Context.Parameter.Name,
                        nativeVal);
                    supportBefore.UnindentAndWriteCloseBrace();
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

        public bool VisitDelegateType(string type)
        {
            // We marshal function pointer types by calling
            // GetFunctionPointerForDelegate to get a native function
            // pointer ouf of the delegate. Then we can pass it in the
            // native call. Since references are not tracked in the
            // native side, we need to be careful and protect it with an
            // explicit GCHandle if necessary.

            var sb = new StringBuilder();
            sb.AppendFormat("static_cast<{0}>(", type);
            sb.Append("::System::Runtime::InteropServices::Marshal::");
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

            if (pointee is FunctionType)
            {
                var cppTypeName = pointer.Visit(typePrinter, quals);

                return VisitDelegateType(cppTypeName);
            }

            Enumeration @enum;
            if (pointee.TryGetEnum(out @enum))
            {
                var isRef = Context.Parameter.Type.IsReference() ||
                    Context.Parameter.Usage == ParameterUsage.Out ||
                    Context.Parameter.Usage == ParameterUsage.InOut;

                if (!isRef)
                {
                    ArgumentPrefix.Write("&");
                }

                Context.Return.Write("(enum ::{0}){1}{2}", @enum.QualifiedOriginalName,
                    isRef ? string.Empty : "*", Context.Parameter.Name);
                return true;
            }

            Class @class;
            if (pointee.TryGetClass(out @class) && @class.IsValueType)
            {
                if (Context.Function == null)
                    Context.Return.Write("&");
                return pointer.QualifiedPointee.Visit(this);
            }

            var finalPointee = pointer.GetFinalPointee();
            if (finalPointee.IsPrimitiveType())
            {
                var cppTypeName = pointer.Visit(typePrinter, quals);

                Context.Return.Write("({0})", cppTypeName);
                Context.Return.Write(Context.Parameter.Name);
                return true;
            }

            // Pass address of argument if the parameter is a pointer to the pointer itself.
            if (pointee is PointerType && !Context.Parameter.Type.IsReference())
            {
                ArgumentPrefix.Write("&");
            }

            return pointer.QualifiedPointee.Visit(this);
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
                case PrimitiveType.SChar:
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
            if (Context.Context.TypeMaps.FindTypeMap(decl.Type, out typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.MarshalToNative(Context);
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
                    cppTypeName = decl.Type.Visit(typePrinter, quals);
                }

                VisitDelegateType(cppTypeName);
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
            if (Context.Context.TypeMaps.FindTypeMap(template, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = template;
                typeMap.MarshalToNative(Context);
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
            var type = Context.Parameter.Type.Desugar();
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.MarshalToNative(Context);
                return;
            }

            var method = Context.Function as Method;
            if (type.IsReference() && (method == null ||
                // redundant for comparison operators, they are handled in a special way
                (method.OperatorKind != CXXOperatorKind.EqualEqual &&
                method.OperatorKind != CXXOperatorKind.ExclaimEqual)))
            {
                Context.Before.WriteLine("if (ReferenceEquals({0}, nullptr))", Context.Parameter.Name);
                Context.Before.WriteLineIndent(
                    "throw gcnew ::System::ArgumentNullException(\"{0}\", " +
                    "\"Cannot be null because it is a C++ reference (&).\");",
                    Context.Parameter.Name);
            }

            if (!type.SkipPointerRefs().IsPointer())
            {
                Context.Return.Write("*");

                if (Context.Parameter.Type.IsReference())
                    VarPrefix.Write("&");
                else
                {
                    Context.Before.WriteLine($"if (ReferenceEquals({Context.Parameter.Name}, nullptr))");
                    Context.Before.WriteLineIndent(
                        $@"throw gcnew ::System::ArgumentNullException(""{
                            Context.Parameter.Name}"", ""Cannot be null because it is passed by value."");");
                }
            }

            if (method != null
                && method.Conversion == MethodConversionKind.FunctionToInstanceMethod
                && Context.ParameterIndex == 0)
            {
                Context.Return.Write($"({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*)");
                Context.Return.Write("NativePtr");
                return;
            }

            Context.Return.Write($"({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*)");
            Context.Return.Write("{0}->NativePtr", Context.Parameter.Name);
        }

        public void MarshalValueClass(Class @class)
        {
            var marshalVar = Context.VarPrefix + "_marshal" +
                Context.ParameterIndex++;

            Context.Before.WriteLine("auto {0} = ::{1}();", marshalVar,
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

            foreach (var property in @class.Properties.Where(p => !ASTUtils.CheckIgnoreProperty(p)))
            {
                if (property.Field == null)
                    continue;

                MarshalValueClassProperty(property, marshalVar);
            }
        }

        private void MarshalValueClassProperty(Property property, string marshalVar)
        {
            var fieldRef = $"{Context.Parameter.Name}.{property.Name}";

            var marshalCtx = new MarshalContext(Context.Context, Context.Indentation)
            {
                ArgName = fieldRef,
                ParameterIndex = Context.ParameterIndex++,
                ReturnVarName = $"{marshalVar}.{property.Field.OriginalName}"
            };

            var marshal = new CLIMarshalManagedToNativePrinter(marshalCtx);
            property.Visit(marshal);

            Context.ParameterIndex = marshalCtx.ParameterIndex;

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Context.Before.Write(marshal.Context.Before);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Return))
            {
                Type type;
                Class @class;
                var isRef = property.Type.IsPointerTo(out type) &&
                    !(type.TryGetClass(out @class) && @class.IsValueType) &&
                    !type.IsPrimitiveType();

                if (isRef)
                {
                    Context.Before.WriteLine("if ({0} != nullptr)", fieldRef);
                    Context.Before.Indent();
                }

                Context.Before.WriteLine("{0}.{1} = {2};", marshalVar,
                    property.Field.OriginalName, marshal.Context.Return);

                if (isRef)
                    Context.Before.Unindent();
            }
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
            Context.Return.Write("(enum ::{0}){1}", @enum.QualifiedOriginalName,
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
