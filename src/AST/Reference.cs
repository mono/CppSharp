
namespace CppSharp.AST
{
    /// <summary>
    /// Represents a type reference.
    /// </summary>
    public class TypeReference
    {
        public Declaration Declaration;
        public string FowardReference;

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(FowardReference))
                return FowardReference;

            return base.ToString();
        }
    }
}
