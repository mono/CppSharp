using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Parser;

namespace CppSharp
{
    /// <summary>
    /// Represents a reference to a source file.
    /// </summary>
    public class SourceFile
    {
        /// <summary>
        /// Gets the file name.
        /// </summary>
        public string Name
        {
            get { return System.IO.Path.GetFileName(Path); }
        }

        /// <summary>
        /// Gets the file path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets/sets the parser options for the file.
        /// </summary>
        public ParserOptions Options { get; set; }

        /// <summary>
        /// Gets/sets the AST representation of the file.
        /// </summary>
        public TranslationUnit Unit { get; set; }

        public SourceFile(string path)
        {
            Path = path;
        }

        public override string ToString()
        {
            return Path;
        }
    }

    /// <summary>
    /// Represents a C++ project with source and library files.
    /// </summary>
    public class Project
    {
        /// <summary>
        /// List of per-project C/C++ preprocessor defines.
        /// </summary>
        public IList<string> Defines { get; private set; }

        /// <summary>
        /// List of per-project C/C++ include directories.
        /// </summary>
        public IList<string> IncludeDirs { get; private set; }

        /// <summary>
        /// List of per-project C/C++ system include directories.
        /// </summary>
        public IList<string> SystemIncludeDirs { get; private set; }

        /// <summary>
        /// List of source files in the project.
        /// </summary>
        public IList<SourceFile> Sources { get; private set; }

        /// <summary>
        /// Main AST context with translation units for the sources.
        /// </summary>
        public ASTContext ASTContext { get; private set; }

        /// <summary>
        /// Main symbols context with indexed library symbols.
        /// </summary>
        public SymbolContext SymbolsContext { get; private set; }
        
        public Project()
        {
            Defines = new List<string>();
            IncludeDirs = new List<string>();
            SystemIncludeDirs = new List<string>();

            Sources = new List<SourceFile>();
        }

        /// <summary>
        /// Adds a new source file to the project.
        /// </summary>
        public SourceFile AddFile(string path)
        {
            var sourceFile = new SourceFile(path);
            Sources.Add(sourceFile);

            return sourceFile;
        }

        /// <summary>
        /// Adds a group of source files to the project.
        /// </summary>
        public void AddFiles(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                AddFile(path);
        }

        /// <summary>
        /// Adds a new source file to the project.
        /// </summary>
        public void AddFolder(string path)
        {
            var sourceFile = new SourceFile(path);
            Sources.Add(sourceFile);
        }
    }
}
