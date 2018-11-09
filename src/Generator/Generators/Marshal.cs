using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators
{
    public class MarshalContext : TypePrinter
    {
        public MarshalContext(BindingContext context, Stack<uint> indentation)
        {
            Context = context;
            Before = new TextGenerator();
            indentation.PushTo(Before.CurrentIndentation);
            Return = new TextGenerator();
            indentation.PushTo(Return.CurrentIndentation);
            MarshalVarPrefix = string.Empty;
            this.Indentation = indentation;
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
        public Stack<uint> Indentation { get; }
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
