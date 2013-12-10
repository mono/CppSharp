using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FieldToPropertyPass : TranslationUnitPass
    {
        public override bool VisitFieldDecl(Field field)
        {
            if (AlreadyVisited(field))
                return false;

            var @class = field.Namespace as Class;
            if (@class == null)
                return false;

            if (@class.IsValueType)
                return false;

            if (ASTUtils.CheckIgnoreField(field))
                return false;

            // Check if we already have a synthetized property.
            var existingProp = @class.Properties.FirstOrDefault(property =>
                property.Name == field.Name && 
                property.QualifiedType == field.QualifiedType);

            if (existingProp != null)
            {
                field.ExplicityIgnored = true;
                return false;
            }

            var prop = new Property
            {
                Name = field.Name,
                Namespace = field.Namespace,
                QualifiedType = field.QualifiedType,
                Access = field.Access,
                Field = field
            };
            @class.Properties.Add(prop);

            Log.Debug("Property created from field: {0}::{1}", @class.Name, field.Name);

            field.ExplicityIgnored = true;

            return false;
        }
    }
}
