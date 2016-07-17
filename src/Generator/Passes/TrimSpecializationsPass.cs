using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class TrimSpecializationsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || @class.IsIncomplete || !@class.IsDependent)
                return false;

            @class.Specializations.RemoveAll(
                s => s.Fields.Any(f => f.Type.IsPrimitiveType(PrimitiveType.Void)));

            if (@class.Specializations.Count == 0)
                return false;

            var groups = (from specialization in @class.Specializations
                          group specialization by specialization.Arguments.All(
                              a => a.Type.Type != null && a.Type.Type.IsAddress()) into @group
                          select @group).ToList();

            foreach (var group in groups.Where(g => g.Key))
                foreach (var specialization in group.Skip(1))
                    @class.Specializations.Remove(specialization);

            for (int i = @class.Specializations.Count - 1; i >= 0; i--)
                if (@class.Specializations[i] is ClassTemplatePartialSpecialization)
                    @class.Specializations.RemoveAt(i);

            return true;
        }
    }
}
