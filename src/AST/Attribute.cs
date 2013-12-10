namespace CppSharp.AST
{
    /// <summary>
    /// Gives the ability to specify attributes to generate, for example ObsoleteAttribute.
    /// </summary>
    public class Attribute
    {
        public Attribute()
        {
            Type = typeof(object);
            Value = string.Empty;
        }

        public System.Type Type { get; set; }

        public string Value { get; set; }
    }
}
