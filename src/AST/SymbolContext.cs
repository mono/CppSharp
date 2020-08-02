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

        public bool FindSymbol(ref string symbol)
        {
            if (FindLibraryBySymbol(symbol, out _))
                return true;

            string alternativeSymbol;

            // Check for C symbols with a leading underscore.
            alternativeSymbol = "_" + symbol;
            if (FindLibraryBySymbol(alternativeSymbol, out _))
            {
                symbol = alternativeSymbol;
                return true;
            }

            alternativeSymbol = symbol.TrimStart('_');
            if (FindLibraryBySymbol(alternativeSymbol, out _))
            {
                symbol = alternativeSymbol;
                return true;
            }

            alternativeSymbol = "_imp_" + symbol;
            if (FindLibraryBySymbol(alternativeSymbol, out _))
            {
                symbol = alternativeSymbol;
                return true;
            }

            alternativeSymbol = "__imp_" + symbol;
            if (FindLibraryBySymbol("__imp_" + symbol, out _))
            {
                symbol = alternativeSymbol;
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
