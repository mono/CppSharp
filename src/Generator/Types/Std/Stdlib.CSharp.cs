using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using Type = CppSharp.AST.Type;

namespace CppSharp.Types.Std.CSharp
{
    [TypeMap("int", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class Int : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetSignedType(Context.TargetInfo.IntWidth);
    }

    [TypeMap("unsigned int", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class UnsignedInt : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetUnsignedType(Context.TargetInfo.IntWidth);
    }

    [TypeMap("long", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class Long : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetSignedType(Context.TargetInfo.LongWidth);
    }

    [TypeMap("unsigned long", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class UnsignedLong : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetUnsignedType(Context.TargetInfo.LongWidth);
    }

    [TypeMap("char", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class Char : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(ctx.Kind == TypePrinterContextKind.Native ||
                !Context.Options.MarshalCharAsManagedChar ? typeof(sbyte) : typeof(char));
        }

        public override void MarshalToNative(MarshalContext ctx)
        {
            if (Context.Options.MarshalCharAsManagedChar)
                ctx.Return.Write("global::System.Convert.ToSByte({0})",
               ctx.Parameter.Name);
            else
                ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void MarshalToManaged(MarshalContext ctx)
        {
            if (Context.Options.MarshalCharAsManagedChar)
                ctx.Return.Write("global::System.Convert.ToChar({0})",
                                   ctx.ReturnVarName);
            else
                ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("char16_t", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class Char16T : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(char));
        }
    }

    [TypeMap("wchar_t", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class WCharT : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(char));
        }
    }

    [TypeMap("const char*", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class ConstCharPointer : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Managed)
                return new CILType(typeof(string));

            if (ctx.Parameter == null || ctx.Parameter.Name == Helpers.ReturnIdentifier)
            {
                var typePrinter = new CSharpTypePrinter(Context);
                return new CustomType(typePrinter.IntPtrType);
            }

            var (encoding, _) = GetEncoding();

            if (encoding == Encoding.ASCII || encoding == Encoding.Default)
                // This is not really right. ASCII is 7-bit only - the 8th bit is stripped; ANSI has
                // multi-byte support via a code page. MarshalAs(UnmanagedType.LPStr) marshals as ANSI.
                // Perhaps we need a CppSharp.Runtime.ASCIIMarshaller?
                return new CustomType("[MarshalAs(UnmanagedType.LPStr)] string");
            else if (encoding == Encoding.UTF8)
                return new CustomType("[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF8Marshaller))] string");
            else if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
                return new CustomType("[MarshalAs(UnmanagedType.LPWStr)] string");
            else if (encoding == Encoding.UTF32)
                return new CustomType("[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF32Marshaller))] string");

            throw new System.NotSupportedException(
                $"{Context.Options.Encoding.EncodingName} is not supported yet.");
        }

        public override void MarshalToNative(MarshalContext ctx)
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

            // Allow setting native field to null via setter property.
            if (ctx.MarshalKind == MarshalKind.NativeField)
            {
                // Free memory if we're holding a pointer to unmanaged memory that we (think we)
                // allocated. We can't simply compare with IntPtr.Zero since the reference could be
                // owned by the native side.

                // TODO: Surely, we can do better than stripping out the name of the field using
                // string manipulation on the ReturnVarName, but I don't see it yet. Seems like it
                // would be really helpful to have ctx hold a Decl property representing the
                // "appropriate" Decl when we get here. When MarshalKind == NativeField, Decl would
                // be set to the Field we're operating on.
                var fieldName = ctx.ReturnVarName[Math.Max(ctx.ReturnVarName.LastIndexOf('.') + 1, ctx.ReturnVarName.LastIndexOf("->") + 2)..];

                ctx.Before.WriteLine($"if (__{fieldName}_OwnsNativeMemory)");
                ctx.Before.WriteLineIndent($"Marshal.FreeHGlobal({ctx.ReturnVarName});");
                ctx.Before.WriteLine($"__{fieldName}_OwnsNativeMemory = true;");
                ctx.Before.WriteLine($"if ({param} == null)");
                ctx.Before.WriteOpenBraceAndIndent();
                ctx.Before.WriteLine($"{ctx.ReturnVarName} = global::System.IntPtr.Zero;");
                ctx.Before.WriteLine("return;");
                ctx.Before.UnindentAndWriteCloseBrace();
            }

            var bytes = $"__bytes{ctx.ParameterIndex}";
            var bytePtr = $"__bytePtr{ctx.ParameterIndex}";
            var encodingName = GetEncoding().Name;

            switch (encodingName)
            {
                case nameof(Encoding.Unicode):
                    ctx.Before.WriteLine($@"var {bytePtr} = Marshal.StringToHGlobalUni({param});");
                    break;
                case nameof(Encoding.Default):
                    ctx.Before.WriteLine($@"var {bytePtr} = Marshal.StringToHGlobalAnsi({param});");
                    break;
                default:
                    {
                        var encodingBytesPerChar = GetCharWidth() / 8;
                        var writeNulMethod = encodingBytesPerChar switch
                        {
                            1 => nameof(Marshal.WriteByte),
                            2 => nameof(Marshal.WriteInt16),
                            4 => nameof(Marshal.WriteInt32),
                            _ => throw new System.NotImplementedException(
                                    $"Encoding bytes per char: {encodingBytesPerChar} is not implemented.")
                        };

                        ctx.Before.WriteLine($@"var {bytes} = global::System.Text.Encoding.{encodingName}.GetBytes({param});");
                        ctx.Before.WriteLine($@"var {bytePtr} = Marshal.AllocHGlobal({bytes}.Length + {encodingBytesPerChar});");
                        ctx.Before.WriteLine($"Marshal.Copy({bytes}, 0, {bytePtr}, {bytes}.Length);");
                        ctx.Before.WriteLine($"Marshal.{writeNulMethod}({bytePtr} + {bytes}.Length, 0);");
                    }
                    break;
            }

            ctx.Return.Write($"{bytePtr}");
        }

        public override void MarshalToManaged(MarshalContext ctx)
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
                    if (Context.Options.Encoding == Encoding.Default)   // aka ANSI with system default code page
                        return (Context.Options.Encoding, nameof(Encoding.Default));
                    if (Context.Options.Encoding == Encoding.ASCII)
                        return (Context.Options.Encoding, nameof(Encoding.ASCII));
                    if (Context.Options.Encoding == Encoding.UTF8)
                        return (Context.Options.Encoding, nameof(Encoding.UTF8));
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

    [TypeMap("const char[]", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class ConstCharArray : ConstCharPointer
    {
    }

    [TypeMap("const wchar_t*", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class ConstWCharTPointer : ConstCharPointer
    {
    }

    [TypeMap("const char16_t*", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class ConstChar16TPointer : ConstCharPointer
    {
    }

    [TypeMap("const char32_t*", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class ConstChar32TPointer : ConstCharPointer
    {
    }

    [TypeMap("basic_string<char, char_traits<char>, allocator<char>>", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class String : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
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

        public override void MarshalToNative(MarshalContext ctx)
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
                string var;
                if (ctx.ReturnVarName.LastIndexOf('.') > ctx.ReturnVarName.LastIndexOf("->"))
                {
                    var = Generator.GeneratedIdentifier(ctx.ArgName);
                    ctx.Before.WriteLine($"fixed (void* {var} = &{ctx.ReturnVarName})");
                    ctx.Before.WriteOpenBraceAndIndent();
                    (ctx as CSharpMarshalContext).HasCodeBlock = true;
                }
                else
                {
                    var = $"&{ctx.ReturnVarName}";
                }
                ctx.Return.Write($@"{qualifiedBasicString}Extensions.{Helpers.InternalStruct}.{assign.Name}(new {typePrinter.IntPtrType}({var}), ");
                if (ctx.Parameter.Type.IsTemplateParameterType())
                    ctx.Return.Write("(string) (object) ");
                ctx.Return.Write($"{ctx.Parameter.Name})");
                ctx.ReturnVarName = string.Empty;
            }
            else
            {
                var varBasicString = $"__basicString{ctx.ParameterIndex}";
                ctx.Before.WriteLine($@"var {varBasicString} = new {basicString.Visit(typePrinter)}();");

                ctx.Before.Write($@"{qualifiedBasicString}Extensions.{assign.Name}({varBasicString}, ");
                if (ctx.Parameter.Type.IsTemplateParameterType())
                    ctx.Before.Write("(string) (object) ");
                ctx.Before.WriteLine($"{ctx.Parameter.Name});");

                ctx.Return.Write($"{varBasicString}.{Helpers.InstanceIdentifier}");
                ctx.Cleanup.WriteLine($@"{varBasicString}.Dispose({(!Type.IsAddress() || ctx.Parameter?.IsIndirect == true ? "disposing: true, callNativeDtor:false" : string.Empty)});");
            }
        }

        public override void MarshalToManaged(MarshalContext ctx)
        {
            var type = Type.Desugar(resolveTemplateSubstitution: false);
            ClassTemplateSpecialization basicString = GetBasicString(type);
            var data = basicString.Methods.First(m => m.OriginalName == "data");
            var typePrinter = new CSharpTypePrinter(ctx.Context);
            string qualifiedBasicString = GetQualifiedBasicString(basicString);
            string varBasicString = $"__basicStringRet{ctx.ParameterIndex}";
            bool usePointer = type.IsAddress() || ctx.MarshalKind == MarshalKind.NativeField ||
                ctx.MarshalKind == MarshalKind.ReturnVariableArray;
            ctx.Before.WriteLine($@"var {varBasicString} = {basicString.Visit(typePrinter)}.{Helpers.CreateInstanceIdentifier}({(usePointer ? string.Empty : $"new {typePrinter.IntPtrType}(&")}{ctx.ReturnVarName}{(usePointer ? string.Empty : ")")});");
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
            return (ClassTemplateSpecialization)((TagType)template).Declaration;
        }
    }

    [TypeMap("FILE", GeneratorKindID = GeneratorKind.CSharp_ID)]
    public class FILE : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.IntPtr));
        }
    }
}
