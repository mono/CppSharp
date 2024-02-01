using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CppSharp.AST
{
    public enum CppAbi
    {
        Itanium,
        Microsoft,
        ARM,
        AArch64,
        iOS,
        iOS64,
        WebAssembly
    }

    /// <summary>
    /// An AST context is a container for translation units.
    /// </summary>
    public class ASTContext
    {
        public List<TranslationUnit> TranslationUnits;

        public ASTContext()
        {
            TranslationUnits = new List<TranslationUnit>();
        }

        static private string NormalizeTranslationUnitFilePath(string name)
        {
            if (name.StartsWith("<"))
                return name;

            try
            {
                name = Path.GetFullPath(name);
            }
            catch (ArgumentException)
            {
                // Normalization errors are expected when dealing with virtual
                // compiler files like <built-in>.
            }

            return name;
        }

        /// Finds an existing or creates a new one translation unit given a path.
        public TranslationUnit FindOrCreateTranslationUnit(string file)
        {
            file = NormalizeTranslationUnitFilePath(file);
            var translationUnit = TranslationUnits.Find(m => m.FilePath.Equals(file));

            if (translationUnit == null)
            {
                translationUnit = new TranslationUnit(file);
                TranslationUnits.Add(translationUnit);
            }

            return translationUnit;
        }

        /// Finds an existing enum in the library modules.
        public IEnumerable<Enumeration> FindEnum(string name)
        {
            return TranslationUnits.Select(
                module => module.FindEnum(name)).Where(type => type != null);
        }

        /// Finds the complete declaration of an enum.
        public Enumeration FindCompleteEnum(string name)
        {
            return FindEnum(name).FirstOrDefault(@enum => !@enum.IsIncomplete);
        }

        /// Finds an existing struct/class in the library modules.
        public IEnumerable<Class> FindClass(string name, bool create = false,
            bool ignoreCase = false)
        {
            return TranslationUnits.Select(
                module => module.FindClass(name,
                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .Where(type => type != null);
        }

        /// Finds the complete declaration of a class.
        public Class FindCompleteClass(string name, bool ignoreCase = false)
        {
            return FindClass(name, ignoreCase: ignoreCase).FirstOrDefault(
                @class => !@class.IsIncomplete);
        }

        /// Finds an existing function in the library modules.
        public IEnumerable<Function> FindFunction(string name)
        {
            return TranslationUnits.SelectMany(module => module.FindFunction(name));
        }

        /// Finds an existing typedef in the library modules.
        public IEnumerable<TypedefNameDecl> FindTypedef(string name)
        {
            return TranslationUnits.Select(module => module.FindTypedef(name))
                .Where(type => type != null);
        }

        /// Finds an existing declaration by name.
        public IEnumerable<T> FindDecl<T>(string name) where T : Declaration
        {
            switch (typeof(T).Name)
            {
                case "Enumeration":
                    foreach (var module in TranslationUnits)
                        yield return module.FindEnum(name) as T;
                    break;
                case "Class":
                    foreach (var module in TranslationUnits)
                        yield return module.FindClass(name) as T;
                    break;
                case "ClassTemplate":
                    foreach (var module in TranslationUnits)
                        foreach (var template in module.FindClassTemplate(name))
                            yield return template as T;
                    break;
                case "Function":
                    foreach (var module in TranslationUnits)
                        yield return module.FindFunction(name) as T;
                    break;
                case "Variable":
                    foreach (var variable in from unit in TranslationUnits
                                             from variable in unit.Variables
                                             where variable.Name == name
                                             select variable)
                        yield return variable as T;
                    break;
                default:
                    foreach (var decl in from unit in TranslationUnits
                                         from decl in unit.Declarations
                                         where decl.Name == name
                                         select decl)
                        yield return decl as T;
                    break;
            }
        }

        public void SetEnumAsFlags(string name)
        {
            var enums = FindEnum(name);
            foreach (var @enum in enums)
                @enum.SetFlags();
        }

        public void ExcludeFromPass(string name, System.Type type)
        {
            var decls = FindDecl<Declaration>(name);

            foreach (var decl in decls)
                decl.ExcludeFromPasses.Add(type);
        }

        /// <summary>
        /// Use this to rename namespaces.
        /// </summary>
        public void RenameNamespace(string name, string newName)
        {
            foreach (var unit in TranslationUnits)
            {
                var @namespace = unit.FindNamespace(name);

                if (@namespace != null)
                    @namespace.Name = newName;
            }
        }
    }
}