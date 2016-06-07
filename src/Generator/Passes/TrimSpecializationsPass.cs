using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class TrimSpecializationsPass : TranslationUnitPass
    {
        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!base.VisitClassTemplateDecl(template) || template.IsIncomplete)
                return false;

            template.Specializations.RemoveAll(
                s => s.Fields.Any(f => f.Type.IsPrimitiveType(PrimitiveType.Void)));

            if (template.Specializations.Count == 0)
                return false;

            var groups = (from specialization in template.Specializations
                          group specialization by specialization.Arguments.All(
                              a => a.Type.Type != null && a.Type.Type.IsAddress()) into @group
                          select @group).ToList();

            foreach (var group in groups.Where(g => g.Key))
                foreach (var specialization in group.Skip(1))
                    template.Specializations.Remove(specialization);

            for (int i = template.Specializations.Count - 1; i >= 0; i--)
                if (template.Specializations[i] is ClassTemplatePartialSpecialization)
                    template.Specializations.RemoveAt(i);

            return true;
        }
    }
}
