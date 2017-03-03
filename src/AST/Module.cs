using System;
using System.Collections.Generic;

namespace CppSharp.AST
{
    public class Module
    {
        public List<string> IncludeDirs { get; } = new List<string>();
        public List<string> Headers { get; } = new List<string>();
        public List<string> LibraryDirs { get; } = new List<string>();
        public List<string> Libraries { get; } = new List<string>();
        public List<string> Defines { get; } = new List<string>();
        public List<string> Undefines { get; } = new List<string>();
        public string OutputNamespace { get; set; }
        public List<TranslationUnit> Units { get; } = new List<TranslationUnit>();
        public List<string> CodeFiles { get; } = new List<string>();
        public List<Module> Dependencies { get; } = new List<Module>();

        [Obsolete("Use Module(string libraryName) instead.")]
        public Module()
        {
        }

        public Module(string libraryName)
        {
            LibraryName = libraryName;
        }

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
                    if (string.IsNullOrEmpty(OutputNamespace))
                        return string.Format("{0}-inlines", LibraryName);

                    return string.Format("{0}-inlines", OutputNamespace);
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
                    if (string.IsNullOrEmpty(OutputNamespace))
                        return string.Format("{0}-templates", LibraryName);

                    return string.Format("{0}-templates", OutputNamespace);
                }

                return templatesLibraryName;
            }
            set { templatesLibraryName = value; }
        }

        public string LibraryName { get; set; }

        public override string ToString() => LibraryName;

        private string sharedLibraryName;
        private string inlinesLibraryName;
        private string templatesLibraryName;
    }
}
