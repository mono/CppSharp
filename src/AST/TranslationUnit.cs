using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CppSharp
{
    /// <summary>
    /// Represents a parsed C++ unit.
    /// </summary>
    [DebuggerDisplay("File = {FileName}, Ignored = {Ignore}")]
    public class TranslationUnit : Namespace
    {
        public TranslationUnit(string file)
        {
            Macros = new List<MacroDefinition>();
            FilePath = file;
        }

        /// Contains the macros present in the unit.
        public List<MacroDefinition> Macros;

        // Whether the unit should be generated.
        public override bool IsGenerated
        {
            get { return !IgnoreFlags.HasFlag(IgnoreFlags.Generation); }
        }

        // Whether the unit should be processed.
        public override bool IsProcessed
        {
            get { return !IgnoreFlags.HasFlag(IgnoreFlags.Processing); }
        }

        // Whether the unit should be ignored.
        public override bool Ignore
        {
            get { return IgnoreFlags != IgnoreFlags.None; }
        }

        public bool IsSystemHeader { get; set; }

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

        /// Type references object.
        public object TypeReferences;
    }
}