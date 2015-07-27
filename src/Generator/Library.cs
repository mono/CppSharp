using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.AST;

namespace CppSharp
{
    /// <summary>
    /// Used to massage the library types into something more .NET friendly.
    /// </summary>
    public interface ILibrary
    {
        /// <summary>
        /// Do transformations that should happen before passes are processed.
        /// </summary>
        void Preprocess(Driver driver, ASTContext ctx);

        /// <summary>
        /// Do transformations that should happen after passes are processed.
        /// </summary>
        void Postprocess(Driver driver, ASTContext ctx);

        /// <summary>
        /// Setup the driver options here.
        /// </summary>
        void Setup(Driver driver);

        /// <summary>
        /// Setup your passes here.
        /// </summary>
        /// <param name="driver"></param>
        void SetupPasses(Driver driver);
    }

    public static class LibraryHelpers
    {
        #region Enum Helpers

        public static Enumeration FindEnum(this ASTContext context, string name)
        {
            foreach (var unit in context.TranslationUnits)
            {
                var @enum = unit.FindEnum(name);
                if (@enum != null)
                    return @enum;
            }

            return null;
        }

        public static void IgnoreEnumWithMatchingItem(this ASTContext context, string pattern)
        {
            Enumeration @enum = context.GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.ExplicitlyIgnore();
        }

        public static void SetNameOfEnumWithMatchingItem(this ASTContext context, string pattern,
            string name)
        {
            Enumeration @enum = context.GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.Name = name;
        }

        public static void SetNameOfEnumWithName(this ASTContext context, string enumName,
            string name)
        {
            foreach (var @enum in context.FindEnum(enumName))
            {
                if (@enum != null)
                    @enum.Name = name;
            }
        }

        public static Enumeration GetEnumWithMatchingItem(this ASTContext context, string pattern)
        {
            foreach (var module in context.TranslationUnits)
            {
                Enumeration @enum = module.FindEnumWithItem(pattern);
                if (@enum == null) continue;
                return @enum;
            }

            return null;
        }

