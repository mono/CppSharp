using System;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class CheckFlagEnumsPass : TranslationUnitPass
    {
        static bool IsFlagEnum(Enumeration @enum)
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

    public static class CheckFlagEnumsExtensions
    {
        public static void CheckFlagEnums(this PassBuilder builder)
        {
            var pass = new CheckFlagEnumsPass();
            builder.AddPass(pass);
        }
    }
}
