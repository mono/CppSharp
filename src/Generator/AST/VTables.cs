using System;
using System.Collections.Generic;

namespace CppSharp.AST
{
    public static class VTables
    {
        public static List<VTableComponent> GatherVTableMethodEntries(Class @class)
        {
            switch (@class.Layout.ABI)
            {
            case CppAbi.Microsoft:
                return GatherVTableMethodsMS(@class);
            case CppAbi.Itanium:
                return GatherVTableMethodsItanium(@class);
            }

            throw new NotSupportedException();
        }

        public static List<VTableComponent> GatherVTableMethodEntries(VTableLayout layout)
        {
            var entries = new List<VTableComponent>();

            foreach (var component in layout.Components)
            {
                if (component.Kind == VTableComponentKind.CompleteDtorPointer)
                    continue;

                if (component.Kind == VTableComponentKind.RTTI)
                    continue;

                if (component.Kind == VTableComponentKind.UnusedFunctionPointer)
                    continue;

                if (component.Method != null)
                    entries.Add(component);
            }

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

        public static int GetVTableComponentIndex(VTableLayout layout, VTableComponent entry)
        {
            return layout.Components.IndexOf(entry);
        }

        public static int GetVTableComponentIndex(Class @class, VTableComponent entry)
        {
            switch (@class.Layout.ABI)
            {
            case CppAbi.Microsoft:
                foreach (var vfptr in @class.Layout.VFTables)
                {
                    var index = GetVTableComponentIndex(vfptr.Layout, entry);
                    if (index >= 0)
                        return index;
                }
                break;
            case CppAbi.Itanium:
                return GetVTableComponentIndex(@class.Layout.Layout, entry);
            }

            throw new NotSupportedException();
        }
    }
}
