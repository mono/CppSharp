using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class FieldToPropertyPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            return base.VisitClassDecl(@class);
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            var @class = field.Namespace as Class;
            if (@class == null)
                return false;

            if (ASTUtils.CheckIgnoreField(field))
                return false;

            // Check if we already have a synthetized property.
            var existingProp = @class.Properties.FirstOrDefault(property => property.Field == field);
            if (existingProp != null)
                return false;

            field.GenerationKind = GenerationKind.Internal;

            var prop = new Property
            {
                Name = field.Name,
                Namespace = field.Namespace,
                QualifiedType = field.QualifiedType,
                Access = field.Access,
                Field = field
            };

            // do not rename value-class fields because they would be
            // generated as fields later on even though they are wrapped by properties;
            // that is, in turn, because it's cleaner to write
            // the struct marshalling logic just for properties
            if (!prop.IsInRefTypeAndBackedByValueClassField())
                field.Name = Generator.GeneratedIdentifier(field.Name);

            @class.Properties.Add(prop);

            Log.Debug("Property created from field: {0}::{1}", @class.Name,
                field.Name);

            return false;
        }
    }
}
