using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Types;

namespace CppSharp.Passes
{
    /// <summary>
    /// Used to provide different types of code transformation on a module
    /// declarations and types before the code generation process is started.
    /// </summary>
    public abstract class TranslationUnitPass : AstVisitor
    {
        public BindingContext Context { get; set; }

        public DriverOptions Options { get { return Context.Options; } }
        public ASTContext ASTContext { get { return Context.ASTContext; } }
        public TypeMapDatabase TypeMaps { get { return Context.TypeMaps; } }

        public bool ClearVisitedDeclarations = false;

        public virtual bool VisitASTContext(ASTContext context)
        {
            foreach (var unit in context.TranslationUnits)
                VisitTranslationUnit(unit);

            return true;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!unit.IsValid || unit.Ignore)
                return false;

            if (ClearVisitedDeclarations)
                Visited.Clear();

            VisitDeclarationContext(unit);

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            return !IsDeclExcluded(decl) && base.VisitDeclaration(decl);
        }

        bool IsDeclExcluded(Declaration decl)
        {
            var type = this.GetType();
            return decl.ExcludeFromPasses.Contains(type);
        }
    }

    public class TranslationUnitPassGeneratorDependent : TranslationUnitPass
    {
        public Generator Generator { get; }

        public TranslationUnitPassGeneratorDependent(Generator generator)
        {
            Generator = generator;
        }
    }

    /// <summary>
    /// Used to modify generated output.
    /// </summary>
    public abstract class GeneratorOutputPass
    {
        public IDiagnostics Log { get; set; }

        public virtual void HandleBlock(Block block)
        {
            switch (block.Kind)
            {
                case BlockKind.Class:
                    VisitClass(block);
                    break;
                case BlockKind.Method:
                    VisitMethod(block);
                    break;
                case BlockKind.Constructor:
                    VisitConstructor(block);
                    break;
                case BlockKind.ConstructorBody:
                    VisitConstructorBody(block);
                    break;
                case BlockKind.Namespace:
                    VisitNamespace(block);
                    break;
                case BlockKind.Includes:
                    VisitIncludes(block);
                    break;
            }

            foreach (var childBlock in block.Blocks)
                HandleBlock(childBlock);
        }

        public virtual void VisitCodeGenerator(CodeGenerator generator)
        {
            foreach (var block in generator.ActiveBlock.Blocks)
            {
                HandleBlock(block);
            }
        }

        public virtual void VisitGeneratorOutput(GeneratorOutput output)
        {
            foreach (var generator in output.Outputs)
            {
                VisitCodeGenerator(generator);
            }
        }

        public virtual void VisitClass(Block block)
        {

        }

        public virtual void VisitNamespace(Block block)
        {

        }

        public virtual void VisitMethod(Block block)
        {

        }

        public virtual void VisitConstructor(Block block)
        {

        }

        public virtual void VisitConstructorBody(Block block)
        {

        }

        public virtual void VisitIncludes(Block block)
        {

        }
    }
}
