using System.Collections.Generic;

namespace CppSharp.AST
{
    public class Module
    {
        public static readonly Module SystemModule = new Module { OutputNamespace = string.Empty, LibraryName = "Std" };

        public Module()
        {
            IncludeDirs = new List<string>();
            Headers = new List<string>();
            LibraryDirs = new List<string>();
            Libraries = new List<string>();
            Defines = new List<string>();
            Undefines = new List<string>();
            Units = new List<TranslationUnit>();
            CodeFiles = new List<string>();
        }

        public List<string> IncludeDirs { get; private set; }
        public List<string> Headers { get; private set; }
        public List<string> LibraryDirs { get; set; }
        public List<string> Libraries { get; private set; }
        public List<string> Defines { get; set; }
        public List<string> Undefines { get; set; }
        public string OutputNamespace { get; set; }

        public List<TranslationUnit> Units { get; private set; }
        public List<string> CodeFiles { get; private set; }

        public string SharedLibraryName
        {
            get
            {
                if (string.IsNullOrEmpty(sharedLibraryName))
                    return LibraryName;
                return sharedLibraryName;
            }
            set { sharedLibraryName = value; }
        }

        public string InlinesLibraryName
        {
            get
            {
                if (string.IsNullOrEmpty(inlinesLibraryName))
                {
                    return string.Format("{0}-inlines", LibraryName);
                }
                return inlinesLibraryName;
            }
            set { inlinesLibraryName = value; }
        }

        public string TemplatesLibraryName
        {
            get
            {
                if (string.IsNullOrEmpty(templatesLibraryName))
                {
                    return string.Format("{0}-templates", LibraryName);
                }
                return templatesLibraryName;
            }
            set { templatesLibraryName = value; }
        }

        public string LibraryName { get; set; }

        private string sharedLibraryName;
        private string inlinesLibraryName;
        private string templatesLibraryName;
    }
}
