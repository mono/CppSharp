using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;

namespace CppSharp.Types.Std.CLI
{
    [TypeMap("const char*", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class ConstCharPointer : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override void MarshalToNative(MarshalContext ctx)
        {
            ctx.Before.WriteLine(
                "auto _{0} = clix::marshalString<clix::E_UTF8>({1});",
                ctx.ArgName, ctx.Parameter.Name);

            ctx.Return.Write("_{0}.c_str()", ctx.ArgName);
        }

        public override void MarshalToManaged(MarshalContext ctx)
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
                $@"({ctx.ReturnVarName} == 0 ? nullptr : clix::marshalString<clix::{param}>({ctx.ReturnVarName}))");
        }
    }

    [TypeMap("const char[]", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class ConstCharArray : ConstCharPointer
    {
    }

    [TypeMap("const wchar_t*", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class ConstWCharTPointer : ConstCharPointer
    {
    }

    [TypeMap("const char16_t*", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class ConstChar16TPointer : ConstCharPointer
    {
    }

    [TypeMap("const char32_t*", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class ConstChar32TPointer : ConstCharPointer
    {
    }

    [TypeMap("basic_string<char, char_traits<char>, allocator<char>>", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class String : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override void MarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0})",
                ctx.Parameter.Name);
        }

        public override void MarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0})",
                ctx.ReturnVarName);
        }
    }

    [TypeMap("std::wstring", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class WString : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override void MarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF16>({0})",
                ctx.Parameter.Name);
        }

        public override void MarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF16>({0})",
                ctx.ReturnVarName);
        }
    }

    [TypeMap("std::vector", GeneratorKindID = GeneratorKind.CLI_ID)]
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
                    var injectedClassNameType = (InjectedClassNameType)finalType;
                    type = (TemplateSpecializationType)injectedClassNameType.InjectedSpecializationType.Type;
                }
                var checker = new TypeIgnoreChecker(TypeMapDatabase);
                type.Arguments[0].Type.Visit(checker);

                return checker.IsIgnored;
            }
        }

        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CustomType(
                $"::System::Collections::Generic::List<{ctx.GetTemplateParameterList()}>^");
        }

        public override void MarshalToNative(MarshalContext ctx)
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

        public override void MarshalToManaged(MarshalContext ctx)
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
                "auto {0} = gcnew ::System::Collections::Generic::List<{1}>();",
                tmpVarName, managedType);

            string retVarName = "__list" + ctx.ParameterIndex;
            ctx.Before.WriteLine($@"auto {retVarName} = {(
                ctx.ReturnType.Type.Desugar().IsPointer() ? $"*{ctx.ReturnVarName}" : ctx.ReturnVarName)};");
            ctx.Before.WriteLine($"for(auto _element : {retVarName})");
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
    }

    [TypeMap("std::map", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class Map : TypeMap
    {
        public override bool IsIgnored { get { return true; } }

        public override Type SignatureType(TypePrinterContext ctx)
        {
            var type = Type as TemplateSpecializationType;
            return new CustomType(
                $@"::System::Collections::Generic::Dictionary<{type.Arguments[0].Type}, {type.Arguments[1].Type}>^");
        }

        public override void MarshalToNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override void MarshalToManaged(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }

    [TypeMap("std::list", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class List : TypeMap
    {
        public override bool IsIgnored { get { return true; } }
    }

    [TypeMap("std::shared_ptr", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class SharedPtr : TypeMap
    {
        public override bool IsIgnored { get { return true; } }
    }

    [TypeMap("basic_ostream<char, char_traits<char>>", GeneratorKind.CLI_ID)]
    public class OStream : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.IO.TextWriter));
        }

        public override void MarshalToNative(MarshalContext ctx)
        {
            var marshal = (CLIMarshalManagedToNativePrinter)ctx.MarshalToNative;
            if (!ctx.Parameter.Type.Desugar().IsPointer())
                marshal.ArgumentPrefix.Write("*");
            var marshalCtxName = string.Format("ctx_{0}", ctx.Parameter.Name);
            ctx.Before.WriteLine("msclr::interop::marshal_context {0};", marshalCtxName);
            ctx.Return.Write("{0}.marshal_as<std::ostream*>({1})",
                marshalCtxName, ctx.Parameter.Name);
        }
    }

    [TypeMap("std::nullptr_t", GeneratorKindID = GeneratorKind.CLI_ID)]
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
}
