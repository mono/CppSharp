using System;
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

        public static string GetVirtualCallDelegate(INamedDecl method, Class @class,
            bool is32Bit, out string delegateId)
        {
            var virtualCallBuilder = new StringBuilder();
            virtualCallBuilder.AppendFormat(
                "void* vtable = *((void**) {0}.ToPointer());",
                Helpers.InstanceIdentifier);
            virtualCallBuilder.AppendLine();

            int i;
            switch (@class.Layout.ABI)
            {
                case CppAbi.Microsoft:
                    i = (from table in @class.Layout.VFTables
                         let j = table.Layout.Components.FindIndex(m => m.Method == method)
                         where j >= 0
                         select j).First();
                    break;
                default:
                    i = @class.Layout.Layout.Components.FindIndex(m => m.Method == method);
                    break;
            }

            virtualCallBuilder.AppendFormat(
                "void* slot = *((void**) vtable + {0} * {1});", i, is32Bit ? 4 : 8);
            virtualCallBuilder.AppendLine();

            string @delegate = method.Name + "Delegate";
            delegateId = Generator.GeneratedIdentifier(@delegate);

            virtualCallBuilder.AppendFormat(
                "var {1} = ({0}) Marshal.GetDelegateForFunctionPointer(new IntPtr(slot), typeof({0}));",
                @delegate, delegateId);
            virtualCallBuilder.AppendLine();
            return virtualCallBuilder.ToString();
        }
    }
}
