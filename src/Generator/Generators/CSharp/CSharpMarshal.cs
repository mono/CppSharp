using System;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public enum CSharpMarshalKind
    {
        Unknown,
        NativeField,
        GenericDelegate,
        DefaultExpression,
        VTableReturnValue
    }

    public class CSharpMarshalContext : MarshalContext
    {
        public CSharpMarshalContext(BindingContext context)
            : base(context)
        {
            Kind = CSharpMarshalKind.Unknown;
            ArgumentPrefix = new TextGenerator();
            Cleanup = new TextGenerator();
        }

        public CSharpMarshalKind Kind { get; set; }
        public QualifiedType FullType;

        public TextGenerator ArgumentPrefix { get; private set; }
        public TextGenerator Cleanup { get; private set; }
        public bool HasCodeBlock { get; set; }
    }

    public abstract class CSharpMarshalPrinter : MarshalPrinter<CSharpMarshalContext>
    {
        protected CSharpMarshalPrinter(CSharpMarshalContext context)
            : base(context)
        {
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }
    }

    public class CSharpMarshalNativeToManagedPrinter : CSharpMarshalPrinter
    {
        public CSharpMarshalNativeToManagedPrinter(CSharpMarshalContext context)
            : base(context)
        {
            typePrinter = new CSharpTypePrinter(context.Context);
        }

        public bool MarshalsParameter { get; set; }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = type;
                typeMap.CSharpMarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(decl, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Declaration = decl;
                typeMap.CSharpMarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    var supportBefore = Context.SupportBefore;
                    string value = Generator.GeneratedIdentifier("value");
                    supportBefore.WriteLine("{0}[] {1} = null;", array.Type, value, array.Size);
                    supportBefore.WriteLine("if ({0} != null)", Context.ReturnVarName);
                    supportBefore.WriteStartBraceIndent();
                    supportBefore.WriteLine("{0} = new {1}[{2}];", value, array.Type, array.Size);
                    supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                    if (array.Type.IsPointerToPrimitiveType(PrimitiveType.Void))
                        supportBefore.WriteLineIndent("{0}[i] = new global::System.IntPtr({1}[i]);",
                            value, Context.ReturnVarName);
                    else
                    {
                        var arrayType = array.Type.Desugar();
                        Class @class;
                        if (arrayType.TryGetClass(out @class) && @class.IsRefType)
                            supportBefore.WriteLineIndent(
                                "{0}[i] = {1}.{2}(*(({1}.{3}*)&({4}[i * sizeof({1}.{3})])));",
                                value, array.Type, Helpers.CreateInstanceIdentifier,
                                Helpers.InternalStruct, Context.ReturnVarName);
                        else
                        {
                            if (arrayType.IsPrimitiveType(PrimitiveType.Char) &&
                                Context.Context.Options.MarshalCharAsManagedChar)
                            {
                                supportBefore.WriteLineIndent(
                                    "{0}[i] = global::System.Convert.ToChar({1}[i]);",
                                    value, Context.ReturnVarName);
                            }
                            else
                            {
                                supportBefore.WriteLineIndent("{0}[i] = {1}[i];",
                                    value, Context.ReturnVarName);
                            }
                        }
                    }
                    supportBefore.WriteCloseBraceIndent();
                    Context.Return.Write(value);
                    break;
                case ArrayType.ArraySize.Incomplete:
                    // const char* and const char[] are the same so we can use a string
                    if (array.Type.Desugar().IsPrimitiveType(PrimitiveType.Char) &&
                        array.QualifiedType.Qualifiers.IsConst)
                        return VisitPointerType(new PointerType
                            {
                                QualifiedPointee = array.QualifiedType
                            }, quals);

                    goto case ArrayType.ArraySize.Variable;
                case ArrayType.ArraySize.Variable:
                    Context.Return.Write(Context.ReturnVarName);
                    break;
            }

            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            var param = Context.Parameter;
            var isRefParam = param != null && (param.IsInOut || param.IsOut);

            var pointee = pointer.Pointee.Desugar();
            bool marshalPointeeAsString = CSharpTypePrinter.IsConstCharString(pointee) && isRefParam;

            if ((CSharpTypePrinter.IsConstCharString(pointer) && !MarshalsParameter) ||
                marshalPointeeAsString)
            {
                Context.Return.Write(MarshalStringToManaged(Context.ReturnVarName,
                    pointer.GetFinalPointee().Desugar() as BuiltinType));
                return true;
            }

            var finalPointee = pointer.GetFinalPointee();
            PrimitiveType primitive;
            if (finalPointee.IsPrimitiveType(out primitive) || finalPointee.IsEnumType())
            {
                if (isRefParam)
                {
                    Context.Return.Write("_{0}", param.Name);
                    return true;
                }

                if (Context.Context.Options.MarshalCharAsManagedChar && primitive == PrimitiveType.Char)
                    Context.Return.Write(string.Format("({0}) ", pointer));
                Context.Return.Write(Context.ReturnVarName);
                return true;
            }

            return pointer.Pointee.Visit(this, quals);
        }

        private string MarshalStringToManaged(string varName, BuiltinType type)
        {
            var isChar = type.Type == PrimitiveType.Char;
            var encoding = isChar ? Encoding.ASCII : Encoding.Unicode;

            if (Equals(encoding, Encoding.ASCII))
                encoding = Context.Context.Options.Encoding;

            if (Equals(encoding, Encoding.ASCII))
                return string.Format("Marshal.PtrToStringAnsi({0})", varName);

            // If we reach this, we know the string is Unicode.
            if (type.Type == PrimitiveType.Char ||
                Context.Context.TargetInfo.WCharWidth == 16)
                return string.Format("Marshal.PtrToStringUni({0})", varName);

            // If we reach this, we should have an UTF-32 wide string.
            const string encodingName = "System.Text.Encoding.UTF32";
            return string.Format(
                "CppSharp.Runtime.Helpers.MarshalEncodedString({0}, {1})",
                varName, encodingName);
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Char:
                    // returned structs must be blittable and char isn't
                    if (Context.Context.Options.MarshalCharAsManagedChar)
                    {
                        Context.Return.Write("global::System.Convert.ToChar({0})",
                            Context.ReturnVarName);
                        return true;
                    }
                    goto default;
                case PrimitiveType.Char16:
                    return false;
                case PrimitiveType.Bool:
                    if (Context.Kind == CSharpMarshalKind.NativeField)
                    {
                        // returned structs must be blittable and bool isn't
                        Context.Return.Write("{0} != 0", Context.ReturnVarName);
                        return true;
                    }
                    goto default;
                default:
                    Context.Return.Write(Context.ReturnVarName);
                    return true;
            }
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (!VisitType(typedef, quals))
                return false;

            var decl = typedef.Declaration;

            FunctionType function;
            if (decl.Type.IsPointerTo(out function))
            {
                var ptrName = Generator.GeneratedIdentifier("ptr") +
                    Context.ParameterIndex;

                Context.SupportBefore.WriteLine("var {0} = {1};", ptrName,
                    Context.ReturnVarName);

                Context.Return.Write("({1})Marshal.GetDelegateForFunctionPointer({0}, typeof({1}))",
                    ptrName, typedef.ToString());
                return true;
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var ptrName = Generator.GeneratedIdentifier("ptr") + Context.ParameterIndex;

            Context.SupportBefore.WriteLine("var {0} = {1};", ptrName,
                Context.ReturnVarName);

            Context.Return.Write("({1})Marshal.GetDelegateForFunctionPointer({0}, typeof({1}))",
                ptrName, function.ToString());
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var originalClass = @class.OriginalClass ?? @class;
            Type returnType = Context.ReturnType.Type.Desugar();

            // if the class is an abstract impl, use the original for the object map
            var qualifiedClass = originalClass.Visit(typePrinter);

            if (returnType.IsAddress())
                Context.Return.Write(HandleReturnedPointer(@class, qualifiedClass.Type));
            else
                Context.Return.Write("{0}.{1}({2})", qualifiedClass, Helpers.CreateInstanceIdentifier, Context.ReturnVarName);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write("{0}", Context.ReturnVarName);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (parameter.Usage == ParameterUsage.Unknown || parameter.IsIn)
                return base.VisitParameterDecl(parameter);

            var ctx = new CSharpMarshalContext(Context.Context)
            {
                ReturnType = Context.ReturnType,
                ReturnVarName = Context.ReturnVarName
            };

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            parameter.Type.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(ctx.SupportBefore))
                Context.SupportBefore.WriteLine(ctx.SupportBefore);

            if (!string.IsNullOrWhiteSpace(ctx.Return) &&
                !parameter.Type.IsPrimitiveTypeConvertibleToRef())
            {
                Context.SupportBefore.WriteLine("var _{0} = {1};", parameter.Name,
                    ctx.Return);
            }

            Context.Return.Write("{0}{1}",
                parameter.Type.IsPrimitiveTypeConvertibleToRef() ? "ref *" : "_", parameter.Name);
            return true;
        }

        private string HandleReturnedPointer(Class @class, string qualifiedClass)
        {
            var originalClass = @class.OriginalClass ?? @class;
            var ret = Generator.GeneratedIdentifier("result") + Context.ParameterIndex;
            var qualifiedIdentifier = @class.Visit(typePrinter);
            Context.SupportBefore.WriteLine("{0} {1};", qualifiedIdentifier, ret);
            Context.SupportBefore.WriteLine("if ({0} == IntPtr.Zero) {1} = {2};", Context.ReturnVarName, ret,
                originalClass.IsRefType ? "null" : string.Format("new {0}()", qualifiedClass));
            if (originalClass.IsRefType)
            {
                Context.SupportBefore.WriteLine(
                    "else if ({0}.NativeToManagedMap.ContainsKey({1}))", qualifiedClass, Context.ReturnVarName);
                Context.SupportBefore.WriteLineIndent("{0} = ({1}) {2}.NativeToManagedMap[{3}];",
                    ret, qualifiedIdentifier, qualifiedClass, Context.ReturnVarName);
                var dtor = originalClass.Destructors.FirstOrDefault();
                if (dtor != null && dtor.IsVirtual)
                {
                    Context.SupportBefore.WriteLine("else {0}{1} = ({2}) {3}.{4}({5}{6});",
                        MarshalsParameter
                            ? string.Empty
                            : string.Format("{0}.NativeToManagedMap[{1}] = ", qualifiedClass, Context.ReturnVarName),
                        ret, qualifiedIdentifier, qualifiedClass, Helpers.CreateInstanceIdentifier, Context.ReturnVarName,
                        MarshalsParameter ? ", skipVTables: true" : string.Empty);
                }
                else
                {
                    Context.SupportBefore.WriteLine("else {0} = {1}.{2}({3});", ret, qualifiedClass,
                        Helpers.CreateInstanceIdentifier, Context.ReturnVarName);
                }
            }
            else
            {
                Context.SupportBefore.WriteLine("else {0} = {1}.{2}({3});", ret, qualifiedClass,
                    Helpers.CreateInstanceIdentifier, Context.ReturnVarName);
            }
            return ret;
        }

        private readonly CSharpTypePrinter typePrinter;
    }

    public class CSharpMarshalManagedToNativePrinter : CSharpMarshalPrinter
    {
        public CSharpMarshalManagedToNativePrinter(CSharpMarshalContext context)
            : base(context)
        {
            typePrinter = new CSharpTypePrinter(context.Context);
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = type;
                typeMap.CSharpMarshalToNative(Context);
                return false;
            }

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(decl, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Declaration = decl;
                typeMap.CSharpMarshalToNative(Context);
                return false;
            }

            return true;
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
                        Context.SupportBefore.WriteLine("if ({0} == null || {0}.Length != {1})",
                            Context.Parameter.Name, array.Size);
                        ThrowArgumentOutOfRangeException();
                        var ptr = "__ptr" + Context.ParameterIndex;
                        Context.SupportBefore.WriteLine("fixed ({0}* {1} = {2})",
                            array.Type, ptr, Context.Parameter.Name);
                        Context.SupportBefore.WriteStartBraceIndent();
                        Context.Return.Write("new global::System.IntPtr({0})", ptr);
                        Context.HasCodeBlock = true;
                    }
                    else
                    {
                        var supportBefore = Context.SupportBefore;
                        supportBefore.WriteLine("if ({0} != null)", Context.ArgName);
                        supportBefore.WriteStartBraceIndent();
                        Class @class;
                        var arrayType = array.Type.Desugar();
                        if (array.Type.Desugar().TryGetClass(out @class) && @class.IsRefType)
                        {
                            supportBefore.WriteLine("if (value.Length != {0})", array.Size);
                            ThrowArgumentOutOfRangeException();
                        }
                        supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                        if (@class != null && @class.IsRefType)
                        {
                            supportBefore.WriteLineIndent(
                                "{0}[i * sizeof({1}.{2})] = *((byte*)({1}.{2}*){3}[i].{4});",
                                Context.ReturnVarName, array.Type, Helpers.InternalStruct,
                                Context.ArgName, Helpers.InstanceIdentifier);
                        }
                        else
                        {
                            if (arrayType.IsPrimitiveType(PrimitiveType.Char) &&
                                Context.Context.Options.MarshalCharAsManagedChar)
                            {
                                supportBefore.WriteLineIndent(
                                    "{0}[i] = global::System.Convert.ToSByte({1}[i]);",
                                    Context.ReturnVarName, Context.ArgName);
                            }
                            else
                            {
                                supportBefore.WriteLineIndent("{0}[i] = {1}[i]{2};",
                                    Context.ReturnVarName,
                                    Context.ArgName,
                                    array.Type.IsPointerToPrimitiveType(PrimitiveType.Void)
                                        ? ".ToPointer()"
                                        : string.Empty);
                            }
                        }
                        supportBefore.WriteCloseBraceIndent();
                    }
                    break;
                default:
                    Context.Return.Write("null");
                    break;
            }
            return true;
        }

        private void ThrowArgumentOutOfRangeException()
        {
            Context.SupportBefore.WriteLineIndent(
                "throw new ArgumentOutOfRangeException(\"{0}\", " +
                "\"The dimensions of the provided array don't match the required size.\");",
                Context.Parameter.Name);
        }

        public bool VisitDelegateType(FunctionType function, string type)
        {
            Context.Return.Write("{0} == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate({0})",
                  Context.Parameter.Name);
            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            if (Context.Function != null && pointer.IsPrimitiveTypeConvertibleToRef() &&
                Context.Kind != CSharpMarshalKind.VTableReturnValue)
            {
                var refParamPtr = string.Format("__refParamPtr{0}", Context.ParameterIndex);
                Context.SupportBefore.WriteLine("fixed ({0} {1} = &{2})",
                    pointer, refParamPtr, Context.Parameter.Name);
                Context.HasCodeBlock = true;
                Context.SupportBefore.WriteStartBraceIndent();
                Context.Return.Write(refParamPtr);
                return true;
            }

            var param = Context.Parameter;
            var isRefParam = param != null && (param.IsInOut || param.IsOut);

            var pointee = pointer.Pointee.Desugar();
            if (CSharpTypePrinter.IsConstCharString(pointee) && isRefParam)
            {
                if (param.IsOut)
                {
                    Context.Return.Write("IntPtr.Zero");
                    Context.ArgumentPrefix.Write("&");
                }
                else if (param.IsInOut)
                {
                    Context.Return.Write(MarshalStringToUnmanaged(Context.Parameter.Name));
                    Context.ArgumentPrefix.Write("&");
                }
                else
                {
                    Context.Return.Write(MarshalStringToUnmanaged(Context.Parameter.Name));
                    Context.Cleanup.WriteLine("Marshal.FreeHGlobal({0});", Context.ArgName);
                }
                return true;
            }

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return VisitDelegateType(function, function.ToString());
            }

            Class @class;
            if (pointee.TryGetClass(out @class) && @class.IsValueType)
            {
                if (Context.Parameter.Usage == ParameterUsage.Out)
                {
                    var qualifiedIdentifier = (@class.OriginalClass ?? @class).Visit(typePrinter);
                    Context.SupportBefore.WriteLine("var {0} = new {1}.{2}();",
                        Generator.GeneratedIdentifier(Context.ArgName), qualifiedIdentifier,
                        Helpers.InternalStruct);
                }
                else
                {
                    Context.SupportBefore.WriteLine("var {0} = {1}.{2};",
                            Generator.GeneratedIdentifier(Context.ArgName),
                            Context.Parameter.Name,
                            Helpers.InstanceIdentifier);
                }

                Context.Return.Write("new global::System.IntPtr(&{0})",
                    Generator.GeneratedIdentifier(Context.ArgName));
                return true;
            }

            var marshalAsString = CSharpTypePrinter.IsConstCharString(pointer);
            var finalPointee = pointer.GetFinalPointee();
            PrimitiveType primitive;
            if (finalPointee.IsPrimitiveType(out primitive) || finalPointee.IsEnumType() ||
                marshalAsString)
            {
                // From MSDN: "note that a ref or out parameter is classified as a moveable
                // variable". This means we must create a local variable to hold the result
                // and then assign this value to the parameter.

                if (isRefParam)
                {
                    var typeName = Type.TypePrinterDelegate(finalPointee);

                    if (param.IsInOut)
                        Context.SupportBefore.WriteLine("{0} _{1} = {1};", typeName, param.Name);
                    else
                        Context.SupportBefore.WriteLine("{0} _{1};", typeName, param.Name);

                    Context.Return.Write("&_{0}", param.Name);
                }
                else
                {
                    if (!marshalAsString &&
                        Context.Context.Options.MarshalCharAsManagedChar &&
                        primitive == PrimitiveType.Char)
                    {
                        typePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                        Context.Return.Write(string.Format("({0}) ", pointer.Visit(typePrinter)));
                        typePrinter.PopContext();
                    }
                    if (marshalAsString && (Context.Kind == CSharpMarshalKind.NativeField ||
                        Context.Kind == CSharpMarshalKind.VTableReturnValue))
                        Context.Return.Write(MarshalStringToUnmanaged(Context.Parameter.Name));
                    else
                        Context.Return.Write(Context.Parameter.Name);
                }

                return true;
            }

            return pointer.Pointee.Visit(this, quals);
        }

        private string MarshalStringToUnmanaged(string varName)
        {
            if (Equals(Context.Context.Options.Encoding, Encoding.ASCII))
            {
                return string.Format("Marshal.StringToHGlobalAnsi({0})", varName);
            }
            if (Equals(Context.Context.Options.Encoding, Encoding.Unicode) ||
                Equals(Context.Context.Options.Encoding, Encoding.BigEndianUnicode))
            {
                return string.Format("Marshal.StringToHGlobalUni({0})", varName);
            }
            throw new NotSupportedException(string.Format("{0} is not supported yet.",
                Context.Context.Options.Encoding.EncodingName));
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Char:
                    // returned structs must be blittable and char isn't
                    if (Context.Context.Options.MarshalCharAsManagedChar)
                    {
                        Context.Return.Write("global::System.Convert.ToSByte({0})",
                            Context.Parameter.Name);
                        return true;
                    }
                    goto default;
                case PrimitiveType.Char16:
                    return false;
                case PrimitiveType.Bool:
                    if (Context.Kind == CSharpMarshalKind.NativeField)
                    {
                        // returned structs must be blittable and bool isn't
                        Context.Return.Write("(byte) ({0} ? 1 : 0)", Context.Parameter.Name);
                        return true;
                    }
                    goto default;
                default:
                    Context.Return.Write(Context.Parameter.Name);
                    return true;
            }
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (!VisitType(typedef, quals))
                return false;

            var decl = typedef.Declaration;

            FunctionType func;
            if (decl.Type.IsPointerTo(out func))
            {
                VisitDelegateType(func, typedef.Declaration.OriginalName);
                return true;
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            Context.Return.Write(param.Parameter.Name);
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            if (@class.IsValueType)
            {
                MarshalValueClass();
            }
            else
            {
                MarshalRefClass(@class);
            }

            return true;
        }

        private void MarshalRefClass(Class @class)
        {
            var method = Context.Function as Method;
            if (method != null
                && method.Conversion == MethodConversionKind.FunctionToInstanceMethod
                && Context.ParameterIndex == 0)
            {
                Context.Return.Write("{0}", Helpers.InstanceIdentifier);
                return;
            }

            string param = Context.Parameter.Name;
            Type type = Context.Parameter.Type.Desugar();
            string paramInstance;
            Class @interface;
            if ((type.GetFinalPointee() ?? type).TryGetClass(out @interface) &&
                @interface.IsInterface)
                paramInstance = string.Format("{0}.__PointerTo{1}",
                    param, @interface.OriginalClass.Name);
            else
                paramInstance = string.Format("{0}.{1}", param, Helpers.InstanceIdentifier);
            if (type.IsAddress())
            {
                Class decl;
                if (type.TryGetClass(out decl) && decl.IsValueType)
                    Context.Return.Write(paramInstance);
                else
                {
                    if (type.IsPointer())
                    {
                        Context.Return.Write("{0}{1}",
                            method != null && method.OperatorKind == CXXOperatorKind.EqualEqual
                                ? string.Empty
                                : string.Format("ReferenceEquals({0}, null) ? global::System.IntPtr.Zero : ", param),
                            paramInstance);
                    }
                    else
                    {
                        if (method == null ||
                            // redundant for comparison operators, they are handled in a special way
                            (method.OperatorKind != CXXOperatorKind.EqualEqual &&
                            method.OperatorKind != CXXOperatorKind.ExclaimEqual))
                        {
                            Context.SupportBefore.WriteLine("if (ReferenceEquals({0}, null))", param);
                            Context.SupportBefore.WriteLineIndent(
                                "throw new global::System.ArgumentNullException(\"{0}\", " +
                                "\"Cannot be null because it is a C++ reference (&).\");",
                                param);
                        }
                        Context.Return.Write(paramInstance);
                    }
                }
                return;
            }

            var realClass = @class.OriginalClass ?? @class;
            var qualifiedIdentifier = realClass.Visit(this.typePrinter);
            Context.Return.Write(
                "ReferenceEquals({0}, null) ? new {1}.{2}{3}() : *({1}.{2}{3}*) {4}",
                param, qualifiedIdentifier, Helpers.InternalStruct,
                Helpers.GetSuffixForInternal(@class), paramInstance);
        }

        private void MarshalValueClass()
        {
            Context.Return.Write("{0}.{1}", Context.Parameter.Name, Helpers.InstanceIdentifier);
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            Context.Parameter = new Parameter
            {
                Name = Context.ArgName,
                QualifiedType = field.QualifiedType
            };

            return field.Type.Visit(this, field.QualifiedType.Qualifiers);
        }

        public override bool VisitProperty(Property property)
        {
            if (!VisitDeclaration(property))
                return false;

            Context.Parameter = new Parameter
            {
                Name = Context.ArgName,
                QualifiedType = property.QualifiedType
            };

            return base.VisitProperty(property);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write(Context.Parameter.Name);
            return true;
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return VisitClassDecl(template.TemplatedClass);
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
        }

        private readonly CSharpTypePrinter typePrinter;
    }

    public static class CSharpMarshalExtensions
    {
        public static void CSharpMarshalToNative(this QualifiedType type,
            CSharpMarshalManagedToNativePrinter printer)
        {
            printer.Context.FullType = type;
            type.Visit(printer);
        }

        public static void CSharpMarshalToNative(this Type type,
            CSharpMarshalManagedToNativePrinter printer)
        {
            CSharpMarshalToNative(new QualifiedType(type), printer);
        }

        public static void CSharpMarshalToNative(this ITypedDecl decl,
            CSharpMarshalManagedToNativePrinter printer)
        {
            CSharpMarshalToNative(decl.QualifiedType, printer);
        }

        public static void CSharpMarshalToManaged(this QualifiedType type,
            CSharpMarshalNativeToManagedPrinter printer)
        {
            printer.Context.FullType = type;
            type.Visit(printer);
        }

        public static void CSharpMarshalToManaged(this Type type,
            CSharpMarshalNativeToManagedPrinter printer)
        {
            CSharpMarshalToManaged(new QualifiedType(type), printer);
        }

        public static void CSharpMarshalToManaged(this ITypedDecl decl,
            CSharpMarshalNativeToManagedPrinter printer)
        {
            CSharpMarshalToManaged(decl.QualifiedType, printer);
        }
    }
}
