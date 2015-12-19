using System;

namespace CppSharp
{
    public class ParserException : Exception
    {
        public ParserException(string message)
            : base(message)
        {
        }
    }
}
