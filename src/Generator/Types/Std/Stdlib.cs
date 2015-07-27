using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;

namespace CppSharp.Types.Std
{
    [TypeMap("va_list")]
    public class VaList : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "va_list";
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return "va_list";
        }

        public override bool IsIgnored
        {
            get { return true; }
        }
    }

    [TypeMap("std::string", GeneratorKind.CLI)]
    public class String : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "System::String^";
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

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return "Std.String";
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("new Std.String()");
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("std::wstring")]
    public class WString : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "System::String^";
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

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return "string";
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("new Std.WString()");
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("std::vector")]
    public class Vector : TypeMap
    {
        public override bool IsIgnored
        {
            get
            {
                var type = Type as TemplateSpecializationType;
                var pointeeType = type.Arguments[0].Type;

                var checker = new TypeIgnoreChecker(TypeMapDatabase);
                pointeeType.Visit(checker);

                return checker.IsIgnored;
            }
        }

        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return string.Format("System::Collections::Generic::List<{0}>^",
                ctx.GetTemplateParameterList());
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;
            var isPointerToPrimitive = type.Type.IsPointerToPrimitiveType();
            var managedType = isPointerToPrimitive
                ? new CILType(typeof(System.IntPtr))
                : type.Type;

            var entryString = (ctx.Parameter != null) ? ctx.Parameter.Name
                : ctx.ArgName;

            var tmpVarName = "_tmp" + entryString;

            var cppTypePrinter = new CppTypePrinter(ctx.Driver.TypeDatabase);
            var nativeType = type.Type.Visit(cppTypePrinter);

            ctx.SupportBefore.WriteLine("auto {0} = std::vector<{1}>();",
                tmpVarName, nativeType);
            ctx.SupportBefore.WriteLine("for each({0} _element in {1})",
                managedType, entryString);
            ctx.SupportBefore.WriteStartBraceIndent();
            {
                var param = new Parameter
                {
                    Name = "_element",
                    QualifiedType = type
                };

                var elementCtx = new MarshalContext(ctx.Driver)
                                     {
                                         Parameter = param,
                                         ArgName = param.Name,
                                     };

                var marshal = new CLIMarshalManagedToNativePrinter(elementCtx);
                type.Type.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    ctx.SupportBefore.Write(marshal.Context.SupportBefore);

                if (isPointerToPrimitive)
                    ctx.SupportBefore.WriteLine("auto _marshalElement = {0}.ToPointer();",
                        marshal.Context.Return);
                else
                    ctx.SupportBefore.WriteLine("auto _marshalElement = {0};",
                    marshal.Context.Return);

                ctx.SupportBefore.WriteLine("{0}.push_back(_marshalElement);",
                    tmpVarName);
            }
            
            ctx.SupportBefore.WriteCloseBraceIndent();

            ctx.Return.Write(tmpVarName);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;
            var isPointerToPrimitive = type.Type.IsPointerToPrimitiveType();
            var managedType = isPointerToPrimitive
                ? new CILType(typeof(System.IntPtr))
                : type.Type;
            var tmpVarName = "_tmp" + ctx.ArgName;
            
            ctx.SupportBefore.WriteLine(
                "auto {0} = gcnew System::Collections::Generic::List<{1}>();",
                tmpVarName, managedType);
            ctx.SupportBefore.WriteLine("for(auto _element : {0})",
                ctx.ReturnVarName);
            ctx.SupportBefore.WriteStartBraceIndent();
            {
                var elementCtx = new MarshalContext(ctx.Driver)
                                     {
                                         ReturnVarName = "_element",
                                         ReturnType = type
                                     };

                var marshal = new CLIMarshalNativeToManagedPrinter(elementCtx);
                type.Type.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    ctx.SupportBefore.Write(marshal.Context.SupportBefore);

                ctx.SupportBefore.WriteLine("auto _marshalElement = {0};",
                    marshal.Context.Return);

                if (isPointerToPrimitive)
                    ctx.SupportBefore.WriteLine("{0}->Add({1}(_marshalElement));",
                        tmpVarName, managedType);
                else
                    ctx.SupportBefore.WriteLine("{0}->Add(_marshalElement);",
                        tmpVarName);
            }
            ctx.SupportBefore.WriteCloseBraceIndent();

            ctx.Return.Write(tmpVarName);
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            if (ctx.CSharpKind == CSharpTypePrinterContextKind.Native)
                return "Std.Vector";

            return string.Format("Std.Vector<{0}>", ctx.GetTemplateParameterList());
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("{0}.Internal", ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;

            ctx.Return.Write("new Std.Vector<{0}>({1})", type,
                ctx.ReturnVarName);
        }
    }

    [TypeMap("std::map")]
    public class Map : TypeMap
    {
        public override bool IsIgnored { get { return true; } }

        public override string CLISignature(CLITypePrinterContext ctx)
        {
            var type = Type as TemplateSpecializationType;
            return string.Format(
                "System::Collections::Generic::Dictionary<{0}, {1}>^",
                type.Arguments[0].Type, type.Arguments[1].Type);
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            if (ctx.CSharpKind == CSharpTypePrinterContextKind.Native)
                return "Std.Map";

            var type = Type as TemplateSpecializationType;
            return string.Format(
                "System.Collections.Generic.Dictionary<{0}, {1}>",
                type.Arguments[0].Type, type.Arguments[1].Type);
        }
    }

    [TypeMap("std::list")]
    public class List : TypeMap
    {
        public override bool IsIgnored { get { return true; } }
    }

    [TypeMap("std::shared_ptr")]
    public class SharedPtr : TypeMap
    {
        public override bool IsIgnored { get { return true; } }

        public override string CLISignature(CLITypePrinterContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }

    [TypeMap("std::ostream", GeneratorKind.CLI)]
    public class OStream : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "System::IO::TextWriter^";
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            var marshal = (CLIMarshalManagedToNativePrinter) ctx.MarshalToNative;
            if (!ctx.Parameter.Type.Desugar().IsPointer())
                marshal.ArgumentPrefix.Write("*");
            var marshalCtxName = string.Format("ctx_{0}", ctx.Parameter.Name);
            ctx.SupportBefore.WriteLine("msclr::interop::marshal_context {0};", marshalCtxName);
            ctx.Return.Write("{0}.marshal_as<std::ostream*>({1})",
                marshalCtxName, ctx.Parameter.Name);
        }
    }

    [TypeMap("std::nullptr_t")]
    public class NullPtr : TypeMap
    {
        public override bool DoesMarshalling { get { return false; } }

        public override void CLITypeReference(CLITypeReferenceCollector collector,
            ASTRecord<Declaration> loc)
        {
            var typeRef = collector.GetTypeReference(loc.Value);

            var include = new Include
            {
                File = "cstddef",
                Kind = Include.IncludeKind.Angled,
            };

            typeRef.Include = include;
        }
    }

    [TypeMap("FILE")]
    public class FILE : TypeMap
    {
        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return "global::System.IntPtr";
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }
}
