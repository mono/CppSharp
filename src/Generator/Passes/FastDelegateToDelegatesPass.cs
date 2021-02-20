using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Passes;
using Type = CppSharp.AST.Type;

namespace CppSharp
{
    public class FastDelegateToDelegatesPass : TranslationUnitPass
    {
        static bool IsFastDelegate(Type type)
        {
            if (!type.TryGetClass(out var @class))
                return false;

            return @class.Name == "FastDelegate";
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (unit.FileNameWithoutExtension == "FastDelegates" &&
                unit.Namespaces.Any(n => n.Name == "fastdelegate"))
            {
                unit.GenerationKind = GenerationKind.None;
                return true;
            }

            return base.VisitTranslationUnit(unit);
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!IsFastDelegate(field.Type))
                return false;

            var templateSpecType = field.Type as TemplateSpecializationType;
            if (templateSpecType == null)
                throw new Exception("Expected a template specialization type as delegate type");

            var functionType = templateSpecType.Arguments.First().Type.Type as FunctionType;
            if (functionType == null)
                throw new Exception("Expected a function type as inner delegate type");

            var @class = field.Namespace as Class;
            var @event = new Event
            {
                Name = field.Name,
                Namespace = @class,
                QualifiedType = new QualifiedType(functionType),
                GenerationKind = GenerationKind.Generate
            };

            int paramIndex = 0;
            foreach (var param in functionType.Parameters)
            {
                var newParam = new Parameter(param)
                {
                    GenerationKind = GenerationKind.Generate,
                };

                if (string.IsNullOrEmpty(newParam.Name))
                    newParam.Name = $"arg{paramIndex}";

                @event.Parameters.Add(newParam);
                paramIndex++;
            }

            @class.Declarations.Add(@event);
            field.GenerationKind = GenerationKind.None;

            return base.VisitFieldDecl(field);
        }
    }
}