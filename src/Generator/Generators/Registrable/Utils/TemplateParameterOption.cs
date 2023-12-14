namespace CppSharp.Generators.Registrable
{
    public class TemplateParameterOption
    {
        public static readonly TemplateParameterOption AsParameter = new(false, false);
        public static readonly TemplateParameterOption AsParameterNoDefault = new(false, true);
        public static readonly TemplateParameterOption AsArgument = new(true, true);

        public bool IgnoreKeyword { get; set; }
        public bool IgnoreDefault { get; set; }
        public string CustomPrefix { get; set; }

        TemplateParameterOption(bool ignoreKeyword = false,
            bool ignoreDefault = false,
            string customPrefix = "")
        {
            IgnoreKeyword = ignoreKeyword;
            IgnoreDefault = ignoreDefault;
            CustomPrefix = customPrefix;
        }
    }
}
