namespace CppSharp.AST
{
    public enum MacroLocation
    {
        Unknown,
        ClassHead,
        ClassBody,
        FunctionHead,
        FunctionParameters,
        FunctionBody,
    };

    /// <summary>
    /// Base class that describes a preprocessed entity, which may
    /// be a preprocessor directive or macro expansion.
    /// </summary>
    public abstract class PreprocessedEntity : Declaration
    {
        public MacroLocation MacroLocation = MacroLocation.Unknown;
    }

    /// <summary>
    /// Represents a C preprocessor macro expansion.
    /// </summary>
    public class MacroExpansion : PreprocessedEntity
    {
        // Contains the macro expansion text.
        public string Text;

        public MacroDefinition Definition;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            //return visitor.VisitMacroExpansion(this);
            return default(T);
        }

        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    /// Represents a C preprocessor macro definition.
    /// </summary>
    public class MacroDefinition : PreprocessedEntity
    {
        // Contains the macro definition text.
        public string Expression;

        // Backing enumeration if one was generated.
        public Enumeration Enumeration;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitMacroDefinition(this);
        }

        public override string ToString()
        {
            return string.Format("{0} = {1}", Name, Expression);
        }
    }
}
