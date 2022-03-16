using System;
using System.Collections.Generic;

namespace CppSharp.AST
{
    public class Module
    {
        public List<string> IncludeDirs { get; set; } = new List<string>();
        public List<string> Headers { get; set; } = new List<string>();
        public List<string> LibraryDirs { get; } = new List<string>();
        public List<string> Libraries { get; set; } = new List<string>();
        public List<string> Defines { get; set; } = new List<string>();
        public List<string> Undefines { get; set; } = new List<string>();
        public string OutputNamespace { get; set; }
        public List<TranslationUnit> Units { get; set; } = new List<TranslationUnit>();
        public List<string> CodeFiles { get; set; } = new List<string>();
        public List<string> ReferencedAssemblies { get; set; } = new List<string>();
        public List<Module> Dependencies { get; set; } = new List<Module>();

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

        public string SymbolsLibraryName
        {
            get
            {
                if (string.IsNullOrEmpty(symbolsLibraryName))
                {
                    if (string.IsNullOrEmpty(OutputNamespace))
                        return $"{LibraryName}-symbols";

                    return $"{OutputNamespace}-symbols";
                }

                return symbolsLibraryName;
            }
            set { symbolsLibraryName = value; }
        }

        public string LibraryName { get; set; }

        public override string ToString() => LibraryName;

        private string sharedLibraryName;
        private string symbolsLibraryName;

        /// <summary>
        /// Gets the class template specializations which use in their arguments
        /// types located in this <see cref="Module"/> which is not the module their template is located in.
        /// </summary>
        public HashSet<ClassTemplateSpecialization> ExternalClassTemplateSpecializations { get; } = new HashSet<ClassTemplateSpecialization>();
    }
}
