using System;
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
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitTemplateArguments = false;
            // these need to be visited but in a different order (see VisitClassDecl) so disable the default order
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitNamespaceEvents = false;
        }

        protected RenamePass(RenameTargets targets)
            : this()
        {
            Targets = targets;
        }

        public virtual bool Rename(Declaration decl, out string newName)
        {
            if (decl is Method method && !method.IsStatic)
            {
                Method rootBaseMethod;
                if (method.OriginalNamespace is Class @class && @class.IsInterface)
                    rootBaseMethod = (Method) method.OriginalFunction;
                else
                    rootBaseMethod = method.GetRootBaseMethod();
                if (rootBaseMethod != null && rootBaseMethod != method)
                {
                    newName = rootBaseMethod.Name;
                    return true;
                }
            }

            if (decl is Property property && !property.IsStatic)
            {
                var rootBaseProperty = ((Class) property.Namespace).GetBasePropertyByName(property);
                if (rootBaseProperty != null && rootBaseProperty != property)
                {
                    newName = rootBaseProperty.Name;
                    return true;
                }
            }

            if (!(decl is Class) &&
                !string.IsNullOrEmpty(decl.Name) && AreThereConflicts(decl, decl.Name))
            {
                char initialLetter = char.IsUpper(decl.Name[0]) ?
                    char.ToLowerInvariant(decl.Name[0]) :
                    char.ToUpperInvariant(decl.Name[0]);
                newName = initialLetter + decl.Name.Substring(1);
                return true;
            }

            newName = decl.Name;
            return false;
        }

        public bool IsRenameableDecl(Declaration decl)
        {
            if (decl is Class)
                return Targets.HasFlag(RenameTargets.Class);

            var method = decl as Method;
            if (method != null)
            {
                return Targets.HasFlag(RenameTargets.Method) &&
                    method.Kind == CXXMethodKind.Normal &&
                    method.Name != "dispose";
            }

            var function = decl as Function;
            if (function != null)
            {
                // Special case the IDisposable.Dispose method.
                return Targets.HasFlag(RenameTargets.Function) &&
                    (!function.IsOperator && function.Name != "dispose");
            }

            if (decl is Parameter)
                return Targets.HasFlag(RenameTargets.Parameter);

            if (decl is Enumeration.Item)
                return Targets.HasFlag(RenameTargets.EnumItem);

            if (decl is Enumeration)
                return Targets.HasFlag(RenameTargets.Enum);

            var property = decl as Property;
            if (property != null)
                return Targets.HasFlag(RenameTargets.Property) && !property.IsIndexer;

            if (decl is Event)
                return Targets.HasFlag(RenameTargets.Event);

            if (decl is TypedefDecl)
                return Targets.HasFlag(RenameTargets.Delegate);

            if (decl is Namespace && !(decl is TranslationUnit)) return true;

            if (decl is Variable)
                return Targets.HasFlag(RenameTargets.Variable);

            var field = decl as Field;
            if (field != null)
            {
                if (!Targets.HasFlag(RenameTargets.Field))
                    return false;
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
            if (!Rename(decl, out newName) || AreThereConflicts(decl, newName))
                return false;
            
            decl.Name = newName;
            return true;
        }

        private static bool AreThereConflicts(Declaration decl, string newName)
        {
            if (decl is Parameter)
                return false;

            var declarations = new List<Declaration>();
            declarations.AddRange(decl.Namespace.Classes.Where(c => !c.IsIncomplete));
            declarations.AddRange(decl.Namespace.Enums);
            declarations.AddRange(decl.Namespace.Events);
            declarations.Add(decl.Namespace);

            var function = decl as Function;
            if (function != null)
                // account for overloads
                declarations.AddRange(GetFunctionsWithTheSameParams(function));
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
            if (@class != null)
            {
                declarations.AddRange(from typedefDecl in @class.Typedefs
                                      where typedefDecl.Type.Desugar() is FunctionType
                                      select typedefDecl);
                if (@class.IsDependent)
                    declarations.AddRange(@class.TemplateParameters);
            }

            var existing = declarations.Find(d => d != decl && d.Name == newName);
            if (existing != null)
                return CheckExisting(decl, existing);

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

        private static bool CheckExisting(Declaration decl, Declaration existing)
        {
            var method = decl as Method;
            var property = decl as Property;
            if (method?.IsOverride != true && property?.IsOverride != true)
                return true;

            existing.Name = existing.Name == existing.OriginalName ||
                string.IsNullOrEmpty(existing.OriginalName) ?
                existing.Name + "_" : existing.OriginalName;
            return false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            if (@class.OriginalClass != null)
                VisitClassDecl(@class.OriginalClass);

            foreach (var property in @class.Properties.OrderByDescending(p => p.Access))
                VisitProperty(property);

            foreach (var method in @class.Methods)
                VisitMethodDecl(method);

            foreach (var @event in @class.Events)
                VisitEvent(@event);

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            return VisitDeclaration(field);
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            return VisitDeclaration(parameter);
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
            if (base.Rename(decl, out newName))
                return true;

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
    /// Renames a declaration based on a pre-defined pattern.
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
            if (base.Rename(decl, out newName))
                return true;

            newName = ConvertCaseString(decl, Pattern);
            return true;
        }

        /// <summary>
        /// Converts the phrase to specified convention.
        /// </summary>
        /// <param name="decl"></param>
        /// <param name="pattern">The cases.</param>
        /// <returns>string</returns>
        public static string ConvertCaseString(Declaration decl, RenameCasePattern pattern)
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
            if (sb[0] == '@' || sb[0] == '$')
                sb.Remove(0, 1);

            RemoveUnderscores(sb);

            var @class = decl as Class;
            switch (pattern)
            {
                case RenameCasePattern.UpperCamelCase:
                    // ensure separation in enum items by not ending up with more capitals in a row than before
                    if (sb.Length == 1 || !char.IsUpper(sb[1]) || !(decl is Enumeration.Item))
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

        private static void RemoveUnderscores(StringBuilder sb)
        {
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                if (sb[i] != '_' ||
                    // lower case intentional if the first character is already upper case
                    (i + 1 < sb.Length && char.IsLower(sb[i + 1]) && char.IsUpper(sb[0])) ||
                    // don't end up with more capitals or digits in a row than before
                    (i > 0 && (char.IsUpper(sb[i - 1]) ||
                     (i < sb.Length - 1 && char.IsDigit(sb[i + 1]) && char.IsDigit(sb[i - 1])))))
                    continue;

                if (i < sb.Length - 1)
                    sb[i + 1] = char.ToUpperInvariant(sb[i + 1]);
                sb.Remove(i, 1);
            }
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
