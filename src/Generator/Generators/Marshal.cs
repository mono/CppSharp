using CppSharp.AST;

namespace CppSharp.Generators
{
    public class MarshalContext : TypePrinter
    {
        public MarshalContext(BindingContext context, uint indentation)
        {
            Context = context;
            Before = new TextGenerator { CurrentIndentation = indentation };
            Return = new TextGenerator { CurrentIndentation = indentation };
            Cleanup = new TextGenerator { CurrentIndentation = indentation };
            VarPrefix = new TextGenerator();
            ArgumentPrefix = new TextGenerator();
            Indentation = indentation;
        }

        public BindingContext Context { get; }

        public MarshalPrinter<MarshalContext> MarshalToNative;

        public TextGenerator Before { get; }
        public TextGenerator Return { get; }
        public TextGenerator Cleanup { get; }
        public TextGenerator VarPrefix { get; }
        public TextGenerator ArgumentPrefix { get; }

        public string ReturnVarName { get; set; }
        public QualifiedType ReturnType { get; set; }

        public string ArgName { get; set; }
        public int ParameterIndex { get; set; }
        public Function Function { get; set; }

        public uint Indentation { get; }
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
