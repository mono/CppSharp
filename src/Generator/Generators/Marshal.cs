namespace CppSharp.Generators
{
    public class MarshalContext
    {
        public MarshalContext(Driver driver)
        {
            Driver = driver;
            SupportBefore = new TextGenerator();
            Return = new TextGenerator();
        }

        public Driver Driver { get; private set; }

        public MarshalPrinter MarshalToManaged;
        public MarshalPrinter MarshalToNative;

        public TextGenerator SupportBefore { get; private set; }
        public TextGenerator Return { get; private set; }

        public string ReturnVarName { get; set; }
        public QualifiedType ReturnType { get; set; }

        public string ArgName { get; set; }
        public Parameter Parameter { get; set; }
        public int ParameterIndex { get; set; }
        public Function Function { get; set; }
    }

    public abstract class MarshalPrinter : AstVisitor
    {
        public MarshalContext Context { get; private set; }

        protected MarshalPrinter(MarshalContext ctx)
        {
            Context = ctx;
        }
    }
}
