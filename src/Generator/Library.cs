using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.Generators;

namespace CppSharp
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
        /// Do transformations that should happen before passes are processed.
        /// </summary>
        void Preprocess(Library lib);

        /// <summary>
        /// Do transformations that should happen after passes are processed.
        /// </summary>
        void Postprocess(Library lib);

        /// <summary>
        /// Setup the driver options here.
        /// </summary>
        void Setup(DriverOptions options);

        /// <summary>
        /// Setup your passes here.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="passes"></param>
        void SetupPasses(Driver driver, PassBuilder passes);
    }

    public static class LibraryHelpers
    {
        #region Enum Helpers

        public static Enumeration FindEnum(this Library library, string name)
        {
            foreach (var unit in library.TranslationUnits)
            {
                var @enum = unit.FindEnum(name);
                if (@enum != null)
                    return @enum;
            }

            return null;
        }

        public static void IgnoreEnumWithMatchingItem(this Library library, string pattern)
        {
            Enumeration @enum = library.GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.ExplicityIgnored = true;
        }

        public static void SetNameOfEnumWithMatchingItem(this Library library, string pattern,
            string name)
        {
            Enumeration @enum = library.GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.Name = name;
        }

        public static void SetNameOfEnumWithName(this Library library, string enumName,
            string name)
        {
            foreach (var @enum in library.FindEnum(enumName))
            {
                if (@enum != null)
                    @enum.Name = name;
            }
        }

        public static Enumeration GetEnumWithMatchingItem(this Library library, string pattern)
        {
            foreach (var module in library.TranslationUnits)
            {
                Enumeration @enum = module.FindEnumWithItem(pattern);
                if (@enum == null) continue;
                return @enum;
            }

            return null;
        }

        public static Enumeration.Item GenerateEnumItemFromMacro(this Library library,
            MacroDefinition macro)
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
            // TODO: Handle string expressions

            long val;
            return ParseToNumber(expression, out val) ? val : 0;
        }

        public static Enumeration GenerateEnumFromMacros(this Library library, string name,
            params string[] macros)
        {
            var @enum = new Enumeration { Name = name };

            var pattern = string.Join("|", macros);
            var regex = new Regex(pattern);

            foreach (var unit in library.TranslationUnits)
            {
                foreach (var macro in unit.Macros)
                {
                    var match = regex.Match(macro.Name);
                    if (!match.Success) continue;

                    var item = GenerateEnumItemFromMacro(library, macro);
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

        public static IEnumerable<Class> FindClass(this Library library, string name)
        {
            foreach (var module in library.TranslationUnits)
            {
                var @class = module.FindClass(name);
                if (@class != null)
                    yield return @class;
            }
        }

        public static void SetClassBindName(this Library library, string className, string name)
        {
            foreach (var @class in library.FindClass(className))
                @class.Name = name;
        }

        public static void SetClassAsValueType(this Library library, string className)
        {
            foreach (var @class in library.FindClass(className))
                @class.Type = ClassType.ValueType;
        }

        public static void IgnoreClassWithName(this Library library, string name)
        {
            foreach (var @class in library.FindClass(name))
                @class.ExplicityIgnored = true;
        }

        public static void SetClassAsOpaque(this Library library, string name)
        {
            foreach (var @class in library.FindClass(name))
                @class.IsOpaque = true;
        }

        public static void SetNameOfClassMethod(this Library library, string name,
            string methodName, string newMethodName)
        {
            foreach (var @class in library.FindClass(name))
            {
                var method = @class.Methods.Find(m => m.Name == methodName);
                if (method != null)
                    method.Name = newMethodName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterIndex">first parameter has index 1</param>
        public static void SetMethodParameterUsage(this Library library,
            string className, string methodName, int parameterIndex, ParameterUsage usage)
        {
            if (parameterIndex <= 0 )
                 throw new ArgumentException("parameterIndex");

            foreach (var @class in library.FindClass(className))
            {
                var method = @class.Methods.Find(m => m.Name == methodName);
                if (method == null)
                    throw new ArgumentException("methodName");
                if (method.Parameters.Count < parameterIndex)
                    throw new ArgumentException("parameterIndex");

                method.Parameters[parameterIndex - 1].Usage = usage;
            }
        }

        public static void CopyClassFields(this Library library, string source,
            string destination)
        {
            foreach (var @class in library.FindClass(source))
            {
                foreach (var dest in library.FindClass(destination))
                {
                    dest.Fields.AddRange(@class.Fields);
                    foreach (var field in dest.Fields)
                        field.Namespace = dest;
                }
            }
        }

        #endregion

        #region Function Helpers

        public static IEnumerable<Function> FindFunction(this Library library, string name)
        {
            return library.TranslationUnits
                .Select(module => module.FindFunction(name))
                .Where(function => function != null);
        }

        public static void IgnoreFunctionWithName(this Library library, string name)
        {
            foreach (var function in library.FindFunction(name))
                function.ExplicityIgnored = true;
        }

        public static void IgnoreFunctionWithPattern(this Library library, string pattern)
        {
            foreach (var unit in library.TranslationUnits)
            {
                foreach (var function in unit.Functions)
                {
                    if (Regex.Match(function.Name, pattern).Success)
                        function.ExplicityIgnored = true;
                }
            }
        }

        public static void SetNameOfFunction(this Library library, string name, string newName)
        {
            foreach (var function in library.FindFunction(name))
                function.Name = newName;
        }

        public static void IgnoreClassMethodWithName(this Library library, string className,
            string name)
        {
            foreach (var @class in library.FindClass(name))
            {
                var method = @class.Methods.Find(m => m.Name == name);

                if (method == null)
                    return;

                method.ExplicityIgnored = true;
            }
        }

        #endregion

        #region Module Helpers

        public static void IgnoreHeadersWithName(this Library library, IEnumerable<string> patterns)
        {
            foreach(var pattern in patterns)
                library.IgnoreHeadersWithName(pattern);
        }

        public static void IgnoreHeadersWithName(this Library library, string pattern)
        {
            var units = library.TranslationUnits.FindAll(m =>
            {
                var hasMatch = Regex.Match(m.FilePath, pattern).Success;
                if (m.IncludePath != null)
                    hasMatch |= Regex.Match(m.IncludePath, pattern).Success;
                return hasMatch;
            });

            foreach (var unit in units)
            {
                unit.IsGenerated = false;
                unit.IsProcessed = true;
                unit.ExplicityIgnored = true;
            }
        }

        #endregion
    }
}