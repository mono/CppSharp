using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.C;
using CppSharp.Passes;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// C++ generator responsible for driving the generation of source and
    /// header files.
    /// </summary>
    public class CppGenerator : CGenerator
    {
        public CppGenerator(BindingContext context) : base(context)
        {
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new CppHeaders(Context, units);
            outputs.Add(header);

            var source = new CppSources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override bool SetupPasses()
        {
            new FixupPureMethodsPass().VisitASTContext(Context.ASTContext);
            return true;
        }

        public static bool ShouldGenerateClassNativeInstanceField(Class @class)
        {
            if (@class.IsStatic)
                return false;

            return @class.IsRefType && (!@class.HasBase || !@class.HasRefBase());
        }
    }

    /// <summary>
    /// Removes the pureness of virtual abstract methods in C++ classes since
    /// the generated classes cannot have virtual pure methods, as they call
    /// the original pure method.
    /// This lets user code mark some methods as pure if needed, in that case
    /// the generator can generate the necessary pure C++ code annotations safely
    /// knowing the only pure functions were user-specified.
    /// </summary>
    public class FixupPureMethodsPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            method.IsPure = false;
            return base.VisitMethodDecl(method);
        }
    }
}
