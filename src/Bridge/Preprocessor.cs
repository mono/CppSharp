namespace CppSharp
{
    /// <summary>
    /// Base class that describes a preprocessed entity, which may
    /// be a preprocessor directive or macro expansion.
    /// </summary>
    public abstract class PreprocessedEntity : Declaration
    {

    }

    /// <summary>
    /// Represents a C preprocessor macro expansion.
    /// </summary>
    public class MacroExpansion : PreprocessedEntity
    {
        // Contains the macro expansion text.
        public string Text;

        public MacroDefinition Definition;

        public MacroExpansion()
        {
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            //return visitor.VisitMacroExpansion(this);
            return default(T);
        }
    }

    /// <summary>
    /// Represents a C preprocessor macro definition.
    /// </summary>
    public class MacroDefinition : PreprocessedEntity
    {
        // Contains the macro definition text.
        public string Expression;

        public MacroDefinition()
        {
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitMacroDefinition(this);
        }
    }
}
