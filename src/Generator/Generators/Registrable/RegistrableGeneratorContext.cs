namespace CppSharp.Generators.Registrable
{
    public class RegistrableGeneratorContext : InfoMapStack<object>
    {
        public static readonly InfoEntry IndentLevel = new("CppSharp.Generators.Registrable.IndentLevel");
        public static readonly InfoEntry IsDetach = new("CppSharp.Generators.Registrable.IsDetach");
        public static readonly InfoEntry RootContextName = new("CppSharp.Generators.Registrable.RootContextName");
        public static readonly InfoEntry BindingContext = new("CppSharp.Generators.Registrable.BindingContext");
        public static readonly InfoEntry CppContext = new("CppSharp.Generators.Registrable.CppContext");
        public static readonly InfoEntry SanitizeType = new("CppSharp.Generators.Registrable.SanitizeType");
        public static readonly InfoEntry TypeArgumentsPack = new("CppSharp.Generators.Registrable.TypeArgumentsPack");
        public static readonly InfoEntry Resolvable = new("CppSharp.Generators.Registrable.Resolvable");
        public static readonly InfoEntry TemplateLevel = new("CppSharp.Generators.Registrable.TemplateLevel");

        public RegistrableGeneratorContext()
        {
        }

        public DetachmentOption PeekIsDetach()
        {
            return PeekIsDetach(DetachmentOption.Off);
        }

        public DetachmentOption PeekIsDetach(DetachmentOption defaultValue)
        {
            return Peek(IsDetach, defaultValue);
        }

        public void PushIsDetach(DetachmentOption item)
        {
            Push(IsDetach, item);
        }

        public DetachmentOption PopIsDetach()
        {
            return Pop<DetachmentOption>(IsDetach);
        }

        public string PeekRootContextName(string defaultValue = default)
        {
            return Peek(RootContextName, defaultValue);
        }

        public void PushRootContextName(string item)
        {
            Push(RootContextName, item);
        }

        public string PopRootContextName()
        {
            return Pop<string>(RootContextName);
        }

        public string PeekBindingContext(string defaultValue = default)
        {
            return Peek(BindingContext, defaultValue);
        }

        public void PushBindingContext(string item)
        {
            Push(BindingContext, item);
        }

        public string PopBindingContext()
        {
            return Pop<string>(BindingContext);
        }

        public CppContext PeekCppContext(CppContext defaultValue = default)
        {
            return Peek(CppContext, defaultValue);
        }

        public void PushCppContext(CppContext item)
        {
            Push(CppContext, item);
        }

        public CppContext PopCppContext()
        {
            return Pop<CppContext>(CppContext);
        }

        public int PeekTemplateLevel(int defaultValue = default)
        {
            return Peek(TemplateLevel, defaultValue);
        }

        public void PushTemplateLevel(int item)
        {
            Push(TemplateLevel, item);
        }

        public int PopTemplateLevel()
        {
            return Pop<int>(TemplateLevel);
        }
    }

    public class CppContext
    {
        public string FullyQualifiedName { get; set; }
        public FQNOption Option { get; set; }

        public string GetFullQualifiedName(FQNOption option)
        {
            if (!(Option | option).IgnoreTemplateTypenameKeyword)
            {
                return "typename " + FullyQualifiedName;
            }
            return FullyQualifiedName;
        }

        public string GetFullQualifiedName()
        {
            return GetFullQualifiedName(FQNOption.IgnoreNone);
        }
    };

    public enum DetachmentOption
    {
        On,
        Off,
        Forced
    }
}
