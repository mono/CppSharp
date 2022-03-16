using System.Linq;
using System.Runtime.InteropServices;
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
                var fieldName = ctx.ReturnVarName.Substring(ctx.ReturnVarName.LastIndexOf("->") + 2);

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
            return GetEncoding(Context, GetCharWidth());
        }

        public static (Encoding Encoding, string Name) GetEncoding(BindingContext Context, uint charWidth)
        {
            switch (charWidth)
            {
                case 8:
                    if (Context.Options.Encoding == Encoding.Default)   // aka ANSI with system default code page
                        return (Context.Options.Encoding, nameof(Encoding.Default));
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
            ClassTemplateSpecialization basicString = ctx.Type.GetClassTemplateSpecialization();
            return new CustomType(basicString.Visit(typePrinter).Type);
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            Type type = ctx.Parameter.Type.Desugar();
            ClassTemplateSpecialization basicString = type.GetClassTemplateSpecialization();
            var typePrinter = new CSharpTypePrinter(ctx.Context);
            if (!ctx.Parameter.Type.Desugar().IsAddress() &&
                ctx.MarshalKind != MarshalKind.NativeField)
                ctx.Return.Write($"*({typePrinter.PrintNative(basicString)}*) ");
            string qualifiedBasicString = basicString.GetQualifiedName();
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
                    (!Type.IsAddress() || ctx.Parameter?.IsIndirect == true ? "disposing: true, callNativeDtor:false" : string.Empty)});");
            }
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            var type = Type.Desugar(resolveTemplateSubstitution: false);
            ClassTemplateSpecialization basicString = type.GetClassTemplateSpecialization();
            var data = basicString.Methods.First(m => m.OriginalName == "data");
            var typePrinter = new CSharpTypePrinter(ctx.Context);
            string qualifiedBasicString = basicString.GetQualifiedName();
            string varBasicString = $"__basicStringRet{ctx.ParameterIndex}";
            bool usePointer = type.IsAddress() || ctx.MarshalKind == MarshalKind.NativeField ||
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
    }

    [TypeMap("FILE", GeneratorKind = GeneratorKind.CSharp)]
    public partial class FILE : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.IntPtr));
        }
    }


    [TypeMap("basic_string_view<char, char_traits<char>>", GeneratorKind = GeneratorKind.CSharp)]
    public class StringView : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Managed)
                return new CILType(typeof(string));
            var specialization = ctx.Type.GetClassTemplateSpecialization();
            var typePrinter = new CSharpTypePrinter(null);
            typePrinter.PushContext(TypePrinterContextKind.Native);
            return new CustomType(specialization.Visit(typePrinter).Type);
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            if (ctx.MarshalKind == MarshalKind.NativeField)
            {
                ctx.Before.WriteLine($"throw new {typeof(System.NotImplementedException).FullName}();");
                ctx.Return.Write("default");
                return;
            }

            var type = ctx.Parameter.Type.Desugar();
            var name = ctx.Parameter.Name;
            var specialization = type.GetClassTemplateSpecialization();
            var typePrinter = new CSharpTypePrinter(ctx.Context);
            var bytes = Generator.GeneratedIdentifier($"{name}Bytes");
            var bytesPtr = Generator.GeneratedIdentifier($"{name}BytesPtr");
            var view = Generator.GeneratedIdentifier($"{name}StringView");
            var viewPtr = $"new IntPtr(&{view})";
            var encoding = ConstCharPointer.GetEncoding(Context, Context.TargetInfo.CharWidth);
            var extensionClass = $"{specialization.GetQualifiedName()}Extensions";
            ctx.HasCodeBlock = true;
            ctx.Before.WriteLine($"var {view} = new {specialization.Visit(typePrinter)}();");
            ctx.Before.WriteLine($"var {bytes} = {name} != null ? global::{typeof(Encoding).FullName}.{encoding.Name}.GetBytes({name}) : null;");
            ctx.Before.WriteLine($"fixed (byte* {bytesPtr} = {bytes})");
            ctx.Before.WriteOpenBraceAndIndent();
            ctx.Before.WriteLine($"{extensionClass}.{Helpers.InternalStruct}.{specialization.Name}({viewPtr}, new {typePrinter.IntPtrType}({bytesPtr}), (uint)({bytes}?.Length ?? 0));");
            ctx.Return.Write(type.IsAddress() || ctx.Parameter.IsIndirect ? viewPtr : $"{view}.{Helpers.InstanceIdentifier}");
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            var type = Type.Desugar(resolveTemplateSubstitution: false);
            var specialization = type.GetClassTemplateSpecialization();
            var data = specialization.Methods.First(m => m.OriginalName == "data");
            var typePrinter = new CSharpTypePrinter(ctx.Context);
            var qualifiedBasicString = specialization.GetQualifiedName();
            var view = Generator.GeneratedIdentifier($"{ctx.ReturnVarName}StringView");
            var returnvarNameOrPtr = type.IsAddress() ? $"new {typePrinter.IntPtrType}(&{ctx.ReturnVarName})" : ctx.ReturnVarName;
            ctx.Return.Write($"{qualifiedBasicString}Extensions.{data.Name}({specialization.Visit(typePrinter)}.{Helpers.CreateInstanceIdentifier}({returnvarNameOrPtr}))");
        }
    }
}
