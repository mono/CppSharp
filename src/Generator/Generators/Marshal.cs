using CppSharp.AST;
using CppSharp.Generators.C;
using System;

namespace CppSharp.Generators
{
    public class MarshalContext : TypePrinter
    {
        public MarshalContext(BindingContext context, uint indentation) : base(context)
        {
            Before = new TextGenerator { CurrentIndentation = indentation };
            Return = new TextGenerator { CurrentIndentation = indentation };
            Cleanup = new TextGenerator { CurrentIndentation = indentation };
            VarPrefix = new TextGenerator();
            ArgumentPrefix = new TextGenerator();
            Indentation = indentation;
        }

        public MarshalPrinter<MarshalContext, CppTypePrinter> MarshalToNative;

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

    public abstract class MarshalPrinter<C, P> : AstVisitor where C : MarshalContext where P : TypePrinter
    {
        public C Context { get; }

        protected MarshalPrinter(C ctx)
        {
            Context = ctx;
            typePrinter = (P)Activator.CreateInstance(typeof(P), ctx.Context);
        }

        protected P typePrinter;
    }
}
