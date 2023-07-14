using System.Linq;
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
        }

        public bool HasCodeBlock { get; set; }
    }

    public abstract class CSharpMarshalPrinter : MarshalPrinter<CSharpMarshalContext, CSharpTypePrinter>
    {
        protected CSharpMarshalPrinter(CSharpMarshalContext context)
            : base(context)
        {
            VisitOptions.ClearFlags(VisitFlags.FunctionParameters |
                VisitFlags.TemplateArguments);
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

                    var arrayType = array.Type.Desugar();

                    if (CheckIfArrayCanBeCopiedUsingMemoryCopy(array))
                    {
                        if (Context.Context.Options.UseSpan)
                            Context.Return.Write($"new Span<{arrayType}>({Context.ReturnVarName}, {array.Size})");
                        else
                            Context.Return.Write($"CppSharp.Runtime.MarshalUtil.GetArray<{arrayType}>({Context.ReturnVarName}, {array.Size})");
                    }
                    else if (array.Type.IsPrimitiveType(PrimitiveType.Char) && Context.Context.Options.MarshalCharAsManagedChar)
                        Context.Return.Write($"CppSharp.Runtime.MarshalUtil.GetCharArray({Context.ReturnVarName}, {array.Size})");
                    else if (array.Type.IsPointerToPrimitiveType(PrimitiveType.Void))
                        if (Context.Context.Options.UseSpan)
                            Context.Return.Write($"Span<IntPtr>({Context.ReturnVarName}, {array.Size})");
                        else
                            Context.Return.Write($"CppSharp.Runtime.MarshalUtil.GetIntPtrArray({Context.ReturnVarName}, {array.Size})");
                    else
                    {
                        string value = Generator.GeneratedIdentifier("value");
                        var supportBefore = Context.Before;
                        supportBefore.WriteLine($"{arrayType}[] {value} = null;");
                        supportBefore.WriteLine($"if ({Context.ReturnVarName} != null)");
                        supportBefore.WriteOpenBraceAndIndent();
                        supportBefore.WriteLine($"{value} = new {arrayType}[{array.Size}];");

                        supportBefore.WriteLine($"for (int i = 0; i < {array.Size}; i++)");
                        var finalArrayType = arrayType.GetPointee() ?? arrayType;
                        Class @class;
                        if (finalArrayType.TryGetClass(out @class) && @class.IsRefType)
                        {
                            if (arrayType == finalArrayType)
                                supportBefore.WriteLineIndent(
                                    "{0}[i] = {1}.{2}((IntPtr)(({1}.{3}*)&({4}[i * sizeof({1}.{3})])), true, true);",
                                    value, array.Type, Helpers.GetOrCreateInstanceIdentifier,
                                    Helpers.InternalStruct, Context.ReturnVarName);
                            else
                                supportBefore.WriteLineIndent(
                                    $@"{value}[i] = {finalArrayType}.{Helpers.CreateInstanceIdentifier}(({
                                        typePrinter.IntPtrType}) {Context.ReturnVarName}[i]);");
                        }
                        else
                        {
                            supportBefore.WriteLineIndent($"{value}[i] = {Context.ReturnVarName}[i];");
                        }
                        supportBefore.UnindentAndWriteCloseBrace();
                        Context.Return.Write(value);
                    }
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
            var returnType = Context.ReturnType.Type.Desugar(
                resolveTemplateSubstitution: false);
            if ((pointee.IsConstCharString() && (isRefParam || returnType.IsReference())) ||
                (!finalPointee.IsPrimitiveType(out PrimitiveType primitive) &&
                 !finalPointee.IsEnumType()))
            {
                if (Context.MarshalKind != MarshalKind.NativeField &&
                    pointee.IsPointerTo(out Type type) &&
                    type.Desugar().TryGetClass(out Class c))
                {
                    string ret = Generator.GeneratedIdentifier(Context.ReturnVarName);
                    Context.Before.WriteLine($@"{typePrinter.IntPtrType} {ret} = {
                        Context.ReturnVarName} == {typePrinter.IntPtrType}.Zero ? {
                        typePrinter.IntPtrType}.Zero : new {
                        typePrinter.IntPtrType}(*(void**) {Context.ReturnVarName});");
                    Context.ReturnVarName = ret;
                }
                return pointer.QualifiedPointee.Visit(this);
            }

            if (isRefParam)
            {
                Context.Return.Write(Generator.GeneratedIdentifier(param.Name));
                return true;
            }

            if (Context.Context.Options.MarshalCharAsManagedChar &&
                primitive == PrimitiveType.Char)
                Context.Return.Write($"({pointer}) ");

            if (Context.Function != null &&
                Context.Function.OperatorKind == CXXOperatorKind.Subscript)
            {
                if (returnType.IsPrimitiveType(primitive))
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
            {
                Context.Return.Write("*");
                if (Context.MarshalKind == MarshalKind.NativeField)
                    Context.Return.Write($"({pointer.QualifiedPointee.Visit(typePrinter)}*) ");
            }

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
            if (!(typedef.Declaration.Type.Desugar(false) is TemplateParameterSubstitutionType) &&
                !VisitType(typedef, quals))
                return false;

            var decl = typedef.Declaration;

            Type type = decl.Type.Desugar();
            var functionType = type as FunctionType;
            if (functionType == null && !type.IsPointerTo(out functionType))
                return decl.Type.Visit(this);

            var ptrName = $@"{Generator.GeneratedIdentifier("ptr")}{
                Context.ParameterIndex}";

            Context.Before.WriteLine($"var {ptrName} = {Context.ReturnVarName};");

            var substitution = decl.Type.Desugar(false)
                as TemplateParameterSubstitutionType;

            if (substitution != null)
                Context.Return.Write($@"({
                    substitution.ReplacedParameter.Parameter.Name}) (object) (");

            Context.Return.Write($@"{ptrName} == IntPtr.Zero? null : {
                (substitution == null ? $"({Context.ReturnType}) " :
                 string.Empty)}Marshal.GetDelegateForFunctionPointer({
                ptrName}, typeof({typedef}))");

            if (substitution != null)
                Context.Return.Write(")");
            return true;
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

            // these two aren't the same for members of templates
            if (Context.Function?.OriginalReturnType.Type.Desugar().IsAddress() == true ||
                returnType.IsAddress())
                Context.Return.Write(HandleReturnedPointer(@class, qualifiedClass));
            else
            {
                if (Context.MarshalKind == MarshalKind.NativeField ||
                    Context.MarshalKind == MarshalKind.Variable ||
                    Context.MarshalKind == MarshalKind.ReturnVariableArray ||
                    !originalClass.HasNonTrivialDestructor)
                {
                    Context.Return.Write($"{qualifiedClass}.{Helpers.CreateInstanceIdentifier}({Context.ReturnVarName})");
                }
                else
                {
                    Context.Before.WriteLine($@"var __{Context.ReturnVarName} = {
                        qualifiedClass}.{Helpers.CreateInstanceIdentifier}({Context.ReturnVarName});");
                    Method dtor = originalClass.Destructors.First();
                    if (dtor.IsVirtual)
                    {
                        var i = VTables.GetVTableIndex(dtor);
                        int vtableIndex = 0;
                        if (Context.Context.ParserOptions.IsMicrosoftAbi)
                            vtableIndex = @class.Layout.VFTables.IndexOf(@class.Layout.VFTables.First(
                                v => v.Layout.Components.Any(c => c.Method == dtor)));
                        string instance = $"new {typePrinter.IntPtrType}(&{Context.ReturnVarName})";
                        Context.Before.WriteLine($@"var __vtables = new IntPtr[] {{ {
                            string.Join(", ", originalClass.Layout.VTablePointers.Select(
                                x => $"*({typePrinter.IntPtrType}*) ({instance} + {x.Offset})"))} }};");
                        Context.Before.WriteLine($@"var __slot = *({typePrinter.IntPtrType}*) (__vtables[{
                            vtableIndex}] + {i} * sizeof({typePrinter.IntPtrType}));");
                        Context.Before.Write($"Marshal.GetDelegateForFunctionPointer<{dtor.FunctionType}>(__slot)({instance}");
                        if (dtor.GatherInternalParams(Context.Context.ParserOptions.IsItaniumLikeAbi).Count > 1)
                        {
                            Context.Before.Write(", 0");
                        }
                        Context.Before.WriteLine(");");
                    }
                    else
                    {
                        string suffix = string.Empty;
                        var specialization = @class as ClassTemplateSpecialization;
                        if (specialization != null)
                            suffix = Helpers.GetSuffixFor(specialization);
                        Context.Before.WriteLine($@"{typePrinter.PrintNative(originalClass)}.dtor{
                            suffix}(new {typePrinter.IntPtrType}(&{Context.ReturnVarName}));");
                    }
                    Context.Return.Write($"__{Context.ReturnVarName}");
                }
            }

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
            Type replacement = param.Replacement.Type.Desugar();
            if (replacement.IsPointerToPrimitiveType() && !replacement.IsConstCharString())
                Context.Return.Write($"({typePrinter.IntPtrType}) ");
            return base.VisitTemplateParameterSubstitutionType(param, quals);
        }

        private string HandleReturnedPointer(Class @class, string qualifiedClass)
        {
            var originalClass = @class.OriginalClass ?? @class;
            var ret = Generator.GeneratedIdentifier("result") + Context.ParameterIndex;

            if (originalClass.IsRefType)
            {
                var dtor = originalClass.Destructors.FirstOrDefault();
                var dtorVirtual = (dtor != null && dtor.IsVirtual);
                var cache = dtorVirtual && Context.Parameter == null;
                var skipVTables = dtorVirtual && Context.Parameter != null;
                var get = Context.Context.Options.GenerateNativeToManagedFor(@class) ? "GetOr" : "";
                Context.Before.WriteLine("var {0} = {1}.__{5}CreateInstance({2}, {3}{4});",
                    ret, qualifiedClass, Context.ReturnVarName, cache ? "true" : "false", skipVTables ? ", skipVTables: true" : string.Empty, get);
            }
            else
            {
                Context.Before.WriteLine("var {0} = {1} != IntPtr.Zero ? {2}.{3}({4}) : default;",
                    ret, Context.ReturnVarName, qualifiedClass, Helpers.CreateInstanceIdentifier, Context.ReturnVarName);
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

            Context.Before.WriteLine($"if ({Context.ReturnVarName} is null)");
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

        public bool CheckIfArrayCanBeCopiedUsingMemoryCopy(ArrayType array)
        {
            return array.Type.IsPrimitiveType(out var primitive) &&
                (!Context.Context.Options.MarshalCharAsManagedChar || primitive != PrimitiveType.Char);
        }
    }

    public class CSharpMarshalManagedToNativePrinter : CSharpMarshalPrinter
    {
        public CSharpMarshalManagedToNativePrinter(CSharpMarshalContext context)
            : base(context)
        {
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
            if (pointee.IsConstCharString())
            {
                if (param.IsOut)
                {
                    MarshalString(pointee);
                    Context.Return.Write($"{typePrinter.IntPtrType}.Zero");
                    Context.ArgumentPrefix.Write("&");
                    return true;
                }
                if (param.IsInOut)
                {
                    MarshalString(pointee);
                    pointer.QualifiedPointee.Visit(this);
                    Context.ArgumentPrefix.Write("&");
                    return true;
                }
                if (pointer.IsReference)
                {
                    Context.Return.Write($@"({typePrinter.PrintNative(
                        pointee.GetQualifiedPointee())}*) ");
                    pointer.QualifiedPointee.Visit(this);
                    Context.ArgumentPrefix.Write("&");
                    return true;
                }
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

                bool isConst = quals.IsConst || pointer.QualifiedPointee.Qualifiers.IsConst ||
                     pointer.GetFinalQualifiedPointee().Qualifiers.IsConst;

                if (Context.Context.Options.MarshalCharAsManagedChar &&
                    primitive == PrimitiveType.Char)
                {
                    Context.Return.Write($"({typePrinter.PrintNative(pointer)})");
                    if (isConst)
                        Context.Return.Write("&");
                    Context.Return.Write(param.Name);
                    return true;
                }

                pointer.QualifiedPointee.Visit(this);

                if (Context.Parameter.IsIndirect)
                    Context.ArgumentPrefix.Write("&");
                bool isVoid = primitive == PrimitiveType.Void && pointer.IsReference() && isConst;
                if (pointer.Pointee.Desugar(false) is TemplateParameterSubstitutionType || isVoid)
                {
                    var local = Generator.GeneratedIdentifier($@"{
                        param.Name}{Context.ParameterIndex}");
                    Context.Before.WriteLine($"var {local} = {Context.Return};");
                    Context.Return.StringBuilder.Clear();
                    Context.Return.Write(local);
                }
                if (new QualifiedType(pointer, quals).IsConstRefToPrimitive())
                    Context.Return.StringBuilder.Insert(0, '&');

                return true;
            }


            string arg = Generator.GeneratedIdentifier(Context.ArgName);

            if (pointee.TryGetClass(out Class @class) && @class.IsValueType)
            {
                if (Context.Parameter.Usage == ParameterUsage.Out)
                {
                    var qualifiedIdentifier = (@class.OriginalClass ?? @class).Visit(typePrinter);
                    Context.Before.WriteLine("var {0} = new {1}.{2}();",
                        arg, qualifiedIdentifier, Helpers.InternalStruct);
                }
                else
                {
                    Context.Before.WriteLine("var {0} = {1}.{2};",
                        arg, Context.Parameter.Name, Helpers.InstanceIdentifier);
                }

                Context.Return.Write($"new {typePrinter.IntPtrType}(&{arg})");
                return true;
            }

            if (pointee.IsPointerTo(out Type type) &&
                type.Desugar().TryGetClass(out Class c))
            {
                pointer.QualifiedPointee.Visit(this);
                Context.Before.WriteLine($"var {arg} = {Context.Return};");
                Context.Return.StringBuilder.Clear();
                Context.Return.Write($"new {typePrinter.IntPtrType}(&{arg})");
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
            if ((replacement.IsPrimitiveType() ||
                 replacement.IsPointerToPrimitiveType() ||
                 replacement.IsEnum()) &&
                !replacement.IsConstCharString())
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
                paramInstance = $"{param}.{Helpers.InstanceIdentifier}";

            if (!type.IsAddress())
            {
                Context.Before.WriteLine($"if (ReferenceEquals({Context.Parameter.Name}, null))");
                Context.Before.WriteLineIndent(
                    $@"throw new global::System.ArgumentNullException(""{
                        Context.Parameter.Name}"", ""Cannot be null because it is passed by value."");");
                var realClass = @class.OriginalClass ?? @class;
                var qualifiedIdentifier = typePrinter.PrintNative(realClass);
                Context.ArgumentPrefix.Write($"*({qualifiedIdentifier}*) ");
                Context.Return.Write(paramInstance);
                return;
            }

            Class decl;
            if (type.TryGetClass(out decl) && decl.IsValueType)
            {
                Context.Return.Write(paramInstance);
                return;
            }

            if (type.IsPointer())
            {
                if (Context.Parameter.IsIndirect)
                {
                    Context.Before.WriteLine($"if (ReferenceEquals({Context.Parameter.Name}, null))");
                    Context.Before.WriteLineIndent(
                        $@"throw new global::System.ArgumentNullException(""{
                            Context.Parameter.Name}"", ""Cannot be null because it is passed by value."");");
                    Context.Return.Write(paramInstance);
                }
                else
                {
                    Context.Return.Write("{0}{1}",
                        method != null && method.OperatorKind == CXXOperatorKind.EqualEqual
                            ? string.Empty
                            : $"{param} is null ? {typePrinter.IntPtrType}.Zero : ",
                        paramInstance);
                }
                return;
            }

            if (method == null ||
                // redundant for comparison operators, they are handled in a special way
                (method.OperatorKind != CXXOperatorKind.EqualEqual &&
                method.OperatorKind != CXXOperatorKind.ExclaimEqual))
            {
                Context.Before.WriteLine($"if (ReferenceEquals({Context.Parameter.Name}, null))");
                Context.Before.WriteLineIndent(
                    $@"throw new global::System.ArgumentNullException(""{
                        Context.Parameter.Name}"", ""Cannot be null because it is a C++ reference (&)."");",
                    param);
            }

            Context.Return.Write(paramInstance);
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

            if ((elementType.IsPrimitiveType() &&
                    !(elementType.IsPrimitiveType(PrimitiveType.Char) && Context.Context.Options.MarshalCharAsManagedChar)) ||
                elementType.IsPointerToPrimitiveType())
            {
                if (Context.Context.Options.UseSpan && !elementType.IsConstCharString())
                {
                    var local = Generator.GeneratedIdentifier($@"{
                      Context.Parameter.Name}{Context.ParameterIndex}");
                    Context.Before.WriteLine($@"fixed ({
                        typePrinter.PrintNative(elementType)}* {local} = &MemoryMarshal.GetReference({Context.Parameter.Name}))");
                    Context.HasCodeBlock = true;
                    Context.Before.WriteOpenBraceAndIndent();
                    Context.Return.Write(local);
                }
                else
                    Context.Return.Write(Context.Parameter.Name);
                return;
            }

            var intermediateArray = Generator.GeneratedIdentifier(Context.Parameter.Name);
            var intermediateArrayType = typePrinter.PrintNative(elementType);
            if (Context.Context.Options.UseSpan)
                Context.Before.WriteLine($"Span<{intermediateArrayType}> {intermediateArray};");
            else
                Context.Before.WriteLine($"{intermediateArrayType}[] {intermediateArray};");
            Context.Before.WriteLine($"if ({Context.Parameter.Name} == null)");
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
                Context.Before.WriteLine($@"{intermediateArray}[i] = {
                    element} is null ? {intPtrZero} : {element}.{Helpers.InstanceIdentifier};");
            }
            else if (elementType.IsPrimitiveType(PrimitiveType.Char) &&
                        Context.Context.Options.MarshalCharAsManagedChar)
                Context.Before.WriteLine($@"{intermediateArray}[i] = global::System.Convert.ToSByte({
                    element});");
            else
                Context.Before.WriteLine($@"{intermediateArray}[i] = {
                    element} is null ? new {intermediateArrayType}() : *({
                    intermediateArrayType}*) {element}.{Helpers.InstanceIdentifier};");
            Context.Before.UnindentAndWriteCloseBrace();

            Context.Before.UnindentAndWriteCloseBrace();

            if (Context.Context.Options.UseSpan)
            {
                var local = Generator.GeneratedIdentifier($@"{
                  intermediateArray}{Context.ParameterIndex}");
                Context.Before.WriteLine($@"fixed ({
                    typePrinter.PrintNative(elementType)}* {local} = &MemoryMarshal.GetReference({intermediateArray}))");
                Context.HasCodeBlock = true;
                Context.Before.WriteOpenBraceAndIndent();
                Context.Return.Write(local);
            }
            else
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
    }
}
