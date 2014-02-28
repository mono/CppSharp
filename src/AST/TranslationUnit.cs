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

        // Whether the unit should be generated.
        public override bool IsGenerated
        {
            get { return !IgnoreFlags.HasFlag(IgnoreFlags.Generation); }
        }

        // Whether the unit should be ignored.
        public override bool Ignore
        {
            get { return IgnoreFlags != IgnoreFlags.None; }
        }

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
    }
}