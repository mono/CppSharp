namespace CppSharp.Generators.Registrable
{
    public class FQNOption
    {
        public static readonly FQNOption IgnoreNone = new(false, false, false, false);
        public static readonly FQNOption IgnoreAll = new(true, true, true, true);

        public bool IgnoreGlobalNamespace { get; set; }
        public bool IgnoreTemplateTypenameKeyword { get; set; }
        public bool IgnoreTemplateTemplateKeyword { get; set; }
        public bool IgnoreTemplateParameters { get; set; }

        public FQNOption(bool ignoreGlobalNamespace = false,
                  bool ignoreTemplateTypenameKeyword = false,
                  bool ignoreTemplateTemplateKeyword = false,
                  bool ignoreTemplateParameters = false)
        {
            IgnoreGlobalNamespace = ignoreGlobalNamespace;
            IgnoreTemplateTypenameKeyword = ignoreTemplateTypenameKeyword;
            IgnoreTemplateTemplateKeyword = ignoreTemplateTemplateKeyword;
            IgnoreTemplateParameters = ignoreTemplateParameters;
        }

        public static FQNOption operator |(FQNOption lhs, FQNOption rhs)
        {
            return new FQNOption(
                lhs.IgnoreGlobalNamespace | rhs.IgnoreGlobalNamespace,
                lhs.IgnoreTemplateTypenameKeyword | rhs.IgnoreTemplateTypenameKeyword,
                lhs.IgnoreTemplateTemplateKeyword | rhs.IgnoreTemplateTemplateKeyword,
                lhs.IgnoreTemplateParameters | rhs.IgnoreTemplateParameters
            );
        }

        public static FQNOption operator &(FQNOption lhs, FQNOption rhs)
        {
            return new FQNOption(
                lhs.IgnoreGlobalNamespace & rhs.IgnoreGlobalNamespace,
                lhs.IgnoreTemplateTypenameKeyword & rhs.IgnoreTemplateTypenameKeyword,
                lhs.IgnoreTemplateTemplateKeyword & rhs.IgnoreTemplateTemplateKeyword,
                lhs.IgnoreTemplateParameters & rhs.IgnoreTemplateParameters
            );
        }
    }
}
