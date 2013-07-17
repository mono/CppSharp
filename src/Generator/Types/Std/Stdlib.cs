using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;

namespace CppSharp.Types.Std
{
    [TypeMap("va_list")]
    public class VaList : TypeMap
    {
        public override bool IsIgnored
        {
            get { return true; }
        }
    }

    [TypeMap("std::string")]
    public class String : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "System::String^";
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0})", ctx.Parameter.Name);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0})", ctx.ReturnVarName);
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
            ctx.Return.Write("clix::marshalString<clix::E_UTF16>({0})", ctx.Parameter.Name);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF16>({0})", ctx.ReturnVarName);
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return "string";
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("StringToPtr");
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("PtrToString");
        }
    }

    [TypeMap("std::vector")]
    public class Vector : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return string.Format("System::Collections::Generic::List<{0}>^",
                ctx.GetTemplateParameterList());
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;

            var entryString = (ctx.Parameter != null) ? ctx.Parameter.Name : ctx.ArgName;

            var tmpVarName = "_tmp" + entryString;

            var cppTypePrinter = new CppTypePrinter(ctx.Driver.TypeDatabase);
            var nativeType = type.Type.Visit(cppTypePrinter);

            ctx.SupportBefore.WriteLine("auto {0} = std::vector<{1}>();",
                tmpVarName, nativeType);
            ctx.SupportBefore.WriteLine("for each({0} _element in {1})",
                type.ToString(), entryString);
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

                ctx.SupportBefore.WriteLine("auto _marshalElement = {0};",
                    marshal.Context.Return);

                ctx.SupportBefore.WriteLine("{0}.push_back(_marshalElement);",tmpVarName);
            }
            
            ctx.SupportBefore.WriteCloseBraceIndent();

            ctx.Return.Write(tmpVarName);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;
            var tmpVarName = "_tmp" + ctx.ArgName;
            
            ctx.SupportBefore.WriteLine("auto {0} = gcnew System::Collections::Generic::List<{1}>();",
                tmpVarName, type.ToString());
            ctx.SupportBefore.WriteLine("for(auto _element : {0})",ctx.ReturnVarName);
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

                ctx.SupportBefore.WriteLine("auto _marshalElement = {0};", marshal.Context.Return);

                ctx.SupportBefore.WriteLine("{0}->Add(_marshalElement);", tmpVarName);
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
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;

            ctx.Return.Write("Std.Vector.Create({0})", ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            var templateType = Type as TemplateSpecializationType;
            var type = templateType.Arguments[0].Type;

            ctx.Return.Write("Std.Vector.Create<{0}>({1})", type,
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
            return string.Format("System::Collections::Generic::Dictionary<{0}, {1}>^",
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
            var type = Type as TemplateSpecializationType;
            return string.Format("System.Collections.Generic.Dictionary<{0}, {1}>",
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
}
