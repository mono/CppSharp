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

        /// Contains the name of the file.
        public string FileName
        {
            get { return Path.GetFileName(FilePath); }
        }

        /// Contains the name of the module.
        public string FileNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(FileName); }
        }

        /// Contains the include path.
        public string IncludePath;

        public string FileRelativeDirectory
        {
            get
            {
                var path = IncludePath.Replace('\\', '/');
                var index = path.LastIndexOf('/');
                return path.Substring(0, index);
            }
        }

        public string FileRelativePath
        {
            get
            {
                return Path.Combine(FileRelativeDirectory, FileName);
            }
        }
    }
}