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

        public IDiagnostics Diagnostics { get { return Context.Diagnostics; } }
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

        public virtual bool VisitTranslationUnit(TranslationUnit unit)
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

    /// <summary>
    /// Used to modify generated output.
    /// </summary>
    public abstract class GeneratorOutputPass
    {
        public IDiagnostics Log { get; set; }

        public virtual void VisitGeneratorOutput(GeneratorOutput output)
        {
            
        }
    }
}
