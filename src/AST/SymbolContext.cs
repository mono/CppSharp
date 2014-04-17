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
                    Symbols[symbol] = library;
                    if (symbol.StartsWith("__"))
                    {
                        string stripped = symbol.Substring(1);
                        if (!Symbols.ContainsKey(stripped))
                            Symbols[stripped] = library;
                    }
                }
            }
        }

        public bool FindSymbol(ref string symbol)
        {
            NativeLibrary lib;

            if (FindLibraryBySymbol(symbol, out lib))
                return true;

            // Check for C symbols with a leading underscore.
            if (FindLibraryBySymbol("_" + symbol, out lib))
            {
                symbol = "_" + symbol;
                return true;
            }

            if (FindLibraryBySymbol("_imp_" + symbol, out lib))
            {
                symbol = "_imp_" + symbol;
                return true;
            }

            if (FindLibraryBySymbol("__imp_" + symbol, out lib))
            {
                symbol = "__imp_" + symbol;
                return true;
            }

            return false;
        }

        public bool FindLibraryBySymbol(string symbol, out NativeLibrary library)
        {
            return Symbols.TryGetValue(symbol, out library);
        }
    }
}
