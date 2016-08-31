using CppSharp.Parser.AST;

namespace CppSharp.Parser
{
    public class ParserOptions2 : ParserOptions
    {
        public ParserOptions2()
        {
            Abi = Platform.IsUnixPlatform ? CppAbi.Itanium : CppAbi.Microsoft;
            MicrosoftMode = !Platform.IsUnixPlatform;
        }

        public bool IsItaniumLikeAbi { get { return Abi != CppAbi.Microsoft; } }
        public bool IsMicrosoftAbi { get { return Abi == CppAbi.Microsoft; } }
    }
}
