using System;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public class CSharpMarshalContext : MarshalContext
    {
        public CSharpMarshalContext(BindingContext context, uint indentation)
            : base(context, indentation)
        {
            ArgumentPrefix = new TextGenerator { CurrentIndentation = indentation };
            Cleanup = new TextGenerator { CurrentIndentation = indentation };
        }

        public TextGenerator ArgumentPrefix { get; }
        public TextGenerator Cleanup { get; }
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

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.CSharpMarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitDeclaration(Declaration decl) => true;

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    if (Context.MarshalKind != MarshalKind.NativeField &&
                        Context.MarshalKind != MarshalKind.ReturnVariableArray)
                        goto case ArrayType.ArraySize.Incomplete;

                    var supportBefore = Context.Before;
                    string value = Generator.GeneratedIdentifier("value");
                    var arrayType = array.Type.Desugar();
                    supportBefore.WriteLine($"{arrayType}[] {value} = null;");
                    supportBefore.WriteLine($"if ({Context.ReturnVarName} != null)");
                    supportBefore.WriteOpenBraceAndIndent();
                    supportBefore.WriteLine($"{value} = new {arrayType}[{array.Size}];");
                    supportBefore.WriteLine($"for (int i = 0; i < {array.Size}; i++)");
                    if (array.Type.IsPointerToPrimitiveType(PrimitiveType.Void))
                        supportBefore.WriteLineIndent($@"{value}[i] = new global::System.IntPtr({
                            Context.ReturnVarName}[i]);");
                    else
                    {
                        var finalArrayType = arrayType.GetPointee() ?? arrayType;
                        Class @class;
                        if ((finalArrayType.TryGetClass(out @class)) && @class.IsRefType)
                        {
                            if (arrayType == finalArrayType)
                                supportBefore.WriteLineIndent(
                                    "{0}[i] = {1}.{2}(*(({1}.{3}*)&({4}[i * sizeof({1}.{3})])));",
                                    value, array.Type, Helpers.CreateInstanceIdentifier,
                                    Helpers.InternalStruct, Context.ReturnVarName);
                            else
                                supportBefore.WriteLineIndent(
                                    $@"{value}[i] = {finalArrayType}.{Helpers.CreateInstanceIdentifier}(({
                                        typePrinter.IntPtrType}) {Context.ReturnVarName}[i]);");
                        }
                        else
                        {
                            if (arrayType.IsPrimitiveType(PrimitiveType.Bool))
                                supportBefore.WriteLineIndent($@"{value}[i] = {
                                    Context.ReturnVarName}[i] != 0;");
                            else if (arrayType.IsPrimitiveType(PrimitiveType.Char) &&
                                Context.Context.Options.MarshalCharAsManagedChar)
                                supportBefore.WriteLineIndent($@"{value}[i] = global::System.Convert.ToChar({
                                    Context.ReturnVarName}[i]);");
                            else
                                supportBefore.WriteLineIndent($@"{value}[i] = {
                                    Context.ReturnVarName}[i];");
                        }
                    }
                    supportBefore.UnindentAndWriteCloseBrace();
                    Context.Return.Write(value);
                    break;
                case ArrayType.ArraySize.Incomplete:
                    // const char* and const char[] are the same so we can use a string
                    if (array.Type.Desugar().IsPrimitiveType(PrimitiveType.Char) &&
                        array.QualifiedType.Qualifiers.IsConst)
                    {
                        var pointer = new PointerType { QualifiedPointee = array.QualifiedType };
                        Context.ReturnType = new QualifiedType(pointer);
                        return this.VisitPointerType(pointer, quals);
                    }
                    MarshalArray(array);
                    break;
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
            var finalPointee = pointer.GetFinalPointee().Desugar();
            PrimitiveType primitive;
            if ((pointee.IsConstCharString() && isRefParam) ||
                (!finalPointee.IsPrimitiveType(out primitive) &&
                 !finalPointee.IsEnumType()))
                return pointer.QualifiedPointee.Visit(this);

            if (isRefParam)
            {
                Context.Return.Write(Generator.GeneratedIdentifier(param.Name));
                return true;
            }

            if (Context.Context.Options.MarshalCharAsManagedChar &&
                primitive == PrimitiveType.Char)
                Context.Return.Write($"({pointer}) ");

            var type = Context.ReturnType.Type.Desugar(
                resolveTemplateSubstitution: false);
            if (Context.Function != null &&
                Context.Function.OperatorKind == CXXOperatorKind.Subscript)
            {
                if (type.IsPrimitiveType(primitive))
                {
                    Context.Return.Write("*");
                }
                else
                {
                    var substitution = pointer.Pointee.Desugar(
                        resolveTemplateSubstitution: false) as TemplateParameterSubstitutionType;
                    if (substitution != null)
                        Context.Return.Write($@"({
                            substitution.ReplacedParameter.Parameter.Name}) (object) *");
                }
            }

            if (new QualifiedType(pointer, quals).IsConstRefToPrimitive())
                Context.Return.Write("*");

            Context.Return.Write(Context.ReturnVarName);
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                    if (Context.MarshalKind == MarshalKind.NativeField)
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

            var functionType = decl.Type as FunctionType;
            if (functionType != null || decl.Type.IsPointerTo(out functionType))
            {
                var ptrName = $"{Generator.GeneratedIdentifier("ptr")}{Context.ParameterIndex}";

                Context.Before.WriteLine($"var {ptrName} = {Context.ReturnVarName};");

                var specialization = decl.Namespace as ClassTemplateSpecialization;
                Type returnType = Context.ReturnType.Type.Desugar();
                var finalType = (returnType.GetFinalPointee() ?? returnType).Desugar();
                var res = string.Format(
                    "{0} == IntPtr.Zero? null : {1}({2}) Marshal.GetDelegateForFunctionPointer({0}, typeof({2}))",
                    ptrName,
                    finalType.IsDependent ? $@"({specialization.TemplatedDecl.TemplatedClass.Typedefs.First(
                        t => t.Name == decl.Name).Visit(this.typePrinter)}) (object) " : string.Empty,
                    typedef);
                Context.Return.Write(res);
                return true;
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var ptrName = Generator.GeneratedIdentifier("ptr") + Context.ParameterIndex;

            Context.Before.WriteLine("var {0} = {1};", ptrName,
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

            var finalType = (returnType.GetFinalPointee() ?? returnType).Desugar();
            Class returnedClass;
            if (finalType.TryGetClass(out returnedClass) && returnedClass.IsDependent)
                Context.Return.Write($"({returnType.Visit(typePrinter)}) (object) ");

            if (returnType.IsAddress())
                Context.Return.Write(HandleReturnedPointer(@class, qualifiedClass.Type));
            else
                Context.Return.Write($"{qualifiedClass}.{Helpers.CreateInstanceIdentifier}({Context.ReturnVarName})");

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

            var ctx = new CSharpMarshalContext(Context.Context, Context.Indentation)
            {
                ReturnType = Context.ReturnType,
                ReturnVarName = Context.ReturnVarName
            };

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            parameter.Type.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(ctx.Before))
                Context.Before.WriteLine(ctx.Before);

            if (!string.IsNullOrWhiteSpace(ctx.Return) &&
                !parameter.Type.IsPrimitiveTypeConvertibleToRef())
            {
                Context.Before.WriteLine("var _{0} = {1};", parameter.Name,
                    ctx.Return);
            }

            Context.Return.Write("{0}{1}",
                parameter.Type.IsPrimitiveTypeConvertibleToRef() ? "ref *" : "_", parameter.Name);
            return true;
        }

        public override bool VisitTemplateParameterSubstitutionType(TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            Type returnType = Context.ReturnType.Type.Desugar();
            Type finalType = (returnType.GetFinalPointee() ?? returnType).Desugar();
            if (finalType.IsDependent)
                Context.Return.Write($"({param.ReplacedParameter.Parameter.Name}) (object) ");
            if (param.Replacement.Type.Desugar().IsPointerToPrimitiveType())
                Context.Return.Write($"({typePrinter.IntPtrType}) ");
            return base.VisitTemplateParameterSubstitutionType(param, quals);
        }

        private string HandleReturnedPointer(Class @class, string qualifiedClass)
        {
            var originalClass = @class.OriginalClass ?? @class;
            var ret = Generator.GeneratedIdentifier("result") + Context.ParameterIndex;
            var qualifiedIdentifier = @class.Visit(typePrinter);
            Context.Before.WriteLine("{0} {1};", qualifiedIdentifier, ret);
            Context.Before.WriteLine("if ({0} == IntPtr.Zero) {1} = {2};", Context.ReturnVarName, ret,
                originalClass.IsRefType ? "null" : string.Format("new {0}()", qualifiedClass));
            if (originalClass.IsRefType)
            {
                Context.Before.WriteLine(
                    "else if ({0}.NativeToManagedMap.ContainsKey({1}))", qualifiedClass, Context.ReturnVarName);
                Context.Before.WriteLineIndent("{0} = ({1}) {2}.NativeToManagedMap[{3}];",
                    ret, qualifiedIdentifier, qualifiedClass, Context.ReturnVarName);
                var dtor = originalClass.Destructors.FirstOrDefault();
                if (dtor != null && dtor.IsVirtual)
                {
                    Context.Before.WriteLine("else {0}{1} = ({2}) {3}.{4}({5}{6});",
                        Context.Parameter != null
                            ? string.Empty
                            : string.Format("{0}.NativeToManagedMap[{1}] = ", qualifiedClass, Context.ReturnVarName),
                        ret, qualifiedIdentifier, qualifiedClass, Helpers.CreateInstanceIdentifier, Context.ReturnVarName,
                        Context.Parameter != null ? ", skipVTables: true" : string.Empty);
                }
                else
                {
                    Context.Before.WriteLine("else {0} = {1}.{2}({3});", ret, qualifiedClass,
                        Helpers.CreateInstanceIdentifier, Context.ReturnVarName);
                }
            }
            else
            {
                Context.Before.WriteLine("else {0} = {1}.{2}({3});", ret, qualifiedClass,
                    Helpers.CreateInstanceIdentifier, Context.ReturnVarName);
            }
            return ret;
        }

        private void MarshalArray(ArrayType array)
        {
            Type arrayType = array.Type.Desugar();
            if (arrayType.IsPrimitiveType() ||
                arrayType.IsPointerToPrimitiveType() ||
                Context.MarshalKind != MarshalKind.GenericDelegate)
            {
                Context.Return.Write(Context.ReturnVarName);
                return;
            }

            var intermediateArray = Generator.GeneratedIdentifier(Context.ReturnVarName);
            var intermediateArrayType = arrayType.Visit(typePrinter);

            Context.Before.WriteLine($"{intermediateArrayType}[] {intermediateArray};");

            Context.Before.WriteLine($"if (ReferenceEquals({Context.ReturnVarName}, null))");
            Context.Before.WriteLineIndent($"{intermediateArray} = null;");
            Context.Before.WriteLine("else");

            Context.Before.WriteOpenBraceAndIndent();
            Context.Before.WriteLine($@"{intermediateArray} = new {
                intermediateArrayType}[{Context.ReturnVarName}.Length];");
            Context.Before.WriteLine($"for (int i = 0; i < {intermediateArray}.Length; i++)");

            if (arrayType.IsAddress())
            {
                Context.Before.WriteOpenBraceAndIndent();
                string element = Generator.GeneratedIdentifier("element");
                Context.Before.WriteLine($"var {element} = {Context.ReturnVarName}[i];");
                var intPtrZero = $"{typePrinter.IntPtrType}.Zero";
                Context.Before.WriteLine($@"{intermediateArray}[i] = {element} == {
                    intPtrZero} ? null : {intermediateArrayType}.{
                    Helpers.CreateInstanceIdentifier}({element});");
                Context.Before.UnindentAndWriteCloseBrace();
            }
            else
                Context.Before.WriteLineIndent($@"{intermediateArray}[i] = {
                    intermediateArrayType}.{Helpers.CreateInstanceIdentifier}({
                    Context.ReturnVarName}[i]);");

            Context.Before.UnindentAndWriteCloseBrace();

            Context.Return.Write(intermediateArray);
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
                typeMap.CSharpMarshalToNative(Context);
                return false;
            }

            return true;
        }

        public override bool VisitDeclaration(Declaration decl) => true;

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    if (string.IsNullOrEmpty(Context.ReturnVarName))
                        goto case ArrayType.ArraySize.Incomplete;

                    var supportBefore = Context.Before;
                    supportBefore.WriteLine($"if ({Context.ArgName} != null)");
                    supportBefore.WriteOpenBraceAndIndent();
                    Class @class;
                    var arrayType = array.Type.Desugar();
                    var finalArrayType = arrayType.GetPointee() ?? arrayType;
                    if (finalArrayType.TryGetClass(out @class) && @class.IsRefType)
                    {
                        supportBefore.WriteLine($"if (value.Length != {array.Size})");
                        ThrowArgumentOutOfRangeException();
                    }
                    supportBefore.WriteLine($"for (int i = 0; i < {array.Size}; i++)");
                    if (@class != null && @class.IsRefType)
                    {
                        if (finalArrayType == arrayType)
                            supportBefore.WriteLineIndent(
                                "*({1}.{2}*) &{0}[i * sizeof({1}.{2})] = *({1}.{2}*){3}[i].{4};",
                                Context.ReturnVarName, arrayType, Helpers.InternalStruct,
                                Context.ArgName, Helpers.InstanceIdentifier);
                        else
                            supportBefore.WriteLineIndent($@"{Context.ReturnVarName}[i] = ({
                                (Context.Context.TargetInfo.PointerWidth == 64 ? "long" : "int")}) {
                                Context.ArgName}[i].{Helpers.InstanceIdentifier};");
                    }
                    else
                    {
                        if (arrayType.IsPrimitiveType(PrimitiveType.Bool))
                            supportBefore.WriteLineIndent($@"{
                                Context.ReturnVarName}[i] = (byte)({
                                Context.ArgName}[i] ? 1 : 0);");
                        else if (arrayType.IsPrimitiveType(PrimitiveType.Char) &&
                            Context.Context.Options.MarshalCharAsManagedChar)
                            supportBefore.WriteLineIndent($@"{
                                Context.ReturnVarName}[i] = global::System.Convert.ToSByte({
                                Context.ArgName}[i]);");
                        else
                            supportBefore.WriteLineIndent($@"{Context.ReturnVarName}[i] = {
                                Context.ArgName}[i]{
                                (arrayType.IsPointerToPrimitiveType(PrimitiveType.Void) ?
                                    ".ToPointer()" : string.Empty)};");
                    }
                    supportBefore.UnindentAndWriteCloseBrace();
                    break;
                case ArrayType.ArraySize.Incomplete:
                    MarshalArray(array);
                    break;
                default:
                    Context.Return.Write("null");
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
            if (pointee.IsConstCharString() && isRefParam)
            {
                if (param.IsOut)
                {
                    MarshalString(pointee);
                    Context.Return.Write("IntPtr.Zero");
                    Context.ArgumentPrefix.Write("&");
                    return true;
                }
                if (param.IsInOut)
                {
                    MarshalString(pointee);
                    pointer.QualifiedPointee.Visit(this);
                    Context.ArgumentPrefix.Write("&");
                }
                else
                {
                    pointer.QualifiedPointee.Visit(this);
                    Context.Cleanup.WriteLine($"Marshal.FreeHGlobal({Context.ArgName});");
                }
                return true;
            }

            var finalPointee = (pointee.GetFinalPointee() ?? pointee).Desugar();
            if (finalPointee.IsPrimitiveType(out PrimitiveType primitive) ||
                finalPointee.IsEnumType())
            {
                if (isRefParam)
                {
                    var local = Generator.GeneratedIdentifier($@"{
                        param.Name}{Context.ParameterIndex}");
                    Context.Before.WriteLine($@"fixed ({
                        pointer.Visit(typePrinter)} {local} = &{param.Name})");
                    Context.HasCodeBlock = true;
                    Context.Before.WriteOpenBraceAndIndent();
                    Context.Return.Write(local);
                    return true;
                }

                if (Context.Context.Options.MarshalCharAsManagedChar &&
                    primitive == PrimitiveType.Char)
                {
                    Context.Return.Write($"({typePrinter.PrintNative(pointer)}) ");
                    Context.Return.Write(param.Name);
                    return true;
                }

                pointer.QualifiedPointee.Visit(this);
                bool isVoid = primitive == PrimitiveType.Void &&
                    pointee.IsAddress() && pointer.IsReference();
                if (pointer.Pointee.Desugar(false) is TemplateParameterSubstitutionType ||
                    isVoid)
                {
                    var local = Generator.GeneratedIdentifier($@"{
                        param.Name}{Context.ParameterIndex}");
                    string cast = isVoid ? $@"({pointee.Visit(
                        new CppTypePrinter { PrintTypeQualifiers = false })}) " : string.Empty;
                    Context.Before.WriteLine($"var {local} = {cast}{Context.Return};");
                    Context.Return.StringBuilder.Clear();
                    Context.Return.Write(local);
                }
                if (new QualifiedType(pointer, quals).IsConstRefToPrimitive())
                    Context.Return.StringBuilder.Insert(0, '&');

                return true;
            }

            if (pointee.TryGetClass(out Class @class) && @class.IsValueType)
            {
                if (Context.Parameter.Usage == ParameterUsage.Out)
                {
                    var qualifiedIdentifier = (@class.OriginalClass ?? @class).Visit(typePrinter);
                    Context.Before.WriteLine("var {0} = new {1}.{2}();",
                        Generator.GeneratedIdentifier(Context.ArgName), qualifiedIdentifier,
                        Helpers.InternalStruct);
                }
                else
                {
                    Context.Before.WriteLine("var {0} = {1}.{2};",
                            Generator.GeneratedIdentifier(Context.ArgName),
                            Context.Parameter.Name,
                            Helpers.InstanceIdentifier);
                }

                Context.Return.Write("new global::System.IntPtr(&{0})",
                    Generator.GeneratedIdentifier(Context.ArgName));
                return true;
            }

            return pointer.QualifiedPointee.Visit(this);
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool:
                    if (Context.MarshalKind == MarshalKind.NativeField)
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

            return typedef.Declaration.Type.Visit(this);
        }

        public override bool VisitTemplateParameterSubstitutionType(TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            var replacement = param.Replacement.Type.Desugar();
            if (replacement.IsPrimitiveType() ||
                replacement.IsPointerToPrimitiveType() ||
                replacement.IsEnum())
            {
                Context.Return.Write($"({replacement}) ");
                if (replacement.IsPointerToPrimitiveType())
                    Context.Return.Write($"({typePrinter.IntPtrType}) ");
                Context.Return.Write("(object) ");
            }
            return base.VisitTemplateParameterSubstitutionType(param, quals);
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
            Type type = Context.Parameter.Type.Desugar(resolveTemplateSubstitution: false);
            string paramInstance;
            Class @interface;
            var finalType = type.GetFinalPointee() ?? type;
            var templateType = finalType as TemplateParameterSubstitutionType;
            type = Context.Parameter.Type.Desugar();
            if (templateType != null)
                param = $"(({@class.Visit(typePrinter)}) (object) {param})";
            if (finalType.TryGetClass(out @interface) &&
                @interface.IsInterface)
                paramInstance = $"{param}.__PointerTo{@interface.OriginalClass.Name}";
            else
                paramInstance = $@"{param}.{Helpers.InstanceIdentifier}";
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
                                : $"ReferenceEquals({param}, null) ? global::System.IntPtr.Zero : ",
                            paramInstance);
                    }
                    else
                    {
                        if (method == null ||
                            // redundant for comparison operators, they are handled in a special way
                            (method.OperatorKind != CXXOperatorKind.EqualEqual &&
                            method.OperatorKind != CXXOperatorKind.ExclaimEqual))
                        {
                            Context.Before.WriteLine("if (ReferenceEquals({0}, null))", param);
                            Context.Before.WriteLineIndent(
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
            var qualifiedIdentifier = typePrinter.PrintNative(realClass);
            Context.Return.Write(
                "ReferenceEquals({0}, null) ? new {1}() : *({1}*) {2}",
                param, qualifiedIdentifier, paramInstance);
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

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            Context.Return.Write("{0} == null ? global::System.IntPtr.Zero : Marshal.GetFunctionPointerForDelegate({0})",
                  Context.Parameter.Name);
            return true;
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
        }

        private void ThrowArgumentOutOfRangeException()
        {
            Context.Before.WriteLineIndent(
                "throw new ArgumentOutOfRangeException(\"{0}\", " +
                "\"The dimensions of the provided array don't match the required size.\");",
                Context.Parameter.Name);
        }

        private void MarshalArray(ArrayType arrayType)
        {
            if (arrayType.SizeType == ArrayType.ArraySize.Constant)
            {
                Context.Before.WriteLine("if ({0} == null || {0}.Length != {1})",
                      Context.Parameter.Name, arrayType.Size);
                ThrowArgumentOutOfRangeException();
            }

            var elementType = arrayType.Type.Desugar();

            if (elementType.IsPrimitiveType() ||
                elementType.IsPointerToPrimitiveType())
            {
                Context.Return.Write(Context.Parameter.Name);
                return;
            }

            var intermediateArray = Generator.GeneratedIdentifier(Context.Parameter.Name);
            var intermediateArrayType = typePrinter.PrintNative(elementType);

            Context.Before.WriteLine($"{intermediateArrayType}[] {intermediateArray};");

            Context.Before.WriteLine($"if (ReferenceEquals({Context.Parameter.Name}, null))");
                Context.Before.WriteLineIndent($"{intermediateArray} = null;");
            Context.Before.WriteLine("else");

            Context.Before.WriteOpenBraceAndIndent();
            Context.Before.WriteLine($@"{intermediateArray} = new {
                intermediateArrayType}[{Context.Parameter.Name}.Length];");
            Context.Before.WriteLine($"for (int i = 0; i < {intermediateArray}.Length; i++)");

            Context.Before.WriteOpenBraceAndIndent();
            string element = Generator.GeneratedIdentifier("element");
            Context.Before.WriteLine($"var {element} = {Context.Parameter.Name}[i];");
            if (elementType.IsAddress())
            {
                var intPtrZero = $"{typePrinter.IntPtrType}.Zero";
                Context.Before.WriteLine($@"{intermediateArray}[i] = ReferenceEquals({
                    element}, null) ? {intPtrZero} : {element}.{Helpers.InstanceIdentifier};");
            }
            else
                Context.Before.WriteLine($@"{intermediateArray}[i] = ReferenceEquals({
                    element}, null) ? new {intermediateArrayType}() : *({
                    intermediateArrayType}*) {element}.{Helpers.InstanceIdentifier};");
            Context.Before.UnindentAndWriteCloseBrace();

            Context.Before.UnindentAndWriteCloseBrace();

            Context.Return.Write(intermediateArray);
        }

        private void MarshalString(Type pointee)
        {
            var marshal = new CSharpMarshalNativeToManagedPrinter(Context);
            Context.ReturnVarName = Context.ArgName;
            Context.ReturnType = Context.Parameter.QualifiedType;
            pointee.Visit(marshal);
            Context.Cleanup.WriteLine($@"{Context.Parameter.Name} = {
                marshal.Context.Return};");
            Context.Return.StringBuilder.Clear();
        }

        private readonly CSharpTypePrinter typePrinter;
    }
}
