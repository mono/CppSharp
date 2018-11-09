using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators
{
    public class MarshalContext : TypePrinter
    {
        public MarshalContext(BindingContext context, Stack<uint> indent)
        {
            Context = context;
            Before = new TextGenerator();
            indent.PushTo(Before.CurrentIndent);
            Return = new TextGenerator();
            indent.PushTo(Return.CurrentIndent);
            MarshalVarPrefix = string.Empty;
            this.Indent = indent;
        }

        public BindingContext Context { get; }

        public MarshalPrinter<MarshalContext> MarshalToNative;

        public TextGenerator Before { get; }
        public TextGenerator Return { get; }

        public string ReturnVarName { get; set; }
        public QualifiedType ReturnType { get; set; }

        public string ArgName { get; set; }
        public int ParameterIndex { get; set; }
        public Function Function { get; set; }

        public string MarshalVarPrefix { get; set; }
        public Stack<uint> Indent { get; }
    }

    public abstract class MarshalPrinter<T> : AstVisitor where T : MarshalContext
    {
        public T Context { get; }

        protected MarshalPrinter(T ctx)
        {
            Context = ctx;
        }
    }
}
