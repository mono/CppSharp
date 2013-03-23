using System;
using System.Linq;
using Cxxi.Generators.CLI;

namespace Cxxi.Types.Std
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
        public override string CLISignature(TypePrinterContext ctx)
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

        public override string CSharpSignature()
        {
            return "string";
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }
    }

    [TypeMap("std::wstring")]
    public class WString : TypeMap
    {
        public override string CLISignature(TypePrinterContext ctx)
        {
            return "System::String^";
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("marshalString<E_UTF16>({0})", ctx.Parameter.Name);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("marshalString<E_UTF16>({0})", ctx.ReturnVarName);
        }
    }

    [TypeMap("std::vector")]
    public class Vector : TypeMap
    {
        public override bool IsIgnored { get { return true; } }

        public override string CLISignature(TypePrinterContext ctx)
        {
            var type = Type as TemplateSpecializationType;
            var typeName = type.Arguments[0].Type.ToString();
            return string.Format("System::Collections::Generic::List<{0}>^", typeName);
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

    [TypeMap("std::map")]
    public class Map : TypeMap
    {
        public override bool IsIgnored { get { return true; } }

        public override string CLISignature(TypePrinterContext ctx)
        {
            var type = Type as TemplateSpecializationType;
            return string.Format("System::Collections::Generic::Dictionary<{0}, {1}>^",
                type.Arguments[0].Type,
                type.Arguments[1].Type);
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

    [TypeMap("std::list")]
    public class List : TypeMap
    {
        public override bool IsIgnored { get { return true; } }
    }

    [TypeMap("std::shared_ptr")]
    public class SharedPtr : TypeMap
    {
        public override string CLISignature(TypePrinterContext ctx)
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
