﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.Passes;

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
            return (from unit in context.TranslationUnits
                    let @enum = unit.FindEnumWithItem(pattern)
                    where @enum != null
                    select @enum).FirstOrDefault();
        }

        public static Enumeration.Item GenerateEnumItemFromMacro(this Enumeration @enum,
            MacroDefinition macro)
        {
            return new Enumeration.Item
                {
                    Name = macro.Name,
                    Expression = macro.Expression,
                    Value = ParseMacroExpression(macro.Expression, @enum),
                    Namespace = @enum
                };
        }

        static bool ParseToNumber(string num, Enumeration @enum, out long val)
        {
            //ExpressionEvaluator does not work with hex
            if (num.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                num = num.Substring(2);

                // This is in case the literal contains suffix
                num = Regex.Replace(num, "(?i)[ul]*$", String.Empty);

                return long.TryParse(num, NumberStyles.HexNumber,
                    CultureInfo.CurrentCulture, out val);
            }

            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            //Include values of past items
            evaluator.Variables = new Dictionary<string, object>();
            foreach (Enumeration.Item item in @enum.Items)
            {
                //ExpressionEvaluator is requires lowercase variables
                evaluator.Variables.Add(item.Name.ToLower(), item.Value);
            }
            try
            {
                var ret = evaluator.Evaluate("(long)" + num.ReplaceLineBreaks(" ").Replace('\\',' '));
                val = (long)ret;
                return true;
            }
            catch (ExpressionEvaluatorSyntaxErrorException)
            {
                val = 0;
                return false;
            }
        }

        static ulong ParseMacroExpression(string expression, Enumeration @enum)
        {
            // TODO: Handle string expressions
            if (expression.Length == 3 && expression[0] == '\'' && expression[2] == '\'') // '0' || 'A'
                return expression[1]; // return the ASCI code of this character

            long val;
            return ParseToNumber(expression, @enum, out val) ? (ulong)val : 0;
        }

        public static Enumeration GenerateEnumFromMacros(this ASTContext context, string name,
            params string[] macros)
        {
            var @enum = new Enumeration { Name = name };

            var regexPattern = string.Join("|", macros.Select(pattern => $"({pattern}$)"));
            var regex = new Regex(regexPattern);

            int maxItems = 0;
            TranslationUnit unitToAttach = null;
            ulong maxValue = 0;

            foreach (var unit in context.TranslationUnits)
            {
                int numItems = 0;

                foreach (var macro in unit.PreprocessedEntities.OfType<MacroDefinition>())
                {
                    var match = regex.Match(macro.Name);
                    if (!match.Success) continue;

                    if (macro.Enumeration != null)
                        continue;

                    // Skip this macro if the enum already has an item with same name.
                    if (@enum.Items.Exists(it => it.Name == macro.Name))
                        continue;

                    var item = @enum.GenerateEnumItemFromMacro(macro);
                    @enum.AddItem(item);
                    macro.Enumeration = @enum;

                    maxValue = Math.Max(maxValue, item.Value);
                    numItems++;
                }

                if (numItems > maxItems)
                {
                    maxItems = numItems;
                    unitToAttach = unit;
                }
            }

            if (@enum.Items.Count > 0)
            {
                @enum.BuiltinType = new BuiltinType(GetUnderlyingTypeForEnumValue(maxValue));
                @enum.Type = @enum.BuiltinType;
                @enum.Namespace = unitToAttach;

                unitToAttach.Declarations.Add(@enum);
            }

            return @enum;
        }

        private static PrimitiveType GetUnderlyingTypeForEnumValue(ulong maxValue)
        {
            if (maxValue < (byte)sbyte.MaxValue)
                return PrimitiveType.SChar;

            if (maxValue < byte.MaxValue)
                return PrimitiveType.UChar;

            if (maxValue < (ushort)short.MaxValue)
                return PrimitiveType.Short;

            if (maxValue < ushort.MaxValue)
                return PrimitiveType.UShort;

            if (maxValue < int.MaxValue)
                return PrimitiveType.Int;

            if (maxValue < uint.MaxValue)
                return PrimitiveType.UInt;

            throw new NotImplementedException();
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
                @class.ExplicitlyIgnore();
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
            SetMethodParameterUsage(context, className, methodName, -1, parameterIndex, usage);
        }

        /// <summary>
        /// Sets the parameter usage for a method parameter.
        /// </summary>
        /// <param name="parameterIndex">first parameter has index 1</param>
        public static void SetMethodParameterUsage(this ASTContext context,
            string className, string methodName, int parameterCount, int parameterIndex,
            ParameterUsage usage)
        {
            if (parameterIndex <= 0 )
                 throw new ArgumentException("parameterIndex");

            var @class = context.FindCompleteClass(className);

            Method method;

            if (parameterCount >= 0)
                method = @class.Methods.Find(m => m.Name == methodName
                                          && m.Parameters.Count == parameterCount);
            else
                method = @class.Methods.Find(m => m.Name == methodName);

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

        private static IEnumerable<Class> GetClasses(DeclarationContext decl)
        {
            foreach (var @class in decl.Classes)
            {
                yield return @class;

                foreach (var class2 in GetClasses(@class))
                    yield return class2;
            }

            foreach (var ns in decl.Namespaces)
            {
                foreach (var @class in GetClasses(ns))
                    yield return @class;
            }
        }

        public static void IgnoreConversionToProperty(this ASTContext context, string pattern)
        {
            foreach (var unit in context.TranslationUnits)
            {
                foreach (var @class in GetClasses(unit))
                {
                    foreach (var method in @class.Methods)
                    {
                        if (Regex.Match(method.QualifiedLogicalOriginalName, pattern).Success)
                            method.ExcludeFromPasses.Add(typeof(GetterSetterToPropertyPass));
                    }
                }
            }
        }

        public static void ForceConversionToProperty(this ASTContext context, string pattern)
        {
            foreach (var unit in context.TranslationUnits)
            {
                foreach (var @class in GetClasses(unit))
                {
                    foreach (var method in @class.Methods)
                    {
                        if (Regex.Match(method.QualifiedLogicalOriginalName, pattern).Success)
                            method.ConvertToProperty = true;
                    }
                }
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
                unit.GenerationKind = GenerationKind.None;
            }
        }

        public static void IgnoreTranslationUnits(this ASTContext context, IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                context.IgnoreHeadersWithName(pattern);
        }

        public static void IgnoreTranslationUnits(this ASTContext context)
        {
            foreach (var unit in context.TranslationUnits)
            {
                unit.GenerationKind = GenerationKind.None;
            }
        }

        public static void GenerateTranslationUnits(this ASTContext context, IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                context.GenerateTranslationUnits(pattern);
        }

        public static void GenerateTranslationUnits(this ASTContext context, string pattern)
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
                unit.GenerationKind = GenerationKind.Generate;
            }
        }

        #endregion
    }
}
