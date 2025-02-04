using CppSharp.AST;
using CppSharp.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// Validates enumerations and checks if any should be treated as a collection
    /// of flags (and annotate them with the .NET [Flags] when generated).
    /// </summary>
    public class CheckEnumsPass : TranslationUnitPass
    {
        public CheckEnumsPass()
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

        private bool IsValidEnumBaseType(Enumeration @enum)
        {
            if (Options.IsCSharpGenerator)
                return @enum.BuiltinType.Type.IsIntegerType();

            return @enum.BuiltinType.Type.IsIntegerType() || @enum.BuiltinType.Type == PrimitiveType.Bool;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            if (!IsValidEnumBaseType(@enum))
            {
                if (@enum.BuiltinType.Type == PrimitiveType.Bool)
                {
                    @enum.BuiltinType = new BuiltinType(PrimitiveType.UChar);
                }
                else
                {
                    Diagnostics.Warning(
                        "The enum `{0}` has a base type of `{1}`, which is currently not supported. The base type will be ignored.",
                        @enum, @enum.BuiltinType);
                    @enum.BuiltinType = new BuiltinType(PrimitiveType.Int);
                }
            }

            if (IsFlagEnum(@enum))
                @enum.Modifiers |= Enumeration.EnumModifiers.Flags;
            
            return true;
        }
    }
}
