using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;

namespace CppSharp.Types.Std
{
    [TypeMap("int", GeneratorKind = GeneratorKind.CSharp)]
    public partial class Int : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetSignedType(Context.TargetInfo.IntWidth);
    }

    [TypeMap("unsigned int", GeneratorKind = GeneratorKind.CSharp)]
    public partial class UnsignedInt : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetUnsignedType(Context.TargetInfo.IntWidth);
    }

    [TypeMap("long", GeneratorKind = GeneratorKind.CSharp)]
    public partial class Long : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetSignedType(Context.TargetInfo.LongWidth);
    }

    [TypeMap("unsigned long", GeneratorKind = GeneratorKind.CSharp)]
    public partial class UnsignedLong : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetUnsignedType(Context.TargetInfo.LongWidth);
    }

    [TypeMap("char", GeneratorKind = GeneratorKind.CSharp)]
    public partial class Char : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(ctx.Kind == TypePrinterContextKind.Native ||
                !Context.Options.MarshalCharAsManagedChar ? typeof(sbyte) : typeof(char));
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            if (Context.Options.MarshalCharAsManagedChar)
                ctx.Return.Write("global::System.Convert.ToSByte({0})",
               ctx.Parameter.Name);
            else
                ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            if (Context.Options.MarshalCharAsManagedChar)
                ctx.Return.Write("global::System.Convert.ToChar({0})",
                                   ctx.ReturnVarName);
            else
                ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("char16_t", GeneratorKind = GeneratorKind.CSharp)]
    public partial class Char16T : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(char));
        }
    }

    [TypeMap("wchar_t", GeneratorKind = GeneratorKind.CSharp)]
    public partial class WCharT : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(char));
        }
    }

    [TypeMap("const char*", GeneratorKind = GeneratorKind.CSharp)]
    public partial class ConstCharPointer : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Managed)
                return new CILType(typeof(string));

            if (ctx.Parameter == null || ctx.Parameter.Name == Helpers.ReturnIdentifier)
            {
                var typePrinter = new CSharpTypePrinter(Context);
                return new CustomType(typePrinter.IntPtrType);
            }

            var (enconding, _) = GetEncoding();

            if (enconding == Encoding.ASCII)
                return new CustomType("[MarshalAs(UnmanagedType.LPStr)] string");
            else if (enconding == Encoding.UTF8)
                return new CustomType("[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF8Marshaller))] string");
            else if (enconding == Encoding.Unicode || enconding == Encoding.BigEndianUnicode)
                return new CustomType("[MarshalAs(UnmanagedType.LPWStr)] string");
            else if (enconding == Encoding.UTF32)
                return new CustomType("[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF32Marshaller))] string");

            throw new System.NotSupportedException(
                $"{Context.Options.Encoding.EncodingName} is not supported yet.");
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            string param = ctx.Parameter.Name;
            if (ctx.Parameter.Usage == ParameterUsage.Unknown &&
                !ctx.Parameter.Type.IsReference() &&
                !(ctx.Parameter.Type is TemplateParameterSubstitutionType) &&
                ctx.MarshalKind != MarshalKind.NativeField &&
                ctx.MarshalKind != MarshalKind.VTableReturnValue &&
                ctx.MarshalKind != MarshalKind.Variable)
            {
                ctx.Return.Write(param);
                return;
            }

            var substitution = Type as TemplateParameterSubstitutionType;
            if (substitution != null)
                param = $"({substitution.Replacement}) (object) {param}";

            string bytes = $"__bytes{ctx.ParameterIndex}";
            string bytePtr = $"__bytePtr{ctx.ParameterIndex}";
            ctx.Before.WriteLine($@"byte[] {bytes} = global::System.Text.Encoding.{
                GetEncoding().Name}.GetBytes({param});");
            ctx.Before.WriteLine($"fixed (byte* {bytePtr} = {bytes})");
            ctx.HasCodeBlock = true;
            ctx.Before.WriteOpenBraceAndIndent();
            ctx.Return.Write($"new global::System.IntPtr({bytePtr})");
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            if (ctx.Parameter != null && !ctx.Parameter.IsOut &&
                !ctx.Parameter.IsInOut)
            {
                ctx.Return.Write(ctx.Parameter.Name);
                return;
            }

            string returnVarName = ctx.ReturnVarName;
            if (ctx.Function != null)
            {
                Type returnType = ctx.Function.ReturnType.Type.Desugar();
                if (returnType.IsAddress() &&
                    returnType.GetPointee().Desugar().IsAddress())
                {
                    returnVarName = $"new global::System.IntPtr(*{returnVarName})";
                }
            }

            var encoding = $"global::System.Text.Encoding.{GetEncoding().Name}";
            ctx.Return.Write($@"CppSharp.Runtime.MarshalUtil.GetString({encoding}, {returnVarName})");
        }

        private (Encoding Encoding, string Name) GetEncoding()
        {
            switch (GetCharWidth())
            {
                case 8:
                    if (Context.Options.Encoding == Encoding.ASCII)
                        return (Context.Options.Encoding, nameof(Encoding.ASCII));
                    if (Context.Options.Encoding == Encoding.UTF8)
                        return (Context.Options.Encoding, nameof(Encoding.UTF8));
                    if (Context.Options.Encoding == Encoding.UTF7)
                        return (Context.Options.Encoding, nameof(Encoding.UTF7));
                    if (Context.Options.Encoding == Encoding.BigEndianUnicode)
                        return (Context.Options.Encoding, nameof(Encoding.BigEndianUnicode));
                    if (Context.Options.Encoding == Encoding.Unicode)
                        return (Context.Options.Encoding, nameof(Encoding.Unicode));
                    if (Context.Options.Encoding == Encoding.UTF32)
                        return (Context.Options.Encoding, nameof(Encoding.UTF32));
                    break;
                case 16:
                    return (Encoding.Unicode, nameof(Encoding.Unicode));
                case 32:
                    return (Encoding.UTF32, nameof(Encoding.UTF32));
            }

            throw new System.NotSupportedException(
                $"{Context.Options.Encoding.EncodingName} is not supported yet.");
        }

        public uint GetCharWidth()
        {
            Type type = Type.Desugar();

            if (type is PointerType pointerType)
                return GetCharPtrWidth(pointerType);

            if (type.GetPointee()?.Desugar() is PointerType pointeePointerType)
                return GetCharPtrWidth(pointeePointerType);

            return 0;
        }

        public uint GetCharPtrWidth(PointerType pointer)
        {
            var pointee = pointer?.Pointee?.Desugar();
            if (pointee != null && pointee.IsPrimitiveType(out var primitiveType))
            {
                switch (primitiveType)
                {
                    case PrimitiveType.Char:
                        return Context.TargetInfo.CharWidth;
                    case PrimitiveType.WideChar:
                        return Context.TargetInfo.WCharWidth;
                    case PrimitiveType.Char16:
                        return Context.TargetInfo.Char16Width;
                    case PrimitiveType.Char32:
                        return Context.TargetInfo.Char32Width;
                }
            }

            return 0;
        }
    }

    [TypeMap("const char[]", GeneratorKind = GeneratorKind.CSharp)]
    public partial class ConstCharArray : ConstCharPointer
    {
    }

    [TypeMap("const wchar_t*", GeneratorKind = GeneratorKind.CSharp)]
    public partial class ConstWCharTPointer : ConstCharPointer
    {
    }

    [TypeMap("const char16_t*", GeneratorKind = GeneratorKind.CSharp)]
    public partial class ConstChar16TPointer : ConstCharPointer
    {
    }

    [TypeMap("const char32_t*", GeneratorKind = GeneratorKind.CSharp)]
    public partial class ConstChar32TPointer : ConstCharPointer
    {
    }

    [TypeMap("basic_string<char, char_traits<char>, allocator<char>>", GeneratorKind = GeneratorKind.CSharp)]
    public partial class String : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Managed)
                return new CILType(typeof(string));
            var typePrinter = new CSharpTypePrinter(null);
            typePrinter.PushContext(TypePrinterContextKind.Native);
            if (ctx.Type.Desugar().IsAddress())
                return new CustomType(typePrinter.IntPtrType);
            ClassTemplateSpecialization basicString = GetBasicString(ctx.Type);
            return new CustomType(basicString.Visit(typePrinter).Type);
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            Type type = ctx.Parameter.Type.Desugar();
            ClassTemplateSpecialization basicString = GetBasicString(type);
            var typePrinter = new CSharpTypePrinter(ctx.Context);
            if (!ctx.Parameter.Type.Desugar().IsAddress() &&
                ctx.MarshalKind != MarshalKind.NativeField)
                ctx.Return.Write($"*({typePrinter.PrintNative(basicString)}*) ");
            string qualifiedBasicString = GetQualifiedBasicString(basicString);
            var assign = basicString.Methods.First(m => m.OriginalName == "assign");
            if (ctx.MarshalKind == MarshalKind.NativeField)
            {
                ctx.Return.Write($@"{qualifiedBasicString}Extensions.{
                    Helpers.InternalStruct}.{assign.Name}(new {
                    typePrinter.IntPtrType}(&{
                    ctx.ReturnVarName}), {ctx.Parameter.Name})");
                ctx.ReturnVarName = string.Empty;
            }
            else
            {
                var varBasicString = $"__basicString{ctx.ParameterIndex}";
                ctx.Before.WriteLine($@"var {varBasicString} = new {
                    basicString.Visit(typePrinter)}();");
                ctx.Before.WriteLine($@"{qualifiedBasicString}Extensions.{
                    assign.Name}({varBasicString}, {ctx.Parameter.Name});");
                ctx.Return.Write($"{varBasicString}.{Helpers.InstanceIdentifier}");
                ctx.Cleanup.WriteLine($@"{varBasicString}.Dispose({
                    (!Type.IsAddress() || ctx.Parameter?.IsIndirect == true ? "false" : string.Empty)});");
            }
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            var type = Type.Desugar(resolveTemplateSubstitution: false);
            ClassTemplateSpecialization basicString = GetBasicString(type);
            var data = basicString.Methods.First(m => m.OriginalName == "data");
            var typePrinter = new CSharpTypePrinter(ctx.Context);
            string qualifiedBasicString = GetQualifiedBasicString(basicString);
            string varBasicString = $"__basicStringRet{ctx.ParameterIndex}";
            bool usePointer = type.IsAddress() || ctx.MarshalKind == MarshalKind.NativeField  ||
                ctx.MarshalKind == MarshalKind.ReturnVariableArray;
            ctx.Before.WriteLine($@"var {varBasicString} = {
                basicString.Visit(typePrinter)}.{Helpers.CreateInstanceIdentifier}({
                (usePointer ? string.Empty : $"new {typePrinter.IntPtrType}(&")}{
                 ctx.ReturnVarName}{(usePointer ? string.Empty : ")")});");
            string @string = $"{qualifiedBasicString}Extensions.{data.Name}({varBasicString})";
            if (usePointer)
            {
                ctx.Return.Write(@string);
            }
            else
            {
                string retString = $"{Generator.GeneratedIdentifier("retString")}{ctx.ParameterIndex}";
                ctx.Before.WriteLine($"var {retString} = {@string};");
                ctx.Before.WriteLine($"{varBasicString}.Dispose();");
                ctx.Return.Write(retString);
            }
        }

        private static string GetQualifiedBasicString(ClassTemplateSpecialization basicString)
        {
            var declContext = basicString.TemplatedDecl.TemplatedDecl;
            var names = new Stack<string>();
            while (!(declContext is TranslationUnit))
            {
                var isInlineNamespace = declContext is Namespace && ((Namespace)declContext).IsInline;
                if (!isInlineNamespace)
                    names.Push(declContext.Name);
                declContext = declContext.Namespace;
            }
            var qualifiedBasicString = string.Join(".", names);
            return $"global::{qualifiedBasicString}";
        }

        private static ClassTemplateSpecialization GetBasicString(Type type)
        {
            var desugared = type.Desugar();
            var template = (desugared.GetFinalPointee() ?? desugared).Desugar();
            var templateSpecializationType = template as TemplateSpecializationType;
            if (templateSpecializationType != null)
                return templateSpecializationType.GetClassTemplateSpecialization();
            return (ClassTemplateSpecialization) ((TagType) template).Declaration;
        }
    }

    [TypeMap("FILE", GeneratorKind = GeneratorKind.CSharp)]
    public partial class FILE : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.IntPtr));
        }
    }
}
