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
                ReplaceLayoutField(@class, field, fieldType);
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
                @class.Fields.Insert(i + j, nestedField);
            }
        }

        private static void ReplaceLayoutField(Class @class, Field field, Class fieldType)
        {
            LayoutField layoutField = @class.Layout.Fields.Find(
                                f => f.FieldPtr == field.OriginalPtr);
            int layoutIndex = @class.Layout.Fields.IndexOf(layoutField);
            @class.Layout.Fields.RemoveAt(layoutIndex);

            for (int j = 0; j < fieldType.Layout.Fields.Count; j++)
            {
                LayoutField nestedlayoutField = fieldType.Layout.Fields[j];
                nestedlayoutField.Offset += layoutField.Offset;
                @class.Layout.Fields.Insert(layoutIndex + j, nestedlayoutField);
            }
        }
    }
}
