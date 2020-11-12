using CppSharp.Parser;

namespace CppSharp.Extensions
{
    public static class ParserIntTypeExtensions
    {
        public static bool IsSigned(this ParserIntType intType)
        {
            switch (intType)
            {
                case ParserIntType.SignedChar:
                case ParserIntType.SignedShort:
                case ParserIntType.SignedInt:
                case ParserIntType.SignedLong:
                case ParserIntType.SignedLongLong:
                    return true;
                default:
                    return false;
            }
        }
    }
}
