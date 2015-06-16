using System.Linq;
using CppSharp.AST;
using CppSharp.Utils;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass checks for vftable entries with duplicated components.
    /// This might happen because of bugs in Clang vftable layouting.
    /// </summary>
    public class CheckVTableComponentsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(AST.Class @class)
        {
            foreach (var vfptr in @class.Layout.VFTables)
            {
                var uniqueEntries = new OrderedSet<VTableComponent>();
                foreach (var entry in vfptr.Layout.Components)
                    uniqueEntries.Add(entry);

                // The vftable does not have duplicated components.
                if (vfptr.Layout.Components.Count == uniqueEntries.Count)
                    continue;

                Driver.Diagnostics.Warning(
                    "Class '{0}' found with duplicated vftable components",
                    @class.Name);
                vfptr.Layout.Components = uniqueEntries.ToList();
            }

            return base.VisitClassDecl(@class);
        }
    }
}
