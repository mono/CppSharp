﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;

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
            if (layout == null)
                return entries;

            entries.AddRange(from component in layout.Components
                             where component.Kind != VTableComponentKind.CompleteDtorPointer &&
                                   component.Kind != VTableComponentKind.RTTI &&
                                   component.Kind != VTableComponentKind.UnusedFunctionPointer &&
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

        public static int GetVTableIndex(INamedDecl method, Class @class)
        {
            switch (@class.Layout.ABI)
            {
                case CppAbi.Microsoft:
                    return (from table in @class.Layout.VFTables
                            let j = table.Layout.Components.FindIndex(m => m.Method == method)
                            where j >= 0
                            select j).First();
                default:
                    return @class.Layout.Layout.Components.FindIndex(m => m.Method == method);
            }
        }
    }
}
