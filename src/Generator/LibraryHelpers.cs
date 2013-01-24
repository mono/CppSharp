using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Cxxi.Generators;

namespace Cxxi
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LibraryTransformAttribute : Attribute
    {
    }

    /// <summary>
    /// Used to massage the library types into something more .NET friendly.
    /// </summary>
    public interface ILibrary
    {
        /// <summary>
        /// Do transformations that should happen before processing here.
        /// </summary>
        void Preprocess(LibraryHelpers g);

        /// <summary>
        /// Do transformations that should happen after processing here.
        /// </summary>
        void Postprocess(LibraryHelpers g);

        /// <summary>
        /// Setup your passes here.
        /// </summary>
        /// <param name="passes"></param>
        void SetupPasses(PassBuilder passes);

        /// <summary>
        /// Setup your headers here.
        /// </summary>
        /// <param name="headers"></param>
        void SetupHeaders(List<string> headers);

        /// <summary>
        /// Called to generate text at the start of the text template.
        /// </summary>
        /// <param name="template"></param>
        void GenerateStart(TextTemplate template);

        /// <summary>
        /// Called to generate text after the generation of namespaces.
        /// </summary>
        /// <param name="template"></param>
        void GenerateAfterNamespaces(TextTemplate template);
    }

    public enum InlineMethods
    {
        Present,
        Unavailable
    }

    public class LibraryHelpers
    {
        private Library Library { get; set; }
     
        public LibraryHelpers(Library library)
        {
            Library = library;
        }

        #region Enum Helpers

        public Enumeration FindEnum(string name)
        {
            foreach (var unit in Library.TranslationUnits)
            {
                var @enum = unit.FindEnum(name);
                if (@enum != null)
                    return @enum;
            }

            return null;
        }

        public void IgnoreEnumWithMatchingItem(string pattern)
        {
            Enumeration @enum = GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.ExplicityIgnored = true;
        }

        public void SetNameOfEnumWithMatchingItem(string pattern, string name)
        {
            Enumeration @enum = GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.Name = name;
        }

        public void SetNameOfEnumWithName(string enumName, string name)
        {
            Enumeration @enum = FindEnum(enumName);
            if (@enum != null)
                @enum.Name = name;
        }

        public Enumeration GetEnumWithMatchingItem(string pattern)
        {
            foreach (var module in Library.TranslationUnits)
            {
                Enumeration @enum = module.FindEnumWithItem(pattern);
                if (@enum == null) continue;
                return @enum;
            }

            return null;
        }

        public Enumeration.Item GenerateEnumItemFromMacro(MacroDefinition macro)
        {
            var item = new Enumeration.Item
            {
                Name = macro.Name,
                Expression = macro.Expression,
                Value = ParseMacroExpression(macro.Expression)
            };

            return item;
        }

        static bool ParseToNumber(string num, out long val)
        {
            if (num.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                num = num.Substring(2);

                return long.TryParse(num, NumberStyles.HexNumber,
                    CultureInfo.CurrentCulture, out val);
            }

            return long.TryParse(num, out val);
        }

        static long ParseMacroExpression(string expression)
        {
            long val;
            if (ParseToNumber(expression, out val))
                return val;
            // TODO: Handle string expressions
            return 0;
        }

        public Enumeration GenerateEnumFromMacros(string name, params string[] macros)
        {
            var @enum = new Enumeration { Name = name };

            var pattern = string.Join("|", macros);
            var regex = new Regex(pattern);

            foreach (var unit in Library.TranslationUnits)
            {
                foreach (var macro in unit.Macros)
                {
                    var match = regex.Match(macro.Name);
                    if (!match.Success) continue;

                    var item = GenerateEnumItemFromMacro(macro);
                    @enum.AddItem(item);
                }

                if (@enum.Items.Count > 0)
                {
                    unit.Enums.Add(@enum);
                    break;
                }
            }

            return @enum;
        }

        #endregion

        #region Class Helpers

        public IEnumerable<Class> FindClass(string name)
        {
            foreach (var module in Library.TranslationUnits)
            {
                var @class = module.FindClass(name);
                if (@class != null)
                    yield return @class;
            }
        }

        public void SetClassBindName(string className, string name)
        {
            foreach (var @class in FindClass(className))
                @class.Name = name;
        }

        public void SetClassAsValueType(string className)
        {
            foreach (var @class in FindClass(className))
                @class.Type = ClassType.ValueType;
        }

        public void IgnoreClassWithName(string name)
        {
            foreach (var @class in FindClass(name))
                @class.ExplicityIgnored = true;
        }

        public void SetClassAsOpaque(string name)
        {
            foreach (var @class in FindClass(name))
                @class.IsOpaque = true;
        }

        public void SetNameOfClassMethod(string name, string methodName,
            string newMethodName)
        {
            foreach (var @class in FindClass(name))
            {
                var method = @class.Methods.Find(m => m.Name == methodName);
                if (method != null)
                    method.Name = newMethodName;
            }
        }

        #endregion

        #region Function Helpers

        public IEnumerable<Function> FindFunction(string name)
        {
            return Library.TranslationUnits
                .Select(module => module.FindFunction(name))
                .Where(function => function != null);
        }

        public void IgnoreFunctionWithName(string name)
        {
            Function function = FindFunction(name);
            if (function != null)
                function.ExplicityIgnored = true;
        }

        public void IgnoreClassMethodWithName(string className, string name)
        {
            foreach (var @class in FindClass(name))
            {
                var method = @class.Methods.Find(m => m.Name == name);

                if (method == null)
                    return;

                method.ExplicityIgnored = true;
            }
        }

        #endregion

        #region Module Helpers

        public void IgnoreModulessWithName(string pattern)
        {
            var modules = Library.TranslationUnits.FindAll(m =>
                Regex.Match(m.FilePath, pattern).Success);

            foreach (var module in modules)
            {
                module.ExplicityIgnored = true;
            }
        }

        #endregion
    }
}