using System;
using System.Collections.Generic;

namespace CppSharp.AST
{
    public enum ArchType
    {
        UnknownArch,

        x86, // X86: i[3-9]86
        x86_64 // X86-64: amd64, x86_64
    }

    /// <summary>
    /// Represents a shared library or a static library archive.
    /// </summary>
    public class NativeLibrary
    {
        public NativeLibrary(string file)
            : this()
        {
            FileName = file;
        }

        public NativeLibrary()
        {
            Symbols = new List<string>();
            Dependencies = new List<string>();
        }

        /// <summary>
        /// File name of the library.
        /// </summary>
        public string FileName;

        public ArchType ArchType { get; set; }

        /// <summary>
        /// Symbols gathered from the library.
        /// </summary>
        public IList<string> Symbols;

        public IList<string> Dependencies { get; private set; }
    }

    public class SymbolContext
    {
        /// <summary>
        /// List of native libraries.
        /// </summary>
        public List<NativeLibrary> Libraries;

        /// <summary>
        /// Index of all symbols to their respective libraries.
        /// </summary>
        public Dictionary<string, NativeLibrary> Symbols;

        public SymbolContext()
        {
            Libraries = new List<NativeLibrary>();
            Symbols = new Dictionary<string, NativeLibrary>();
        }

        public NativeLibrary FindOrCreateLibrary(string file)
        {
            var library = Libraries.Find(m => m.FileName.Equals(file));

            if (library == null)
            {
                library = new NativeLibrary(file);
                Libraries.Add(library);
            }

            return library;
        }

        public void IndexSymbols()
        {
            foreach (var library in Libraries)
            {
                foreach (var symbol in library.Symbols)
                {
                    if (!Symbols.ContainsKey(symbol))
                        Symbols[symbol] = library;
                    if (symbol.StartsWith("__", StringComparison.Ordinal))
                    {
                        string stripped = symbol.Substring(1);
                        if (!Symbols.ContainsKey(stripped))
                            Symbols[stripped] = library;
                    }
                }
            }
        }

        public bool FindLibraryBySymbol(string symbol, out NativeLibrary library) =>
            Symbols.TryGetValue(symbol, out library) ||
            Symbols.TryGetValue("_" + symbol, out library) ||
            Symbols.TryGetValue(symbol.TrimStart('_'), out library) ||
            Symbols.TryGetValue("_imp_" + symbol, out library) ||
            Symbols.TryGetValue("__imp_" + symbol, out library);
    }
}
