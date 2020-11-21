using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for enumerations that should be treated as a collection
    /// of flags (and annotated with the .NET [Flags] when generated).
    /// </summary>
    public class CheckFlagEnumsPass : TranslationUnitPass
    {
        public CheckFlagEnumsPass()
            => VisitOptions.ResetFlags(VisitFlags.NamespaceEnums);

        private static bool IsFlagEnum(Enumeration @enum)
        {
            // If the enumeration only has power of two values, assume it's
            // a flags enum.

            var isFlags = true;
            var hasBigRange = false;

            foreach (var item in @enum.Items)
            {
                var value = item.Value;

                if (value >= 4)
                    hasBigRange = true;

                if (value <= 1 || value.IsPowerOfTwo())
                    continue;

                isFlags = false;
            }

            // Only apply this heuristic if there are enough values to have a
            // reasonable chance that it really is a bitfield.

            return isFlags && hasBigRange;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (IsFlagEnum(@enum))
            {
                @enum.Modifiers |= Enumeration.EnumModifiers.Flags;
                return true;
            }

            return base.VisitEnumDecl(@enum);
        }
    }
}
