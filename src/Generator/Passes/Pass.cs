using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    /// <summary>
    /// Used to provide different types of code transformation on a module
    /// declarations and types before the code generation process is started.
    /// </summary>
    public abstract class TranslationUnitPass : AstVisitor
    {
        public Driver Driver { get; set; }
        public ASTContext AstContext { get; set; }
        
        public IDiagnosticConsumer Log
        {
            get { return Driver.Diagnostics; }
        }

        public bool ClearVisitedDeclarations = false;

        public virtual bool VisitLibrary(ASTContext context)
        {
            AstContext = context;
            foreach (var unit in context.TranslationUnits)
                VisitTranslationUnit(unit);

            return true;
        }

        public virtual bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!unit.IsValid)
                return false;

            if (unit.IsSystemHeader)
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
        public Driver Driver { get; set; }

        public virtual void VisitGeneratorOutput(GeneratorOutput output)
        {
            
        }
    }
}
