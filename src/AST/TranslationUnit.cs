using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a source code unit.
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

        public Module Module { get; set; }

        public bool IsSystemHeader { get; set; }

        public bool IsValid => FilePath != "<invalid>";

        /// Contains the path to the file.
        public string FilePath;

        private string fileName;
        private string fileNameWithoutExtension;

        /// Contains the name of the file.
        public string FileName
        {
            get { return !IsValid ? FilePath : fileName ?? (fileName = Path.GetFileName(FilePath)); }
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
                if (fileRelativeDirectory != null)
                    return fileRelativeDirectory;

                if (IncludePath == null)
                    return string.Empty;

                var path = IncludePath.Replace('\\', '/');
                var index = path.LastIndexOf('/');
                return fileRelativeDirectory = path.Substring(0, index);
            }
        }

        public string FileRelativePath
        {
            get
            {
                if (!IsValid) return string.Empty;
                return fileRelativePath ??
                    (fileRelativePath = Path.Combine(FileRelativeDirectory, FileName));
            }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTranslationUnit(this);
        }

        public override string ToString() => FileName;
    }
}