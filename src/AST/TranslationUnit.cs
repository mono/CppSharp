using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a parsed C++ unit.
    /// </summary>
    [DebuggerDisplay("File = {FileName}, Ignored = {Ignore}")]
    public class TranslationUnit : Namespace
    {
        public TranslationUnit()
        {
            Macros = new List<MacroDefinition>();
            Access = AccessSpecifier.Public;
        }

        public TranslationUnit(string file) : this()
        {
            FilePath = file;
        }

        /// Contains the macros present in the unit.
        public List<MacroDefinition> Macros;

        public bool IsSystemHeader { get; set; }

        public bool IsValid { get { return FilePath != "<invalid>"; } }

        /// Contains the path to the file.
        public string FilePath;

        private string fileName;
        private string fileNameWithoutExtension;

        /// Contains the name of the file.
        public string FileName
        {
            get { return fileName ?? (fileName = Path.GetFileName(FilePath)); }
        }

        /// Contains the name of the module.
        public string FileNameWithoutExtension
        {
            get
            {
                return fileNameWithoutExtension ??
                    (fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName));
            }
        }

        /// Contains the include path.
        public string IncludePath;

        private string fileRelativeDirectory;
        private string fileRelativePath;

        public string FileRelativeDirectory
        {
            get
            {
                if (fileRelativeDirectory != null) return fileRelativeDirectory;
                var path = IncludePath.Replace('\\', '/');
                var index = path.LastIndexOf('/');
                return fileRelativeDirectory = path.Substring(0, index);
            }
        }

        public string FileRelativePath
        {
            get
            {
                return fileRelativePath ??
                    (fileRelativePath = Path.Combine(FileRelativeDirectory, FileName));
            }
        }
    }
}