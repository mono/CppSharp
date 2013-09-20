using CppSharp.AST;

namespace CppSharp.Passes
{
    public class ParamTypeToInterfacePass : TranslationUnitPass
    {
        public override bool VisitParameterDecl(Parameter parameter)
        {
            var tagType = parameter.QualifiedType.Type as TagType;
            if (tagType == null)
            {
                var pointerType = parameter.QualifiedType.Type as PointerType;
                if (pointerType != null)
                    tagType = pointerType.Pointee as TagType;
            }
            if (tagType != null)
            {
                var @class = tagType.Declaration as Class;
                if (@class != null)
                {
                    var @interface = @class.Namespace.Classes.Find(c => c.OriginalClass == @class);
                    if (@interface != null)
                        parameter.QualifiedType = new QualifiedType(new TagType(@interface));
                }
            }
            return base.VisitParameterDecl(parameter);
        }
    }
}
