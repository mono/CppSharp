using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CodingSeb.ExpressionEvaluator;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
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

        /// <summary>
        /// Generate custom code here.
        /// </summary>
        void GenerateCode(Driver driver, List<GeneratorOutput> outputs) { }
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

        /// <summary>
        /// Constructs an <see cref="Enumeration.Item"/> by parsing the <see
        /// cref="MacroDefinition.Expression"/> in <paramref name="macro"/>. 
        /// </summary>
        /// <remarks>
        /// As a side effect, updates <see cref="Enumeration.Type"/> and <see
        /// cref="Enumeration.BuiltinType"/> in <paramref name="@enum"/> to reflect an appropriate 
        /// primitive type that can hold the newly-parsed <paramref name="macro"/> expression value 
        /// as well as all previously discovered <see cref="Enumeration.Items"/>. The intent is to
        /// preserve sign information about the values held in <paramref name="enum"/>.
        /// </remarks>
        public static Enumeration.Item GenerateEnumItemFromMacro(this Enumeration @enum,
            MacroDefinition macro)
        {
            var (value, type) = ParseEnumItemMacroExpression(macro, @enum);
            if (type > @enum.BuiltinType.Type)
            {
                @enum.BuiltinType = new BuiltinType(type);
                @enum.Type = @enum.BuiltinType;
            }
            return new Enumeration.Item
            {
                Name = macro.Name,
                Expression = macro.Expression,
                Value = value,
                Namespace = @enum
            };
        }

        static object EvaluateEnumExpression(string expr, Enumeration @enum)
        {
            // Include values of past items. Convert values to reflect the current
            // estimate of the PrimitiveType. In particular, recover sign information.
            var evaluator = new ExpressionEvaluator(@enum.Items.ToDictionary(
                item => item.Name,
                item => @enum.BuiltinType.Type switch
                        {
                            PrimitiveType.SChar => (sbyte)item.Value,
                            PrimitiveType.Short => (short)item.Value,
                            PrimitiveType.Int => (int)item.Value,
                            PrimitiveType.LongLong => (long)item.Value,
                            PrimitiveType.UChar => (byte)item.Value,
                            PrimitiveType.UShort => (ushort)item.Value,
                            PrimitiveType.UInt => (uint)item.Value,
                            PrimitiveType.ULongLong => (object)item.Value,
                            _ => item.Value
                        }
            ));

            expr = expr.ReplaceLineBreaks(" ").Replace('\\', ' ');
            return evaluator.Evaluate(expr);
        }

        /// <summary>
        /// Parse an enum value from a macro expression.
        /// </summary>
        /// <returns>
        /// The result of the expression evaluation cast to a ulong together with the primitive type
        /// of the result as returned from the expression evaluator. Note that the expression
        /// evaluator biases towards returning <see cref="int"/> values: it doesn't attempt to
        /// discover that a value could be stored in a <see cref="byte"/>.
        /// </returns>
        static (ulong value, PrimitiveType type) ParseEnumItemMacroExpression(MacroDefinition macro, Enumeration @enum)
        {
            var expression = macro.Expression;

            // The expression evaluator already handles character and string expressions. No need to
            // special case them here.
            //
            // TODO: Handle string expressions
            //if (expression.Length == 3 && expression[0] == '\'' && expression[2] == '\'') // '0' || 'A'
            //    return expression[1]; // return the ASCII code of this character

            try
            {
                var eval = EvaluateEnumExpression(expression, @enum);
                // Verify we have some sort of integer type. Enums can have negative values: record
                // that fact so that we can set the underlying primitive type correctly in the enum
                // item.
                switch (System.Type.GetTypeCode(eval.GetType()))
                {
                    // Must unbox eval which is typed as object to be able to cast it to a ulong.
                    case TypeCode.SByte: return ((ulong)(sbyte)eval, PrimitiveType.SChar);
                    case TypeCode.Int16: return ((ulong)(short)eval, PrimitiveType.Short);
                    case TypeCode.Int32: return ((ulong)(int)eval, PrimitiveType.Int);
                    case TypeCode.Int64: return ((ulong)(long)eval, PrimitiveType.LongLong);

                    case TypeCode.Byte: return ((byte)eval, PrimitiveType.UChar);
                    case TypeCode.UInt16: return ((ushort)eval, PrimitiveType.UShort);
                    case TypeCode.UInt32: return ((uint)eval, PrimitiveType.UInt);
                    case TypeCode.UInt64: return ((ulong)eval, PrimitiveType.ULongLong);

                    case TypeCode.Char: return ((char)eval, PrimitiveType.UShort);
                    // C++ allows booleans as enum values - they're translated to 1, 0.
                    case TypeCode.Boolean: return ((bool)eval ? 1UL : 0UL, PrimitiveType.UChar);

                    default: throw new Exception($"Expression {expression} is not a valid expression type for an enum");
                }
            }
            catch (Exception ex)
            {
                // TODO: This should just throw, but we have a pre-existing behavior that expects malformed
                // macro expressions to default to 0, see CSharp.h (MY_MACRO_TEST2_0), so do it for now.

                // Like other paths, we can however, write a diagnostic message to the console.

                Diagnostics.PushIndent();
                Diagnostics.Warning($"Unable to translate macro '{macro.Name}' to en enum value: {ex.Message}");
                Diagnostics.PopIndent();

                return (0, PrimitiveType.Int);
            }
        }

        class CollectEnumItems : AstVisitor
        {
            public List<Enumeration.Item> Items;
            private Regex regex;

            public CollectEnumItems(Regex regex)
            {
                Items = new List<Enumeration.Item>();
                this.regex = regex;
            }

            public override bool VisitEnumItemDecl(Enumeration.Item item)
            {
                if (AlreadyVisited(item))
                    return true;

                // Prevents collecting previously generated items when generating enums from macros.
                if (item.IsImplicit)
                    return true;

                var match = regex.Match(item.Name);
                if (match.Success)
                    Items.Add(item);

                return true;
            }
        }

        public static List<Enumeration.Item> FindMatchingEnumItems(this ASTContext context, Regex regex)
        {
            var collector = new CollectEnumItems(regex);
            foreach (var unit in context.TranslationUnits)
                collector.VisitTranslationUnit(unit);

            return collector.Items;
        }

        public static List<MacroDefinition> FindMatchingMacros(this ASTContext context,
            Regex regex, ref TranslationUnit unitToAttach)
        {
            int maxItems = 0;
            var macroDefinitions = new List<MacroDefinition>();

            foreach (var unit in context.TranslationUnits)
            {
                int numItems = 0;

                foreach (var macro in unit.PreprocessedEntities.OfType<MacroDefinition>())
                {
                    var match = regex.Match(macro.Name);
                    if (!match.Success)
                        continue;

                    // if (macro.Enumeration != null)
                    //     continue;

                    if (macroDefinitions.Exists(m => macro.Name == m.Name))
                        continue;

                    macroDefinitions.Add(macro);
                    numItems++;
                }

                if (numItems > maxItems)
                {
                    maxItems = numItems;
                    unitToAttach = unit;
                }
            }

            return macroDefinitions;
        }

        public static Enumeration GenerateEnumFromMacros(this ASTContext context, string name,
            params string[] values)
        {
            TranslationUnit GetUnitFromItems(List<Enumeration.Item> list)
            {
                var units = list.Select(item => item.TranslationUnit)
                    .GroupBy(x => x)
                    .Select(y => new { Element = y.Key, Counter = y.Count() });

                var translationUnit = units.OrderByDescending(u => u.Counter)
                    .FirstOrDefault()?.Element ?? null;

                return translationUnit;
            }

            var @enum = new Enumeration { Name = name };

            var regexPattern = string.Join("|", values.Select(pattern => $"({pattern}$)"));
            var regex = new Regex(regexPattern);

            var items = FindMatchingEnumItems(context, regex);
            foreach (var item in items)
                @enum.AddItem(item);

            TranslationUnit macroUnit = null;
            var macroDefs = FindMatchingMacros(context, regex, ref macroUnit);
            foreach (var macro in macroDefs)
            {
                // Skip this macro if the enum already has an item with same name.
                if (@enum.Items.Exists(it => it.Name == macro.Name))
                    continue;

                var item = @enum.GenerateEnumItemFromMacro(macro);
                @enum.AddItem(item);
                macro.Enumeration = @enum;
            }

            if (@enum.Items.Count <= 0)
                return @enum;

            // The value of @enum.BuiltinType has been adjusted on the fly via
            // GenerateEnumItemFromMacro. However, its notion of PrimitiveType corresponds with
            // what the ExpressionEvaluator returns. In particular, the expression "1" will result
            // in PrimitiveType.Int from the expression evaluator. Narrow the type to account for
            // types narrower than int.
            // 
            // Note that there is no guarantee that all items can be held in any builtin type. For
            // example, some value > long.MaxValue and some negative value. That case will result in
            // compile errors of the generated code. If needed, we could detect it and print a
            // diagnostic.
            ulong maxValue;
            long minValue;
            if (@enum.BuiltinType.IsUnsigned)
            {
                maxValue = @enum.Items.Max(i => i.Value);
                minValue = 0;
            }
            else
            {
                maxValue = (ulong)@enum.Items.Max(i => Math.Max(0, unchecked((long)i.Value)));
                minValue = @enum.Items.Min(i => unchecked((long)i.Value));
            }

            @enum.BuiltinType = new BuiltinType(GetUnderlyingTypeForEnumValue(maxValue, minValue));
            @enum.Type = @enum.BuiltinType;

            var unit = macroUnit ?? GetUnitFromItems(items);
            @enum.Namespace = unit;
            unit.Declarations.Add(@enum);

            return @enum;
        }

        private static PrimitiveType GetUnderlyingTypeForEnumValue(ulong maxValue, long minValue)
        {
            if (maxValue <= (ulong)sbyte.MaxValue && minValue >= sbyte.MinValue)
                // Previously, returned PrimitiveType.SChar. But that type doesn't seem to be well-supported elsewhere.
                // Bias towards UChar (byte), since that's what would normally be used in C#.
                return minValue >= 0 ? PrimitiveType.UChar : PrimitiveType.SChar;

            if (maxValue <= byte.MaxValue && minValue >= 0)
                return PrimitiveType.UChar;

            if (maxValue <= (ulong)short.MaxValue && minValue >= short.MinValue)
                return PrimitiveType.Short;

            if (maxValue <= ushort.MaxValue && minValue >= 0)
                return PrimitiveType.UShort;

            if (maxValue <= int.MaxValue && minValue >= int.MinValue)
                return PrimitiveType.Int;

            if (maxValue <= uint.MaxValue && minValue >= 0)
                return PrimitiveType.UInt;

            if (maxValue <= long.MaxValue && minValue >= long.MinValue)
                // Previously returned Long. However, LongLong is unambiguous and seems to be supported
                // wherever PrimitiveType.Long is
                return PrimitiveType.LongLong;

            if (maxValue <= ulong.MaxValue && minValue >= 0)
                // Previously returned ULong. However, ULongLong is unambiguous and seems to be supported
                // wherever PrimitiveType.ULong is
                return PrimitiveType.ULongLong; // Same as above.

            throw new NotImplementedException($"Cannot identify an integral underlying enum type supporting a maximum value of {maxValue} and a minimum value of {minValue}.");
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
            if (parameterIndex <= 0)
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
                .SelectMany(module => module.FindFunction(name))
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
            foreach (var pattern in patterns)
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
