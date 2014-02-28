using System.Collections.Generic;

namespace CppSharp.AST
{
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
        }

        /// <summary>
        /// File name of the library.
        /// </summary>
        public string FileName;

        /// <summary>
        /// Symbols gathered from the library.
        /// </summary>
        public IList<string> Symbols;
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

        private readonly Dictionary<string, NativeLibrary> compiledSymbols;

        public SymbolContext()
        {
            Libraries = new List<NativeLibrary>();
            Symbols = new Dictionary<string, NativeLibrary>();
            compiledSymbols = new Dictionary<string, NativeLibrary>();
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
                    Symbols[symbol] = library;
                    if (!symbol.StartsWith("_imp_") && !symbol.StartsWith("__imp_") &&
                        !symbol.StartsWith("_head") && !symbol.StartsWith("__head"))
                    {
                        if (symbol.StartsWith("__"))
                            compiledSymbols[symbol.Substring(1)] = library;
                        else
                            compiledSymbols[symbol] = library;
                    }
                }
            }
        }

        public bool FindSymbol(ref string symbol)
        {
            NativeLibrary lib;
            return FindLibraryBySymbol(symbol, out lib);
        }

        public bool FindLibraryBySymbol(string symbol, out NativeLibrary library)
        {
            return compiledSymbols.TryGetValue(symbol, out library);
        }
    }
}
