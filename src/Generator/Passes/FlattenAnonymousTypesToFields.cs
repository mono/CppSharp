using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// Replaces anonymous types with their contained fields
    /// in class layouts and properties in order to avoid generating
    /// these very same anonymous types which is useless and
    /// also forces unappealing names.
    /// </summary>
    public class FlattenAnonymousTypesToFields : TranslationUnitPass
    {
        public FlattenAnonymousTypesToFields()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitPropertyAccessors = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || @class.Ignore || @class.IsDependent)
                return false;

            for (int i = @class.Fields.Count - 1; i >= 0; i--)
            {
                Field field = @class.Fields[i];
                Class fieldType;
                if (!string.IsNullOrEmpty(field.OriginalName) ||
                    !field.Type.Desugar().TryGetClass(out fieldType) ||
                    !string.IsNullOrEmpty(fieldType.OriginalName))
                    continue;

                ReplaceField(@class, i, fieldType);
                fieldType.Fields.Clear();
                fieldType.ExplicitlyIgnore();
            }

            if (@class.Layout == null)
                return true;

            for (int i = @class.Layout.Fields.Count - 1; i >= 0; i--)
            {
                LayoutField field = @class.Layout.Fields[i];
                Class fieldType;
                if (!string.IsNullOrEmpty(field.Name) ||
                    !field.QualifiedType.Type.Desugar().TryGetClass(out fieldType) ||
                    !string.IsNullOrEmpty(fieldType.OriginalName))
                    continue;
                
                ReplaceLayoutField(@class, i, fieldType);
                fieldType.Fields.Clear();
                fieldType.ExplicitlyIgnore();
            }

            return true;
        }

        private static void ReplaceField(Class @class, int i, Class fieldType)
        {
            @class.Fields.RemoveAt(i);

            for (int j = 0; j < fieldType.Fields.Count; j++)
            {
                Field nestedField = fieldType.Fields[j];
                nestedField.Namespace = @class;
                nestedField.Access = fieldType.Access;
                @class.Fields.Insert(i + j, nestedField);
            }
        }

        private static void ReplaceLayoutField(Class @class, int i, Class fieldType)
        {
            uint offset = @class.Layout.Fields[i].Offset;
            @class.Layout.Fields.RemoveAt(i);

            for (int j = 0; j < fieldType.Layout.Fields.Count; j++)
            {
                LayoutField nestedLayoutField = fieldType.Layout.Fields[j];
                var layoutField = new LayoutField
                    {
                        Expression = nestedLayoutField.Expression,
                        FieldPtr = nestedLayoutField.FieldPtr,
                        Name = nestedLayoutField.Name,
                        Offset = nestedLayoutField.Offset + offset,
                        QualifiedType = nestedLayoutField.QualifiedType
                    };
                @class.Layout.Fields.Insert(i + j, layoutField);
            }
        }
    }
}
