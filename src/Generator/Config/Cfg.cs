namespace CppSharp.Config
{
    using CppSharp.AST;
    using CppSharp.Parser;

    internal class Cfg
    {
        public bool SetupMSVC { get; set; }
        public ParserOptions ParserOptions { get; set; }
        public Module Module { get; set; }
        public DriverOptions Options { get; set; }
    }
}