
namespace Cxxi.Passes
{
    /// <summary>
    /// Used to provide different types of code transformation on a module
    /// declarations and types before the code generation process is started.
    /// </summary>
    public abstract class TranslationUnitPass
    {
        public Library Library { get; set; }

        /// <summary>
        /// Processes a library.
        /// </summary>
        public virtual bool ProcessLibrary(Library library)
        {
            return false;
        }

        /// <summary>
        /// Processes a translation unit.
        /// </summary>
        public virtual bool ProcessUnit(TranslationUnit unit)
        {
            return false;
        }

        /// <summary>
        /// Processes a declaration.
        /// </summary>
        public virtual bool ProcessDeclaration(Declaration decl)
        {
            return false;
        }

        /// <summary>
        /// Processes a class.
        /// </summary>
        public virtual bool ProcessClass(Class @class)
        {
            return false;
        }

        /// <summary>
        /// Processes a field.
        /// </summary>
        public virtual bool ProcessField(Field field)
        {
            return false;
        }

        /// <summary>
        /// Processes a function declaration.
        /// </summary>
        public virtual bool ProcessFunction(Function function)
        {
            return false;
        }

        /// <summary>
        /// Processes a method declaration.
        /// </summary>
        public virtual bool ProcessMethod(Method method)
        {
            return false;
        }

        /// <summary>
        /// Processes an enum declaration.
        /// </summary>
        public virtual bool ProcessEnum(Enumeration @enum)
        {
            return false;
        }

        /// <summary>
        /// Processes an enum item.
        /// </summary>
        public virtual bool ProcessEnumItem(Enumeration.Item item)
        {
            return false;
        }
    }
}
