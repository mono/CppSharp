using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FieldToPropertyPass : TranslationUnitPass
    {
        public override bool VisitFieldDecl(Field field)
        {
            var @class = field.Namespace as Class;
            if (@class == null)
                return false;

            if (@class.IsValueType)
                return false;

            if (ASTUtils.CheckIgnoreField(@class,field))
                return false;

            var prop = new Property()
            {
                Name = field.Name,
                Namespace = field.Namespace,
                QualifiedType = field.QualifiedType,
                Field = field
            };
            @class.Properties.Add(prop);

            field.ExplicityIgnored = true;

            return false;
        }
    }
}