        public static Enumeration.Item GenerateEnumItemFromMacro(this ASTContext context,
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

        static ulong ParseMacroExpression(string expression)
        {
            // TODO: Handle string expressions

            long val;
            return ParseToNumber(expression, out val) ? (ulong)val : 0;
        }

        public static Enumeration GenerateEnumFromMacros(this ASTContext context, string name,
            params string[] macros)
        {
            var @enum = new Enumeration { Name = name };

            var pattern = string.Join("|", macros);
            var regex = new Regex(pattern);

            foreach (var unit in context.TranslationUnits)
            {
                foreach (var macro in unit.PreprocessedEntities.OfType<MacroDefinition>())
                {
                    var match = regex.Match(macro.Name);
                    if (!match.Success) continue;

                    if (macro.Enumeration != null)
                        continue;

                    // Skip this macro if the enum already has an item with same entry.
                    if (@enum.Items.Exists(it => it.Name == macro.Name))
                        continue;

                    var item = GenerateEnumItemFromMacro(context, macro);
                    @enum.AddItem(item);

                    macro.Enumeration = @enum;
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

        public static IEnumerable<Class> FindClass(this ASTContext context, string name)
        {
            foreach (var module in context.TranslationUnits)
            {
                var @class = module.FindClass(name);
                if (@class != null)
                    yield return @class;
            }
        }

        public static void SetClassBindName(this ASTContext context, string className, string name)
        {
            foreach (var @class in context.FindClass(className))
                @class.Name = name;
        }

        public static void SetClassAsValueType(this ASTContext context, string className)
        {
            foreach (var @class in context.FindClass(className))
                @class.Type = ClassType.ValueType;
        }

        public static void IgnoreClassWithName(this ASTContext context, string name)
        {
            foreach (var @class in context.FindClass(name))
                @class.GenerationKind = GenerationKind.Internal;
        }

        public static void SetClassAsOpaque(this ASTContext context, string name)
        {
            foreach (var @class in context.FindClass(name))
                @class.IsOpaque = true;
        }

        public static void SetNameOfClassMethod(this ASTContext context, string name,
            string methodName, string newMethodName)
        {
            foreach (var @class in context.FindClass(name))
            {
                var method = @class.Methods.Find(m => m.Name == methodName);
                if (method != null)
                    method.Name = newMethodName;
            }
        }

        public static void SetPropertyAsReadOnly(this ASTContext context, string className, string propertyName)
        {
            var properties = context.FindClass(className)
                .SelectMany(c => c.Properties.Where(p => p.Name == propertyName && p.HasSetter));
            foreach (var property in properties)
                if (property.SetMethod != null)
                    property.SetMethod.GenerationKind = GenerationKind.None;
                else
                {
                    var field = property.Field;
                    var quals = field.QualifiedType.Qualifiers;
                    quals.IsConst = true;

                    var qualType = field.QualifiedType;
                    qualType.Qualifiers = quals;

                    field.QualifiedType = qualType;
                }
        }

        /// <summary>
        /// Sets the parameter usage for a function parameter.
        /// </summary>
        /// <param name="parameterIndex">first parameter has index 1</param>
        public static void SetFunctionParameterUsage(this ASTContext context,
            string functionName, int parameterIndex, ParameterUsage usage)
        {
            if (parameterIndex <= 0)
                throw new ArgumentException("parameterIndex");

            foreach (var function in context.FindFunction(functionName))
            {
                if (function.Parameters.Count < parameterIndex)
                    throw new ArgumentException("parameterIndex");

                function.Parameters[parameterIndex - 1].Usage = usage;
            }
        }

        /// <summary>
        /// Sets the parameter usage for a method parameter.
        /// </summary>
        /// <param name="parameterIndex">first parameter has index 1</param>
        public static void SetMethodParameterUsage(this ASTContext context,
            string className, string methodName, int parameterIndex, ParameterUsage usage)
        {
            if (parameterIndex <= 0 )
                 throw new ArgumentException("parameterIndex");

            var @class = context.FindCompleteClass(className);
            var method = @class.Methods.Find(m => m.Name == methodName);
            if (method == null)
                throw new ArgumentException("methodName");
            if (method.Parameters.Count < parameterIndex)
                throw new ArgumentException("parameterIndex");

            method.Parameters[parameterIndex - 1].Usage = usage;
        }

        public static void CopyClassFields(this ASTContext context, string source,
            string destination)
        {
            var @class = context.FindCompleteClass(source);
            var dest = context.FindCompleteClass(destination);

            dest.Fields.AddRange(@class.Fields);
            foreach (var field in dest.Fields)
                field.Namespace = dest;
        }

        #endregion

        #region Function Helpers

        public static IEnumerable<Function> FindFunction(this ASTContext context, string name)
        {
            return context.TranslationUnits
                .Select(module => module.FindFunction(name))
                .Where(function => function != null);
        }

        public static void IgnoreFunctionWithName(this ASTContext context, string name)
        {
            foreach (var function in context.FindFunction(name))
                function.ExplicitlyIgnore();
        }

        public static void IgnoreFunctionWithPattern(this ASTContext context, string pattern)
        {
            foreach (var unit in context.TranslationUnits)
            {
                foreach (var function in unit.Functions)
                {
                    if (Regex.Match(function.Name, pattern).Success)
                        function.ExplicitlyIgnore();
                }
            }
        }

        public static void SetNameOfFunction(this ASTContext context, string name, string newName)
        {
            foreach (var function in context.FindFunction(name))
                function.Name = newName;
        }

        public static void IgnoreClassMethodWithName(this ASTContext context, string className,
            string name)
        {
            foreach (var method in from @class in context.FindClass(className)
                                   from method in @class.Methods
                                   where method.Name == name
                                   select method)
            {
                method.ExplicitlyIgnore();
            }
        }

        public static void IgnoreClassField(this ASTContext context, string name, string field)
        {
            foreach (var @class in context.FindClass(name))
            {
                foreach (var classField in @class.Fields.FindAll(f => f.Name == field))
                    classField.ExplicitlyIgnore();
            }
        }

        #endregion

        #region Module Helpers

        public static void IgnoreHeadersWithName(this ASTContext context, IEnumerable<string> patterns)
        {
            foreach(var pattern in patterns)
                context.IgnoreHeadersWithName(pattern);
        }

        public static void IgnoreHeadersWithName(this ASTContext context, string pattern)
        {
            var units = context.TranslationUnits.FindAll(m =>
            {
                var hasMatch = Regex.Match(m.FilePath, pattern).Success;
                if (m.IncludePath != null)
                    hasMatch |= Regex.Match(m.IncludePath, pattern).Success;
                return hasMatch;
            });

            foreach (var unit in units)
            {
                unit.ExplicitlyIgnore();
            }
        }

        #endregion
    }
}