using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Types;

namespace TypeMaps.Gen
{
    [TypeMap("QList")]
    public class QList : TypeMap
    {
        public override bool DoesMarshalling
        {
            get { return false; }
        }

        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "TypeMaps::QList^";
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            if (ctx.CSharpKind == CSharpTypePrinterContextKind.Native)
            {
                if (Type.IsAddress())
                {
                    return "QList.Internal*";
                }
                return "QList.Internal";
            }
            return "QList";
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
        }

        public override void CSharpMarshalCopyCtorToManaged(MarshalContext ctx)
        {
            ctx.SupportBefore.WriteLine("var __instance = new {0}.Internal();", ctx.ReturnType);
            ctx.SupportBefore.WriteLine("__instance.i = {0}.i;", ctx.ReturnVarName);
        }
    }
}
