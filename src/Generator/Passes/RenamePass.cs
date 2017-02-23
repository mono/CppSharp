﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// Base class for transform that perform renames of declarations.
    /// </summary>
    public abstract class RenamePass : TranslationUnitPass
    {
        public class ParameterComparer : IEqualityComparer<Parameter>
        {
            public bool Equals(Parameter x, Parameter y)
            {
                return x.QualifiedType == y.QualifiedType && x.GenerationKind == y.GenerationKind;
            }

            public int GetHashCode(Parameter obj)
            {
                return obj.Type.GetHashCode();
            }
        }

        public RenameTargets Targets = RenameTargets.Any;

        protected RenamePass()
        {
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitTemplateArguments = false;
            // properties need to be visited but in a different order (see VisitClassDecl) so disable the default order
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitClassMethods = false;
        }

        protected RenamePass(RenameTargets targets)
            : this()
        {
            Targets = targets;
        }

        public virtual bool Rename(Declaration decl, out string newName)
        {
            var method = decl as Method;
            if (method != null && !method.IsStatic)
            {
                var rootBaseMethod = ((Class) method.Namespace).GetBaseMethod(method);
                if (rootBaseMethod != null && rootBaseMethod != method)
                {
                    newName = rootBaseMethod.Name;
                    return true;
                }
            }

            var property = decl as Property;
            if (property != null && !property.IsStatic)
            {
                var rootBaseProperty = ((Class) property.Namespace).GetBaseProperty(property);
                if (rootBaseProperty != null && rootBaseProperty != property)
                {
                    newName = rootBaseProperty.Name;
                    return true;
                }
            }

            newName = decl.Name;
            return false;
        }

        public bool IsRenameableDecl(Declaration decl)
        {
            if (decl is Class) return true;
            var function = decl as Function;
            if (function != null)
            {
                // special case the IDisposable.Dispose that could be added later
                return !function.IsOperator && function.Name != "dispose";
            }
            if (decl is Parameter) return true;
            if (decl is Enumeration) return true;
            var property = decl as Property;
            if (property != null) return !property.IsIndexer;
            if (decl is Event) return true;
            if (decl is TypedefDecl) return true;
            if (decl is Namespace && !(decl is TranslationUnit)) return true;
            if (decl is Variable) return true;
            var field = decl as Field;
            if (field != null)
            {
                var fieldProperty = ((Class) field.Namespace).Properties.FirstOrDefault(
                    p => p.Field == field);
                return (fieldProperty != null &&
                    fieldProperty.IsInRefTypeAndBackedByValueClassField());
            }
            return false;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (AlreadyVisited(decl))
                return false;

            if (!IsRenameableDecl(decl))
                return true;

            if (decl.Name == null)
                return true;

            return Rename(decl);
        }

        private bool Rename(Declaration decl)
        {
            string newName;
            if (Rename(decl, out newName) && !AreThereConflicts(decl, newName))
                decl.Name = newName;
            return true;
        }

        private static bool AreThereConflicts(Declaration decl, string newName)
        {
            var declarations = new List<Declaration>();
            declarations.AddRange(decl.Namespace.Classes.Where(c => !c.IsIncomplete));
            declarations.AddRange(decl.Namespace.Enums);
            declarations.AddRange(decl.Namespace.Events);
            var function = decl as Function;
            if (function != null && function.SynthKind != FunctionSynthKind.AdjustedMethod)
            {
                declarations.Add(function.Namespace);
                // account for overloads
                declarations.AddRange(GetFunctionsWithTheSameParams(function));
            }
            else
                declarations.AddRange(decl.Namespace.Functions);
            declarations.AddRange(decl.Namespace.Variables);
            declarations.AddRange(from typedefDecl in decl.Namespace.Typedefs
                                  let pointerType = typedefDecl.Type.Desugar() as PointerType
                                  where pointerType != null && pointerType.GetFinalPointee() is FunctionType
                                  select typedefDecl);
            var specialization = decl as ClassTemplateSpecialization;
            if (specialization != null)
                declarations.RemoveAll(d => specialization.TemplatedDecl.TemplatedDecl == d);

            var @class = decl.Namespace as Class;
            if (@class != null && @class.IsDependent)
                declarations.AddRange(@class.TemplateParameters);

            var result = declarations.Any(d => d != decl && d.Name == newName);
            if (result)
                return true;

            if (decl is Method && decl.IsGenerated)
                return @class.GetPropertyByName(newName) != null;

            var property = decl as Property;
            if (property != null && property.Field != null)
                return ((Class) decl.Namespace).Properties.FirstOrDefault(
                    p => p != decl && p.Name == newName) != null;

            return false;
        }

        private static IEnumerable<Function> GetFunctionsWithTheSameParams(Function function)
        {
            var method = function as Method;
            if (method != null)
            {
                return ((Class) method.Namespace).Methods.Where(
                    m => !m.Ignore && m.Parameters.SequenceEqual(function.Parameters, new ParameterComparer()));
            }
            return function.Namespace.Functions.Where(
                f => !f.Ignore && f.Parameters.SequenceEqual(function.Parameters, new ParameterComparer()));
        }

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            if (!Targets.HasFlag(RenameTargets.EnumItem))
                return false;

            string newName;
            if (Rename(item, out newName))
            {
                item.Name = newName;
                return true;
            }

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var result = base.VisitClassDecl(@class);

            foreach (var property in @class.Properties.OrderByDescending(p => p.Access))
                VisitProperty(property);

            foreach (var method in @class.Methods)
                VisitMethodDecl(method);

            return result;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!Targets.HasFlag(RenameTargets.Field))
                return false;

            return base.VisitFieldDecl(field);
        }

        public override bool VisitProperty(Property property)
        {
            if (!Targets.HasFlag(RenameTargets.Property))
                return false;

            return base.VisitProperty(property);
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (!Targets.HasFlag(RenameTargets.Delegate))
                return false;

            return base.VisitTypedefDecl(typedef);
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!Targets.HasFlag(RenameTargets.Method))
                return false;

            if (method.Kind != CXXMethodKind.Normal)
                return false;

            return base.VisitMethodDecl(method);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!Targets.HasFlag(RenameTargets.Function))
                return false;

            return base.VisitFunctionDecl(function);
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (!Targets.HasFlag(RenameTargets.Parameter))
                return false;

            return base.VisitParameterDecl(parameter);
        }

        public override bool VisitEvent(Event @event)
        {
            if (!Targets.HasFlag(RenameTargets.Event))
                return false;

            return base.VisitEvent(@event);
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!Targets.HasFlag(RenameTargets.Variable))
                return false;

            return base.VisitVariableDecl(variable);
        }
    }

    [Flags]
    public enum RenameTargets
    {
        Class     = 1 << 0,
        Field     = 1 << 1,
        Method    = 1 << 2,
        Function  = 1 << 3,
        Parameter = 1 << 4,
        Enum      = 1 << 5,
        EnumItem  = 1 << 6,
        Event     = 1 << 7,
        Property  = 1 << 8,
        Delegate  = 1 << 9,
        Variable  = 1 << 10,
        Any = Function | Method | Parameter | Class | Field | Enum | EnumItem | Event | Property | Delegate | Variable
    }

    /// <summary>
    /// Renames a declaration based on a regular expression pattern.
    /// </summary>
    public class RegexRenamePass : RenamePass
    {
        public string Pattern;
        public string Replacement;

        public RegexRenamePass(string pattern, string replacement)
        {
            Pattern = pattern;
            Replacement = replacement;
        }

        public RegexRenamePass(string pattern, string replacement,
                                    RenameTargets targets)
            : this(pattern, replacement)
        {
            Targets = targets;
        }

        public override bool Rename(Declaration decl, out string newName)
        {
            if (base.Rename(decl, out newName)) return true;

            var replace = Regex.Replace(decl.Name, Pattern, Replacement);

            if (!decl.Name.Equals(replace))
            {
                newName = replace;
                return true;
            }

            newName = null;
            return false;
        }
    }

    public enum RenameCasePattern
    {
        UpperCamelCase,
        LowerCamelCase
    }

    /// <summary>
    /// Renames a declaration based on a regular expression pattern.
    /// </summary>
    public class CaseRenamePass : RenamePass
    {
        public RenameCasePattern Pattern;

        public CaseRenamePass(RenameTargets targets, RenameCasePattern pattern)
            : base(targets)
        {
            Pattern = pattern;
        }

        public override bool Rename(Declaration decl, out string newName)
        {
            if (base.Rename(decl, out newName)) return true;

            newName = ConvertCaseString(decl, Pattern);
            return true;
        }

        /// <summary>
        /// Converts the phrase to specified convention.
        /// </summary>
        /// <param name="decl"></param>
        /// <param name="pattern">The cases.</param>
        /// <returns>string</returns>
        static string ConvertCaseString(Declaration decl, RenameCasePattern pattern)
        {
            if (decl.Name.All(c => !char.IsLetter(c)))
                return decl.Name;

            var typedef = decl as TypedefDecl;
            if (typedef != null && typedef.IsSynthetized)
                return decl.Name;

            var property = decl as Property;
            if (property != null && property.GetMethod != null &&
                property.GetMethod.SynthKind == FunctionSynthKind.InterfaceInstance)
                return decl.Name;

            var sb = new StringBuilder(decl.Name);
            // check if it's been renamed to avoid a keyword
            if (sb[0] == '@')
                sb.Remove(0, 1);

            // do not remove underscores from ALL_CAPS names
            if (!decl.Name.Where(char.IsLetter).All(char.IsUpper))
            {
                for (int i = sb.Length - 1; i >= 0; i--)
                {
                    if (sb[i] == '_' && i < sb.Length - 1)
                    {
                        sb[i + 1] = char.ToUpperInvariant(sb[i + 1]);
                        sb.Remove(i, 1);
                    }
                }
            }

            var @class = decl as Class;
            switch (pattern)
            {
                case RenameCasePattern.UpperCamelCase:
                    sb[0] = char.ToUpperInvariant(sb[0]);
                    if (@class != null && @class.Type == ClassType.Interface)
                        sb[1] = char.ToUpperInvariant(sb[1]);
                    break;
                case RenameCasePattern.LowerCamelCase:
                    sb[0] = char.ToLowerInvariant(sb[0]);
                    if (@class != null && @class.Type == ClassType.Interface)
                        sb[1] = char.ToLowerInvariant(sb[1]);
                    break;
            }

            return sb.ToString();
        }
    }

    public static class RenamePassExtensions
    {
        public static void RenameWithPattern(this PassBuilder<TranslationUnitPass> builder,
            string pattern, string replacement, RenameTargets targets)
        {
            builder.AddPass(new RegexRenamePass(pattern, replacement, targets));
        }

        public static void RemovePrefix(this PassBuilder<TranslationUnitPass> builder, string prefix,
            RenameTargets targets = RenameTargets.Any)
        {
            builder.AddPass(new RegexRenamePass("^" + prefix, string.Empty,
                targets));
        }

        public static void RenameDeclsCase(this PassBuilder<TranslationUnitPass> builder, 
            RenameTargets targets, RenameCasePattern pattern)
        {
            builder.AddPass(new CaseRenamePass(targets, pattern));
        }

        public static void RenameDeclsUpperCase(this PassBuilder<TranslationUnitPass> builder,
            RenameTargets targets)
        {
            builder.AddPass(new CaseRenamePass(targets,
                RenameCasePattern.UpperCamelCase));
        }

        public static void RenameDeclsLowerCase(this PassBuilder<TranslationUnitPass> builder,
            RenameTargets targets)
        {
            builder.AddPass(new CaseRenamePass(targets,
                RenameCasePattern.LowerCamelCase));
        }
    }
}
