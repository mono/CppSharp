using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;

namespace CppSharp.Types.Std
{
    [TypeMap("va_list")]
    public class VaList : TypeMap
    {
        public override bool IsIgnored => true;
    }

    [TypeMap("int", GeneratorKind = GeneratorKind.CSharp)]
    public class Int : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetSignedType(Context.TargetInfo.IntWidth);
    }

    [TypeMap("unsigned int", GeneratorKind = GeneratorKind.CSharp)]
    public class UnsignedInt : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetUnsignedType(Context.TargetInfo.IntWidth);
    }

    [TypeMap("long", GeneratorKind = GeneratorKind.CSharp)]
    public class Long : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetSignedType(Context.TargetInfo.LongWidth);
    }

    [TypeMap("unsigned long", GeneratorKind = GeneratorKind.CSharp)]
    public class UnsignedLong : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx) =>
            CSharpTypePrinter.GetUnsignedType(Context.TargetInfo.LongWidth);
    }

    [TypeMap("char", GeneratorKind = GeneratorKind.CSharp)]
    public class Char : TypeMap
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
    public class Char16T : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(char));
        }
    }

    [TypeMap("wchar_t", GeneratorKind = GeneratorKind.CSharp)]
    public class WCharT : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(char));
        }
    }

    [TypeMap("const char*", GeneratorKind = GeneratorKind.CSharp)]
    [TypeMap("const char*", GeneratorKind = GeneratorKind.CLI)]
    public class ConstCharPointer : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Before.WriteLine(
                "auto _{0} = clix::marshalString<clix::E_UTF8>({1});",
                ctx.ArgName, ctx.Parameter.Name);

            ctx.Return.Write("_{0}.c_str()", ctx.ArgName);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            if (ctx.Parameter != null && !ctx.Parameter.IsOut &&
                !ctx.Parameter.IsInOut)
            {
                ctx.Return.Write(ctx.Parameter.Name);
                return;
            }

            Type type = ctx.ReturnType.Type.Desugar();
            Type pointee = type.GetPointee().Desugar();
            var isChar = type.IsPointerToPrimitiveType(PrimitiveType.Char) ||
                (pointee.IsPointerToPrimitiveType(PrimitiveType.Char) &&
                 ctx.Parameter != null &&
                 (ctx.Parameter.IsInOut || ctx.Parameter.IsOut));
            var encoding = isChar ? Encoding.ASCII : Encoding.Unicode;

            if (Equals(encoding, Encoding.ASCII))
                encoding = Context.Options.Encoding;

            string param;
            if (Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
                param = "E_UTF8";
            else if (Equals(encoding, Encoding.Unicode) ||
                     Equals(encoding, Encoding.BigEndianUnicode))
                param = "E_UTF16";
            else
                throw new System.NotSupportedException(
                    $"{Context.Options.Encoding.EncodingName} is not supported yet.");

            ctx.Return.Write(
                $@"({ctx.ReturnVarName} == 0 ? nullptr : clix::marshalString<clix::{
                    param}>({ctx.ReturnVarName}))");
        }

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
                return new CustomType("[MarshalAs(UnmanagedType.LPUTF8Str)] string");
            else if (enconding == Encoding.Unicode || enconding == Encoding.BigEndianUnicode)
                return new CustomType("[MarshalAs(UnmanagedType.LPWStr)] string");
            else if (enconding == Encoding.UTF32)
                return new CustomType("[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF32Marshaller))] string");

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
    }

    [TypeMap("const char[]", GeneratorKind = GeneratorKind.CSharp)]
    [TypeMap("const char[]", GeneratorKind = GeneratorKind.CLI)]
    public class ConstCharArray : ConstCharPointer
    {
    }

    [TypeMap("const wchar_t*", GeneratorKind = GeneratorKind.CSharp)]
    [TypeMap("const wchar_t*", GeneratorKind = GeneratorKind.CLI)]
    public class ConstWCharTPointer : ConstCharPointer
    {
    }

    [TypeMap("const char16_t*", GeneratorKind = GeneratorKind.CSharp)]
    [TypeMap("const char16_t*", GeneratorKind = GeneratorKind.CLI)]
    public class ConstChar16TPointer : ConstCharPointer
    {
    }

    [TypeMap("const char32_t*", GeneratorKind = GeneratorKind.CSharp)]
    [TypeMap("const char32_t*", GeneratorKind = GeneratorKind.CLI)]
    public class ConstChar32TPointer : ConstCharPointer
    {
    }

    [TypeMap("basic_string<char, char_traits<char>, allocator<char>>", GeneratorKind = GeneratorKind.CSharp)]
    [TypeMap("basic_string<char, char_traits<char>, allocator<char>>", GeneratorKind = GeneratorKind.CLI)]
    public class String : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0})",
                ctx.Parameter.Name);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0})",
                ctx.ReturnVarName);
        }

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

    [TypeMap("std::wstring", GeneratorKind = GeneratorKind.CLI)]
    public class WString : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF16>({0})",
                ctx.Parameter.Name);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF16>({0})",
                ctx.ReturnVarName);
        }

        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write("new Std.WString()");
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("std::vector", GeneratorKind = GeneratorKind.CLI)]
    public class Vector : TypeMap
    {
        public override bool IsIgnored
        {
            get
            {
                var finalType = Type.GetFinalPointee() ?? Type;
                var type = finalType as TemplateSpecializationType;
                if (type == null)
                {
                    var injectedClassNameType = (InjectedClassNameType) finalType;
                    type = (TemplateSpecializationType) injectedClassNameType.InjectedSpecializationType.Type;
                }
                var checker = new TypeIgnoreChecker(TypeMapDatabase);
                type.Arguments[0].Type.Visit(checker);

                return checker.IsIgnored;
            }
        }

        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CustomType(
                $"System::Collections::Generic::List<{ctx.GetTemplateParameterList()}>^");
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            var desugared = Type.Desugar();
            var templateType = desugared as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;
            var isPointerToPrimitive = type.Type.IsPointerToPrimitiveType();
            var managedType = isPointerToPrimitive
                ? new CILType(typeof(System.IntPtr))
                : type.Type;

            var entryString = (ctx.Parameter != null) ? ctx.Parameter.Name
                : ctx.ArgName;

            var tmpVarName = "_tmp" + entryString;

            var cppTypePrinter = new CppTypePrinter(Context);
            var nativeType = type.Type.Visit(cppTypePrinter);

            ctx.Before.WriteLine("auto {0} = std::vector<{1}>();",
                tmpVarName, nativeType);
            ctx.Before.WriteLine("for each({0} _element in {1})",
                managedType, entryString);
            ctx.Before.WriteOpenBraceAndIndent();
            {
                var param = new Parameter
                {
                    Name = "_element",
                    QualifiedType = type
                };

                var elementCtx = new MarshalContext(ctx.Context, ctx.Indentation)
                                     {
                                         Parameter = param,
                                         ArgName = param.Name,
                                     };

                var marshal = new CLIMarshalManagedToNativePrinter(elementCtx);
                type.Type.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    ctx.Before.Write(marshal.Context.Before);

                if (isPointerToPrimitive)
                    ctx.Before.WriteLine("auto _marshalElement = {0}.ToPointer();",
                        marshal.Context.Return);
                else
                    ctx.Before.WriteLine("auto _marshalElement = {0};",
                    marshal.Context.Return);

                ctx.Before.WriteLine("{0}.push_back(_marshalElement);",
                    tmpVarName);
            }
            
            ctx.Before.UnindentAndWriteCloseBrace();

            ctx.Return.Write(tmpVarName);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            var desugared = Type.Desugar();
            var templateType = desugared as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;
            var isPointerToPrimitive = type.Type.IsPointerToPrimitiveType();
            var managedType = isPointerToPrimitive
                ? new CILType(typeof(System.IntPtr))
                : type.Type;
            var tmpVarName = "_tmp" + ctx.ArgName;
            
            ctx.Before.WriteLine(
                "auto {0} = gcnew System::Collections::Generic::List<{1}>();",
                tmpVarName, managedType);

            string retVarName = ctx.ReturnType.Type.Desugar().IsPointer() ? $"*{ctx.ReturnVarName}" : ctx.ReturnVarName;
            ctx.Before.WriteLine("for(auto _element : {0})",
                retVarName);
            ctx.Before.WriteOpenBraceAndIndent();
            {
                var elementCtx = new MarshalContext(ctx.Context, ctx.Indentation)
                                     {
                                         ReturnVarName = "_element",
                                         ReturnType = type
                                     };

                var marshal = new CLIMarshalNativeToManagedPrinter(elementCtx);
                type.Type.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    ctx.Before.Write(marshal.Context.Before);

                ctx.Before.WriteLine("auto _marshalElement = {0};",
                    marshal.Context.Return);

                if (isPointerToPrimitive)
                    ctx.Before.WriteLine("{0}->Add({1}(_marshalElement));",
                        tmpVarName, managedType);
                else
                    ctx.Before.WriteLine("{0}->Add(_marshalElement);",
                        tmpVarName);
            }
            ctx.Before.UnindentAndWriteCloseBrace();

            ctx.Return.Write(tmpVarName);
        }

        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Native)
                return new CustomType("Std.Vector");

            return new CustomType($"Std.Vector<{ctx.GetTemplateParameterList()}>");
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write("{0}.Internal", ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;

            ctx.Return.Write("new Std.Vector<{0}>({1})", type,
                ctx.ReturnVarName);
        }
    }

    [TypeMap("std::map", GeneratorKind = GeneratorKind.CLI)]
    public class Map : TypeMap
    {
        public override bool IsIgnored { get { return true; } }

        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            var type = Type as TemplateSpecializationType;
            return new CustomType(
                $@"System::Collections::Generic::Dictionary<{
                    type.Arguments[0].Type}, {type.Arguments[1].Type}>^");
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Native)
                return new CustomType("Std.Map");

            var type = Type as TemplateSpecializationType;
            return new CustomType(
                $@"System.Collections.Generic.Dictionary<{
                    type.Arguments[0].Type}, {type.Arguments[1].Type}>");
        }
    }

    [TypeMap("std::list", GeneratorKind = GeneratorKind.CLI)]
    public class List : TypeMap
    {
        public override bool IsIgnored { get { return true; } }
    }

    [TypeMap("std::shared_ptr", GeneratorKind = GeneratorKind.CLI)]
    public class SharedPtr : TypeMap
    {
        public override bool IsIgnored { get { return true; } }
    }

    [TypeMap("basic_ostream<char, char_traits<char>>", GeneratorKind.CLI)]
    public class OStream : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.IO.TextWriter));
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            var marshal = (CLIMarshalManagedToNativePrinter) ctx.MarshalToNative;
            if (!ctx.Parameter.Type.Desugar().IsPointer())
                marshal.ArgumentPrefix.Write("*");
            var marshalCtxName = string.Format("ctx_{0}", ctx.Parameter.Name);
            ctx.Before.WriteLine("msclr::interop::marshal_context {0};", marshalCtxName);
            ctx.Return.Write("{0}.marshal_as<std::ostream*>({1})",
                marshalCtxName, ctx.Parameter.Name);
        }
    }

    [TypeMap("std::nullptr_t", GeneratorKind = GeneratorKind.CLI)]
    public class NullPtr : TypeMap
    {
        public override bool DoesMarshalling { get { return false; } }

        public override void CLITypeReference(CLITypeReferenceCollector collector,
            ASTRecord<Declaration> loc)
        {
            var typeRef = collector.GetTypeReference(loc.Value);

            if (typeRef != null)
            {
                var include = new CInclude
                {
                    File = "cstddef",
                    Kind = CInclude.IncludeKind.Angled,
                };

                typeRef.Include = include;
            }
        }
    }

    [TypeMap("FILE", GeneratorKind = GeneratorKind.CSharp)]
    public class FILE : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.IntPtr));
        }
    }
}
