using System.Collections.Generic;
using CppSharp.Parser;

namespace CppSharp
{
    /// <summary>
    /// Contains sets of headers and optionally libraries to wrap in a one target library.
    /// </summary>
    public class Module : AbstractModule
    {
        public Module()
        {
            Headers = new List<string>();
            Libraries = new List<string>();
        }

        public List<string> Headers { get; private set; }
        public List<string> Libraries { get; private set; }
        public string OutputNamespace { get; set; }

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
                    return string.Format("{0}-templates", OutputNamespace);
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
