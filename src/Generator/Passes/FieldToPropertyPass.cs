using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class FieldToPropertyPass : TranslationUnitPass
    {
        public FieldToPropertyPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassFields | VisitFlags.ClassTemplateSpecializations);

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

            if (ASTUtils.CheckIgnoreField(field))
                return false;

            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
            {
                if (field.Access != AccessSpecifier.Public)
                    return false;
            }

            if (field.Namespace is not Class @class)
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
                Field = field,
                AssociatedDeclaration = field,
                Comment = field.Comment
            };

            if (Options.GeneratorKind == GeneratorKind.C ||
                Options.GeneratorKind == GeneratorKind.CPlusPlus)
                GenerateAcessorMethods(field, prop);

            // do not rename value-class fields because they would be
            // generated as fields later on even though they are wrapped by properties;
            // that is, in turn, because it's cleaner to write
            // the struct marshalling logic just for properties
            if (!prop.IsInRefTypeAndBackedByValueClassField())
                field.Name = Generator.GeneratedIdentifier(field.Name);

            @class.Properties.Add(prop);

            Diagnostics.Debug($"Property created from field: {field.QualifiedName}");

            return false;
        }

        private void GenerateAcessorMethods(Field field, Property property)
        {
            var @class = field.Namespace as Class;

            var getter = new Method
            {
                Name = $"get_{field.Name}",
                Namespace = @class,
                ReturnType = field.QualifiedType,
                Access = field.Access,
                AssociatedDeclaration = property,
                IsStatic = field.IsStatic,
                SynthKind = FunctionSynthKind.FieldAcessor
            };

            property.GetMethod = getter;
            @class.Methods.Add(getter);

            var isSetterInvalid = field.QualifiedType.IsConstRef();
            if (!isSetterInvalid)
            {
                var setter = new Method
                {
                    Name = $"set_{field.Name}",
                    Namespace = @class,
                    ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Void)),
                    Access = field.Access,
                    AssociatedDeclaration = property,
                    IsStatic = field.IsStatic,
                    SynthKind = FunctionSynthKind.FieldAcessor
                };

                var param = new Parameter
                {
                    Name = "value",
                    QualifiedType = field.QualifiedType,
                    Namespace = setter
                };

                setter.Parameters.Add(param);

                property.SetMethod = setter;
                @class.Methods.Add(setter);
            }
        }
    }
}
