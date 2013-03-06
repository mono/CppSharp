using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cxxi
{
    public enum CppAbi
    {
        Itanium,
        Microsoft,
        ARM
    }

    public enum CppInlineMethods
    {
        Present,
        Unavailable
    }

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

        /// If the module should be ignored.
        public override bool Ignore
        {
            get { return ExplicityIgnored; }
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

    /// <summary>
    /// A library contains all the modules.
    /// </summary>
    public class Library
    {
        public string Name;
        public string SharedLibrary;
        public List<TranslationUnit> TranslationUnits;

        public Library(string name, string sharedLibrary)
        {
            Name = name;
            SharedLibrary = sharedLibrary;
            TranslationUnits = new List<TranslationUnit>();
        }

        /// Finds an existing module or creates a new one given a file path.
        public TranslationUnit FindOrCreateModule(string file)
        {
            var module = TranslationUnits.Find(m => m.FilePath.Equals(file));

            if (module == null)
            {
                module = new TranslationUnit(file);
                TranslationUnits.Add(module);
            }

            return module;
        }

        /// Finds an existing enum in the library modules.
        public IEnumerable<Enumeration> FindEnum(string name)
        {
            foreach (var module in TranslationUnits)
            {
                var type = module.FindEnum(name);
                if (type != null) yield return type;
            }
        }

        /// Finds an existing struct/class in the library modules.
        public IEnumerable<Class> FindClass(string name, bool create = false)
        {
            foreach (var module in TranslationUnits)
            {
                var type = module.FindClass(name);
                if (type != null) yield return type;
            }
        }

        /// Finds the complete declaration of a class.
        public Class FindCompleteClass(string name)
        {
            foreach (var @class in FindClass(name))
                if (!@class.IsIncomplete)
                    return @class;

            return null;
        }

        /// Finds an existing function in the library modules.
        public IEnumerable<Function> FindFunction(string name)
        {
            foreach (var module in TranslationUnits)
            {
                var type = module.FindFunction(name);
                if (type != null) yield return type;
            }
        }


        /// Finds an existing typedef in the library modules.
        public IEnumerable<TypedefDecl> FindTypedef(string name)
        {
            foreach (var module in TranslationUnits)
            {
                var type = module.FindTypedef(name);
                if (type != null) yield return type;
            }
        }

        /// Finds an existing declaration by name.
        public IEnumerable<T> FindDecl<T>(string name) where T : Declaration
        {
            foreach (var module in TranslationUnits)
            {
                if (module.FindEnum(name) as T != null)
                    yield return module.FindEnum(name) as T;
                else if (module.FindClass(name) as T != null)
                    yield return module.FindClass(name) as T;
                else if (module.FindFunction(name) as T != null)
                    yield return module.FindFunction(name) as T;
            }

            yield return null;
        }

        public void SetEnumAsFlags(string name)
        {
            var enums = FindEnum(name);
            foreach(var @enum in enums)
                @enum.SetFlags();
        }
    }
}