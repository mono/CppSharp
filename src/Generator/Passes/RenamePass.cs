using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// Base class for transform that perform renames of declarations.
    /// </summary>
    public abstract class RenamePass : TranslationUnitPass
    {
        public RenameTargets Targets = RenameTargets.Any;

        protected RenamePass()
        {
        }

        protected RenamePass(RenameTargets targets)
        {
            Targets = targets;
        }

        public abstract bool Rename(string name, out string newName);

        public bool IsRenameableDecl(Declaration decl)
        {
            if (decl is Class) return true;
            if (decl is Field) return true;
            var function = decl as Function;
            if (function != null)
            {
                // special case the IDisposable.Dispose that could be added later
                return !function.IsOperator && function.Name != "dispose";
            }
            if (decl is Parameter) return true;
            if (decl is Enumeration) return true;
            if (decl is Property) return true;
            if (decl is Event) return true;
            if (decl is TypedefDecl) return true;
            return false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsDynamic)
            {
                // HACK: entries in v-tables are not shared (as objects) with the virtual methods they represent;
                // this is why this pass has to rename entries in the v-table as well;
                // this should be fixed in the parser: it should reuse method objects
                foreach (var method in VTables.GatherVTableMethodEntries(@class).Where(
                    e => e.Method != null && IsRenameableDecl(e.Method)).Select(e => e.Method))
                {
                    Rename(method);
                }
            }

            return base.VisitClassDecl(@class);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!IsRenameableDecl(decl))
                return true;

            if (AlreadyVisited(decl))
                return true;

            Visited.Add(decl);

            if (decl.Name == null)
                return true;

            return Rename(decl);
        }

        private bool Rename(Declaration decl)
        {
            string newName;
            if (Rename(decl.Name, out newName) && !AreThereConflicts(decl, newName))
                decl.Name = newName;
            return true;
        }

        private static bool AreThereConflicts(Declaration decl, string newName)
        {
            List<Declaration> declarations = new List<Declaration>();
            declarations.AddRange(decl.Namespace.Classes);
            declarations.AddRange(decl.Namespace.Enums);
            declarations.AddRange(decl.Namespace.Events);
            declarations.AddRange(decl.Namespace.Functions);
            declarations.AddRange(decl.Namespace.Variables);
            bool result = declarations.Any(d => d != decl && d.Name == newName);
            if (result)
                return true;
            Method method = decl as Method;
            if (method == null || !method.IsGenerated)
                return false;
            return ((Class) method.Namespace).GetPropertyByName(newName) != null;
        }

        public override bool VisitEnumItem(Enumeration.Item item)
        {
            if (!Targets.HasFlag(RenameTargets.EnumItem))
                return false;

            string newName;
            if (Rename(item.Name, out newName))
            {
                item.Name = newName;
                return true;
            }

            return true;
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
        Any = Function | Method | Parameter | Class | Field | Enum | EnumItem | Event | Property | Delegate,
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

        public override bool Rename(string name, out string newName)
        {
            var replace = Regex.Replace(name, Pattern, Replacement);

            if (!name.Equals(replace))
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

        public override bool Rename(string name, out string newName)
        {
            newName = null;

            switch (Pattern)
            {
            case RenameCasePattern.LowerCamelCase:
                newName = ConvertCaseString(name, RenameCasePattern.LowerCamelCase);
                return true;
            case RenameCasePattern.UpperCamelCase:
                newName = ConvertCaseString(name, RenameCasePattern.UpperCamelCase);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts the phrase to specified convention.
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="pattern">The cases.</param>
        /// <returns>string</returns>
        static string ConvertCaseString(string phrase, RenameCasePattern pattern)
        {
            var splittedPhrase = phrase.Split(' ', '-', '.');
            var sb = new StringBuilder();

            switch (pattern)
            {
                case RenameCasePattern.LowerCamelCase:
                    sb.Append(splittedPhrase[0].ToLower());
                    splittedPhrase[0] = string.Empty;
                    break;
                case RenameCasePattern.UpperCamelCase:
                    sb = new StringBuilder();
                    break;
            }

            foreach (var s in splittedPhrase)
            {
                var splittedPhraseChars = s.ToCharArray();
                if (splittedPhraseChars.Length > 0)
                {
                    var str = new String(splittedPhraseChars[0], 1);
                    splittedPhraseChars[0] = (str.ToUpper().ToCharArray())[0];
                }
                sb.Append(new String(splittedPhraseChars));
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
