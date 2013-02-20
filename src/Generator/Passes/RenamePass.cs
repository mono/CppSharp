using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Cxxi.Passes
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
    }

    [Flags]
    public enum RenameTargets
    {
        Record,
        Field,
        Method,
        Function,
        Enum,
        EnumItem,
        Any = Function | Method | Record | Field | Enum | EnumItem,
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

        public override bool ProcessDeclaration(Declaration decl)
        {
            if (!Targets.HasFlag(RenameTargets.Any))
                return false;

            string newName;
            if (Rename(decl.Name, out newName))
            {
                decl.Name = newName;
                return true;
            }

            return false;
        }

        public override bool ProcessEnumItem(Enumeration.Item item)
        {
            if (!Targets.HasFlag(RenameTargets.EnumItem))
                return false;

            string newName;
            if (Rename(item.Name, out newName))
            {
                item.Name = newName;
                return true;
            }

            return false;
        }

        public override bool ProcessField(Field field)
        {
            if (!Targets.HasFlag(RenameTargets.Field))
                return false;

            string newName;
            if (Rename(field.Name, out newName))
            {
                field.Name = newName;
                return true;
            }

            return false;
        }

        bool Rename(string name, out string newName)
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

        private void Rename<T>(ref T decl) where T : Declaration
        {
            switch (Pattern)
            {
                case RenameCasePattern.LowerCamelCase:
                    decl.Name = ConvertCaseString(decl.Name, RenameCasePattern.LowerCamelCase);
                    break;
                case RenameCasePattern.UpperCamelCase:
                    decl.Name = ConvertCaseString(decl.Name, RenameCasePattern.UpperCamelCase);
                    break;
            }
        }

        public override bool ProcessDeclaration(Declaration decl)
        {
            if (!Targets.HasFlag(RenameTargets.Any))
                return false;

            Rename(ref decl);
            return true;
        }

        public override bool ProcessFunction(Function function)
        {
            if (!Targets.HasFlag(RenameTargets.Function))
                return false;

            Rename(ref function);
            return true;
        }

        public override bool ProcessField(Field field)
        {
            if (!Targets.HasFlag(RenameTargets.Field))
                return false;

            Rename(ref field);

            return false;
        }

        public override bool ProcessMethod(Method method)
        {
            if (!Targets.HasFlag(RenameTargets.Method))
                return false;

            if (method.Kind != CXXMethodKind.Normal)
                return false;

            Rename(ref method);
            return true;
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
        public static void RenameWithPattern(this PassBuilder builder,
            string pattern, string replacement, RenameTargets targets)
        {
            builder.AddPass(new RegexRenamePass(pattern, replacement, targets));
        }

        public static void RemovePrefix(this PassBuilder builder, string prefix)
        {
            builder.AddPass(new RegexRenamePass("^" + prefix, String.Empty));
        }

        public static void RemovePrefixEnumItem(this PassBuilder builder, string prefix)
        {
            builder.AddPass(new RegexRenamePass("^" + prefix, String.Empty,
                RenameTargets.EnumItem));
        }

        public static void RenameDeclsCase(this PassBuilder builder, 
            RenameTargets targets, RenameCasePattern pattern)
        {
            builder.AddPass(new CaseRenamePass(targets, pattern));
        }

        public static void RenameDeclsUpperCase(this PassBuilder builder,
            RenameTargets targets)
        {
            builder.AddPass(new CaseRenamePass(targets,
                RenameCasePattern.UpperCamelCase));
        }

        public static void RenameDeclsLowerCase(this PassBuilder builder,
            RenameTargets targets)
        {
            builder.AddPass(new CaseRenamePass(targets,
                RenameCasePattern.LowerCamelCase));
        }
    }
}
