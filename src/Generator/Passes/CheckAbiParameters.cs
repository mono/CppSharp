namespace CppSharp.Passes
{
    public class CheckAbiParameters : TranslationUnitPass
    {
        private readonly DriverOptions options;

        public CheckAbiParameters(DriverOptions options)
        {
            this.options = options;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!NeedsHiddenStructParameterReturn(method, options))
                return true;

            var structParameter = new Parameter()
                {
                    Kind = ParameterKind.HiddenStructureReturn,
                    QualifiedType = new QualifiedType(
                        new BuiltinType(PrimitiveType.IntPtr)),
                    Name = "return",
                    IgnoreFlags = IgnoreFlags.Generation
                };

            method.Parameters.Insert(0, structParameter);

            return true;
        }

        public static bool NeedsHiddenStructParameterReturn(Method method, DriverOptions options)
        {
            // In both the Microsoft and Itanium ABI, functions returning
            // structure types by value have an extra parameter 
            // Itanium ABI reference (3.1.4 Return values):
            // http://refspecs.linux-foundation.org/cxxabi-1.83.html#calls
            // Microsoft ABI reference:
            // http://blog.aaronballman.com/2012/02/describing-the-msvc-abi-for-structure-return-types/

            Class retClass;
            if (!method.ReturnType.IsTagDecl(out retClass))
                return false;

            // TODO: Add the various combinations for that need hidden parameter
            var needsMSHiddenPtr = options.IsMicrosoftAbi && method.IsThisCall;

            return needsMSHiddenPtr || options.IsItaniumAbi;
        }
    }

    public static class CheckAbiParametersExtensions
    {
        public static void CheckAbiParameters(this PassBuilder builder,
            DriverOptions options)
        {
            var pass = new CheckAbiParameters(options);
            builder.AddPass(pass);
        }
    }
}
