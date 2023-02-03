using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public static class VTables
    {
        public const int ItaniumOffsetToTopAndRTTI = 2;

        public static List<VTableComponent> GatherVTableMethodEntries(Class @class)
        {
            switch (@class.Layout.ABI)
            {
                case CppAbi.Microsoft:
                    return GatherVTableMethodsMS(@class);
                case CppAbi.Itanium:
                    return GatherVTableMethodsItanium(@class);
            }

            throw new NotSupportedException(
                string.Format("VTable format for {0} is not supported",
                @class.Layout.ABI.ToString().Split('.').Last())
            );
        }

        private static List<VTableComponent> GatherVTableMethodEntries(VTableLayout layout)
        {
            var entries = new List<VTableComponent>();
            if (layout == null)
                return entries;

            entries.AddRange(from component in layout.Components
                             where component.Kind != VTableComponentKind.CompleteDtorPointer &&
                                   component.Kind != VTableComponentKind.RTTI &&
                                   component.Kind != VTableComponentKind.UnusedFunctionPointer &&
                                   component.Kind != VTableComponentKind.OffsetToTop &&
                                   component.Method != null
                             select component);

            return entries;
        }

        public static List<VTableComponent> GatherVTableMethodsMS(Class @class)
        {
            var entries = new List<VTableComponent>();

            foreach (var vfptr in @class.Layout.VFTables)
                entries.AddRange(GatherVTableMethodEntries(vfptr.Layout));

            return entries;
        }

        public static List<VTableComponent> GatherVTableMethodsItanium(Class @class)
        {
            return GatherVTableMethodEntries(@class.Layout.Layout);
        }

        public static int GetVTableIndex(Function function)
        {
            var @class = (Class)function.Namespace;
            switch (@class.Layout.ABI)
            {
                case CppAbi.Microsoft:
                    return (from table in @class.Layout.VFTables
                            let j = table.Layout.Components.FindIndex(m => m.Method == function) -
                                (table.Layout.Components.Any(c => c.Kind == VTableComponentKind.RTTI) ? 1 : 0)
                            where j >= 0
                            select j).First();
                default:
                    return @class.Layout.Layout.Components.FindIndex(
                        m => m.Method == function) - ItaniumOffsetToTopAndRTTI;
            }
        }

        public static bool IsIgnored(this VTableComponent entry)
        {
            return entry.Method != null &&
                   (entry.Method.IsOperator ||
                    (!entry.Method.IsDeclared &&
                     ((Class)entry.Method.Namespace).GetPropertyByConstituentMethod(entry.Method) == null));
        }
    }
}
