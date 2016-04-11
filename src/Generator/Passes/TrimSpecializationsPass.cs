using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class TrimSpecializationsPass : TranslationUnitPass
    {
        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!base.VisitClassTemplateDecl(template) ||
                template.Specializations.Count == 0)
                return false;

            var lastGroup = (from specialization in template.Specializations
                             group specialization by specialization.Arguments.All(
                                 a => a.Type.Type != null && a.Type.Type.IsAddress()) into @group
                             select @group).Last();
            if (lastGroup.Key)
            {
                foreach (var specialization in lastGroup.Skip(1))
                    template.Specializations.Remove(specialization);
            }

            return true;
        }
    }
}
